using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SubscriptionBilling.API.Middleware;
using SubscriptionBilling.Application;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Infrastructure;
using SubscriptionBilling.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add Layers Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Controllers
builder.Services.AddControllers();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "SubscriptionBillingDefaultSuperSecretKey1234567890!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "SubscriptionBillingAPI",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "SubscriptionBillingClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireCustomerRole", policy => policy.RequireRole("Customer"));
});

// 6. Health Checks
builder.Services.AddHealthChecks();

// 7. Swagger / OpenAPI Configuration with JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Subscription & Billing Management System API",
        Version = "v1",
        Description = "Enterprise-grade Subscription and Billing Management Web API powered by ASP.NET Core, MySQL 8.4 LTS, and Stripe."
    });

    // JWT Security Definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token only (do NOT type 'Bearer ' manually)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Ensure logs directory exists for IIS stdout logging and file logging
var logsPath = Path.Combine(app.Environment.ContentRootPath, "logs");
if (!Directory.Exists(logsPath))
{
    try { Directory.CreateDirectory(logsPath); } catch { }
}

// 8. Startup Diagnostics & Database Connectivity Check
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = app.Environment;
    var contentRoot = env.ContentRootPath;
    var prodSettingsPath = Path.Combine(contentRoot, "appsettings.Production.json");
    bool prodSettingsExists = File.Exists(prodSettingsPath);

    var rawConnStr = config.GetConnectionString("DefaultConnection");
    string dbHost = "Unknown";
    string dbName = "Unknown";
    uint dbPort = 3306;
    string sslMode = "Default";

    if (!string.IsNullOrWhiteSpace(rawConnStr))
    {
        try
        {
            var csb = new MySqlConnector.MySqlConnectionStringBuilder(rawConnStr);
            dbHost = csb.Server;
            dbName = csb.Database;
            dbPort = csb.Port;
            sslMode = csb.SslMode.ToString();
        }
        catch
        {
            dbHost = "(Unparseable connection string)";
        }
    }

    logger.LogInformation("================== STARTUP DIAGNOSTICS ==================");
    logger.LogInformation("Environment: {Environment}", env.EnvironmentName);
    logger.LogInformation("ContentRootPath: {ContentRootPath}", contentRoot);
    logger.LogInformation("appsettings.Production.json found: {ProdSettingsExists} ({Path})", prodSettingsExists, prodSettingsPath);
    logger.LogInformation("Database Provider: MySQL / MariaDB (Pomelo.EntityFrameworkCore.MySql)");
    logger.LogInformation("Database Target: Host={Host}, Port={Port}, Database={Database}, SslMode={SslMode}", dbHost, dbPort, dbName, sslMode);

    bool isLocalhost = dbHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) || dbHost.Equals("127.0.0.1");
    if (env.IsProduction() && isLocalhost)
    {
        logger.LogWarning("ALERT: The application is running in PRODUCTION but the database host is configured as '{Host}'. MonsterASP.NET database instances are NOT on localhost! Verify that appsettings.Production.json is placed in the website root (/wwwroot/) and contains your MonsterASP database credentials.", dbHost);
    }

    var dbContext = scope.ServiceProvider.GetRequiredService<SubscriptionBillingDbContext>();
    try
    {
        bool canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Database Connectivity Test: SUCCESS. Connection to {Host}/{Database} established.", dbHost, dbName);
            var applied = await dbContext.Database.GetAppliedMigrationsAsync();
            var pending = await dbContext.Database.GetPendingMigrationsAsync();
            logger.LogInformation("Migrations Status: Applied={AppliedCount}, Pending={PendingCount}", applied.Count(), pending.Count());
        }
        else
        {
            logger.LogWarning("Database Connectivity Test: FAILED. CanConnect returned false for {Host}/{Database}.", dbHost, dbName);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database Connectivity Test: ERROR connecting to {Host}/{Database}. Exception: {Message}", dbHost, dbName, ex.Message);
    }
    logger.LogInformation("=========================================================");
}

// 9. Auto-Migration & Database Seeder on startup (Development only or if explicitly configured)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<SubscriptionBillingDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        logger.LogInformation("Applying EF Core migrations to MySQL...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations applied successfully. Seeding initial data (Roles, Admin, Plans)...");
        await DatabaseSeeder.SeedAsync(dbContext, passwordHasher);
        logger.LogInformation("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding. Ensure MySQL is running and connection string is configured.");
    }
}

// 9. Pipeline Configuration
// Global exception handling at the top
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Development-only tools: Swagger & permissive CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("AngularDevPolicy");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscription Billing API v1");
        c.RoutePrefix = "swagger";
    });
}

// Serve default file (index.html) and static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// API Controllers
app.MapControllers();

// Unknown /api routes return 404 instead of falling back to index.html
app.Map("/api/{**slug}", () => Results.NotFound());

// Fallback to Angular SPA index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
