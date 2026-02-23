using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Petek.Server.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Petek.Server.Authentication;

public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Empty token");
        }

        try
        {
            // Create a new scope to get the DbContext
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PetekDbContext>();

            var session = await context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                return AuthenticateResult.Fail("Invalid or expired session");
            }

            // Update last activity
            session.LastActivityAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
                new Claim(ClaimTypes.Name, session.User.Username),
                new Claim("DisplayName", session.User.DisplayName ?? session.User.Username),
                new Claim(ClaimTypes.Role, session.User.Role.ToString())
            };

            if (!string.IsNullOrEmpty(session.User.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, session.User.Email));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Session authentication failed");
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }
}

public static class SessionAuthenticationExtensions
{
    public const string SchemeName = "SessionAuth";

    public static AuthenticationBuilder AddSessionAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SchemeName, null);
    }
}
