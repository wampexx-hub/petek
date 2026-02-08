using Petek.Server.Data;
using Petek.Server.Hubs;
using Petek.Server.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
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

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// Admin dashboard at /admin
app.MapGet("/admin", () => Results.File(
    Path.Combine(app.Environment.WebRootPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), "admin.html"),
    "text/html"));

app.UseCors("PetekCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/hubs/message");

// Ensure database is created
try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("========================================");
    Console.WriteLine("  Petek Messenger Server v1.0");
    Console.WriteLine("========================================");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine($"Veritabani tipi: {dbType}");
    Console.WriteLine("Veritabani baglantisi kontrol ediliyor...");

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PetekDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Mevcut veritabanına eksik kolonları ekle (migration olmadan)
        try
        {
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();

            // PasswordHash kolonu var mı kontrol et
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

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Veritabani hazir.");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Sunucu baslatiliyor...");
    Console.WriteLine("  API:   http://localhost:5000/api");
    Console.WriteLine("  Admin: http://localhost:5000/admin");
    Console.WriteLine("  Hub:   http://localhost:5000/hubs/message");
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
