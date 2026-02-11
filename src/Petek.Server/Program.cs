using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Server.Hubs;
using Petek.Server.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Windows Service destegi
if (!builder.Environment.IsDevelopment())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "PetekServer";
    });
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Database Configuration
var dbType = builder.Configuration["Database:Type"] ?? "SQLite";
var connectionString = builder.Configuration.GetConnectionString(dbType);

builder.Services.AddDbContext<PetekDbContext>(options =>
{
    if (dbType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString ?? "Host=localhost;Database=Petek;Username=postgres;Password=postgres");
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=Petek.db");
    }
});

// Authentication - Windows Negotiate (Kerberos/NTLM)
var ssoEnabled = builder.Configuration.GetValue<bool>("SSO:Enabled", true);
if (ssoEnabled)
{
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
else
{
    builder.Services.AddAuthentication("Bearer");
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 5 * 1024 * 1024; // 5 MB - dosya transferi icin
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PetekCors", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? new[] { "http://localhost:5000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// HTTP Client Factory (for webhooks)
builder.Services.AddHttpClient();

// Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ISurveillanceService, SurveillanceService>();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// Admin dashboard at /admin
app.MapGet("/admin", () => Results.File(
    Path.Combine(app.Environment.WebRootPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), "admin.html"),
    "text/html"));

// Health check endpoint
app.MapHealthChecks("/health");

// Server bilgi endpoint'i (kimlik dogrulamasi gerektirmez)
app.MapGet("/api/server/info", () =>
{
    return Results.Ok(new
    {
        name = "Petek Messenger Server",
        version = "1.1.0",
        status = "running",
        time = DateTime.UtcNow
    });
});

app.UseCors("PetekCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/hubs/message");

// Veritabani olustur, admin kullanici seed'le
try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("========================================");
    Console.WriteLine("  Petek Messenger Server v1.1.0");
    Console.WriteLine("========================================");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine($"Veritabani tipi: {dbType}");
    Console.WriteLine("Veritabani baglantisi kontrol ediliyor...");

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PetekDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Mevcut veritabanina eksik kolonlari ekle (migration olmadan)
        if (dbType.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var conn = context.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "PRAGMA table_info(Users)";
                var columns = new List<string>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader.GetString(1));
                    }
                }

                if (!columns.Contains("PasswordHash"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE Users ADD COLUMN PasswordHash TEXT NULL";
                    await alterCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("PasswordHash kolonu eklendi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kolon kontrol hatasi (onemli degil): {ex.Message}");
            }
        }

        // Otomatik admin kullanici olusturma (kurulum scriptinden gelen ayarlar)
        var adminUsername = app.Configuration["AdminSetup:Username"];
        var adminPassword = app.Configuration["AdminSetup:Password"];

        if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
        {
            var existingAdmin = await context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == adminUsername.ToLower());

            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = adminUsername.ToLower(),
                    DisplayName = "Yonetici",
                    Email = $"{adminUsername.ToLower()}@petek.local",
                    Role = Petek.Shared.Enums.UserRole.Admin,
                    Status = Petek.Shared.Enums.UserStatus.Offline,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = HashPassword(adminPassword)
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Yonetici hesabi olusturuldu: {adminUsername}");
                Console.ResetColor();
            }
        }
        else
        {
            // Hic kullanici yoksa varsayilan admin olustur
            var hasAnyUser = await context.Users.AnyAsync();
            if (!hasAnyUser)
            {
                var defaultAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    DisplayName = "Yonetici",
                    Email = "admin@petek.local",
                    Role = Petek.Shared.Enums.UserRole.Admin,
                    Status = Petek.Shared.Enums.UserStatus.Offline,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = HashPassword("admin123")
                };
                context.Users.Add(defaultAdmin);
                await context.SaveChangesAsync();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("UYARI: Varsayilan yonetici hesabi olusturuldu.");
                Console.WriteLine("  Kullanici: admin");
                Console.WriteLine("  Sifre: admin123");
                Console.WriteLine("  Lutfen ilk girisden sonra sifreyi degistirin!");
                Console.ResetColor();
            }
        }
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Veritabani hazir.");
    Console.ResetColor();
    Console.WriteLine();

    // Sunucu baglanti bilgilerini goster
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("========================================");
    Console.WriteLine("  SUNUCU BAGLANTI BILGILERI");
    Console.WriteLine("========================================");
    Console.ResetColor();
    Console.WriteLine();

    var urls = app.Urls.Any()
        ? app.Urls
        : new[] { "http://localhost:5000" };

    foreach (var url in urls)
    {
        Console.WriteLine($"  Dinleniyor: {url}");
    }

    Console.WriteLine();

    // Ag arayuzlerinin IP adreslerini goster
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Istemciler asagidaki adreslerden baglanabilir:");
    Console.ResetColor();

    try
    {
        var port = "5000";
        foreach (var url in urls)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                port = uri.Port.ToString();
                break;
            }
        }

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        foreach (var ni in interfaces)
        {
            var ipProps = ni.GetIPProperties();
            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    http://{addr.Address}:{port}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"      ({ni.Name} - {ni.Description})");
                    Console.ResetColor();
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    http://{Dns.GetHostName()}:{port}");
        Console.ResetColor();
    }
    catch
    {
        Console.WriteLine("    (Ag bilgileri alinamadi)");
    }

    Console.WriteLine();
    Console.WriteLine("  Endpointler:");
    Console.WriteLine("    API      : /api");
    Console.WriteLine("    Hub      : /hubs/message");
    Console.WriteLine("    Admin    : /admin");
    Console.WriteLine("    Saglik   : /health");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("HATA: Veritabanina baglanamadi!");
    Console.WriteLine($"Hata Detayi: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine("\nSunucu kapatiliyor...");
    await Task.Delay(10000);
    return;
}

app.Run();

// Sifre hashleme yardimci metodu
static string HashPassword(string password)
{
    var salt = new byte[16];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(salt);

    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(32);

    var combined = new byte[48];
    Array.Copy(salt, 0, combined, 0, 16);
    Array.Copy(hash, 0, combined, 16, 32);

    return Convert.ToBase64String(combined);
}
