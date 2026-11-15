using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Infrastructure.Data;

namespace SubscriptionBilling.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly SubscriptionBillingDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        SubscriptionBillingDbContext dbContext,
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Temporary database diagnostics and health check endpoint.
    /// Safely reports connectivity, environment, and migration status without exposing sensitive credentials.
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> GetDbHealth()
    {
        var rawConnStr = _configuration.GetConnectionString("DefaultConnection");
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

        var prodSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.Production.json");
        bool prodSettingsExists = System.IO.File.Exists(prodSettingsPath);

        bool canConnect = false;
        string? error = null;
        IEnumerable<string> appliedMigrations = Array.Empty<string>();
        IEnumerable<string> pendingMigrations = Array.Empty<string>();
        bool rolesSeeded = false;
        bool adminUserExists = false;
        int planCount = 0;

        try
        {
            canConnect = await _dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();

                if (appliedMigrations.Any())
                {
                    rolesSeeded = await _dbContext.Roles.AnyAsync();
                    planCount = await _dbContext.SubscriptionPlans.CountAsync();
                    var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null)
                    {
                        adminUserExists = await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == adminRole.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "Health check failed to query database.");
        }

        bool isLocalhost = dbHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) || dbHost.Equals("127.0.0.1");
        bool isHealthy = canConnect && error == null;

        var response = new
        {
            Status = isHealthy ? "Healthy" : "Unhealthy",
            TimestampUtc = DateTime.UtcNow,
            Environment = _env.EnvironmentName,
            DatabaseProvider = "MySQL / MariaDB (Pomelo.EntityFrameworkCore.MySql)",
            ConfiguredTarget = new
            {
                Host = dbHost,
                Port = dbPort,
                Database = dbName,
                SslMode = sslMode
            },
            ConfigurationCheck = new
            {
                AppsettingsProductionFound = prodSettingsExists,
                IsLocalhost = isLocalhost,
                Warning = (isLocalhost && _env.IsProduction())
                    ? "Database host is set to 'localhost' while running in PRODUCTION. MonsterASP.NET database instances are not hosted on localhost! Please configure the MonsterASP server name in appsettings.Production.json."
                    : null
            },
            Connectivity = new
            {
                CanConnect = canConnect,
                AppliedMigrations = appliedMigrations,
                PendingMigrations = pendingMigrations,
                TablesExist = appliedMigrations.Any(),
                RolesSeeded = rolesSeeded,
                PlansCount = planCount,
                AdminUserExists = adminUserExists,
                Error = error
            }
        };

        return isHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
