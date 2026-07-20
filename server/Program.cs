using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OneSecurity.Server.Data;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Services;
using OneSecurity.Server.Realtime;
using OneSecurity.Server.Configuration;

using OneSecurity.Server.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        
        
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());
    });
builder.Services.AddRequestDecompression();
builder.Services.AddHttpContextAccessor();

// Configure SQLite Database using LocalAgentDbContext
builder.Services.AddDbContext<LocalAgentDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("OneSecurity.Server") // Đảm bảo đúng tên Assembly project của bạn
    ));
// Configure Identity Services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<LocalAgentDbContext>()
.AddDefaultTokenProviders();

// Configure SignalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.PayloadSerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());
    });

// Register Repositories and Services
builder.Services.AddSingleton<IHospitalHierarchyCache, HospitalHierarchyCache>();
builder.Services.AddScoped<IHospitalAuthService, HospitalAuthService>(); // <--- ĐĂNG KÝ HOSPITAL AUTH SERVICE
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAgentConfigRepository, AgentConfigRepository>();
builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IResponseActionRepository, ResponseActionRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ICollectorRepository, CollectorRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IAgentRegistrationService, AgentRegistrationService>();
builder.Services.AddScoped<IAgentHeartbeatService, AgentHeartbeatService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ICollectorService, CollectorService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IMetricService, MetricService>();
builder.Services.AddScoped<ISecurityEventService, SecurityEventService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
builder.Services.AddSingleton<RegexCache>();
builder.Services.AddSingleton<IRuleCacheService, RuleCacheService>();
builder.Services.AddSingleton<IRuleEngine, RuleEngine>();
builder.Services.AddSingleton<IRuleStatisticsTracker, RuleStatisticsTracker>();
builder.Services.AddHttpClient<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IResponseActionService, ResponseActionService>();
builder.Services.AddScoped<ICommandQueueService, CommandQueueService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IOverviewService, OverviewService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IThreatHuntingService, ThreatHuntingService>();
builder.Services.AddScoped<OneSecurity.Server.Services.HuntingProviders.AgentThreatSearchProvider>();
builder.Services.AddScoped<OneSecurity.Server.Services.HuntingProviders.AlertThreatSearchProvider>();
builder.Services.AddScoped<OneSecurity.Server.Services.HuntingProviders.IncidentThreatSearchProvider>();
builder.Services.AddScoped<OneSecurity.Server.Services.HuntingProviders.AuditLogThreatSearchProvider>();
builder.Services.AddScoped<OneSecurity.Server.Services.HuntingProviders.SecurityEventThreatSearchProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAlertManagementService, AlertManagementService>();
builder.Services.AddScoped<IAlertRuleManagementService, AlertRuleManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

// Register Telegram Notifications
builder.Services.AddHttpClient();
builder.Services.Configure<OneSecurity.Server.Configuration.TelegramOptions>(
    builder.Configuration.GetSection(OneSecurity.Server.Configuration.TelegramOptions.SectionName));
builder.Services.AddScoped<INotificationService, TelegramNotificationService>();
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();
builder.Services.AddHostedService<HeartbeatMonitorService>();

// Bind JWT Options Configuration Section
builder.Services.Configure<OneSecurity.Server.Configuration.JwtOptions>(
    builder.Configuration.GetSection(OneSecurity.Server.Configuration.JwtOptions.SectionName));

// Resolve JwtOptions at runtime for JWT Bearer setup
var jwtSection = builder.Configuration.GetSection(OneSecurity.Server.Configuration.JwtOptions.SectionName);
var issuer = jwtSection.GetValue<string>("Issuer") ?? "OneSecurityServer";
var audience = jwtSection.GetValue<string>("Audience") ?? "OneSecurityDashboard";

// Fail-safe logic: priority environment variable -> dev fallback -> throw in production
var secretKey = Environment.GetEnvironmentVariable("ONESECURITY_JWT_SECRET") ?? jwtSection.GetValue<string>("SecretKey");
if (string.IsNullOrWhiteSpace(secretKey))
{
    var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
                        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    if (isDevelopment)
    {
        Console.WriteLine("WARNING: ONESECURITY_JWT_SECRET environment variable is missing. Falling back to development key!");
        secretKey = "onesecurity_secret_jwt_key_2026_super_long_key_development_only";
    }
    else
    {
        throw new InvalidOperationException("ONESECURITY_JWT_SECRET environment variable is missing in Production environment!");
    }
}
var key = Encoding.ASCII.GetBytes(secretKey);

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS for Local Development
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure AlertRules columns exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LocalAgentDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE alert_rules ADD COLUMN Priority INTEGER DEFAULT 3;");
    }
    catch { }
    try
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE alert_rules ADD COLUMN Category TEXT DEFAULT 'General';");
    }
    catch { }
    try
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE alert_rules ADD COLUMN Version INTEGER DEFAULT 1;");
    }
    catch { }
}

app.UseCors("CorsPolicy");
app.UseRequestDecompression();
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<NotificationHub>("/hubs/security");

// Ensure the database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LocalAgentDbContext>();
        context.Database.Migrate();

        // Initialize Hospital Hierarchy Cache
        var hierarchyCache = services.GetRequiredService<IHospitalHierarchyCache>();
        hierarchyCache.RefreshCache(app.Services);

        // Programmatic Seeding of Identity Roles and Users
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed Roles
        string[] roles = { "Administrator", "Operator", "SecurityOperator", "Viewer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Administrator
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@onesecurity.com",
                FullName = "Super Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HospitalId = null // Admin toàn quyền truy cập
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }

        // Seed Operator
        var opUser = await userManager.FindByNameAsync("operator1");
        if (opUser == null)
        {
            opUser = new ApplicationUser
            {
                UserName = "operator1",
                Email = "operator1@onesecurity.com",
                FullName = "Security Operator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HospitalId = 2 // <--- Gán trực tiếp cho Hospital A (Id: 2)
            };
            var result = await userManager.CreateAsync(opUser, "Admin123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed operator1 user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            await userManager.AddToRoleAsync(opUser, "Operator");
            await userManager.AddToRoleAsync(opUser, "SecurityOperator");
        }

        // Seed Viewer
        var viewerUser = await userManager.FindByNameAsync("viewer1");
        if (viewerUser == null)
        {
            viewerUser = new ApplicationUser
            {
                UserName = "viewer1",
                Email = "viewer1@onesecurity.com",
                FullName = "Security Viewer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HospitalId = 5 // <--- Gán trực tiếp cho Hospital B (Id: 5)
            };
            var result = await userManager.CreateAsync(viewerUser, "Admin123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed viewer1 user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            await userManager.AddToRoleAsync(viewerUser, "Viewer");
        }

        // Seed default AlertRule for Brute force SSH
        if (!context.AlertRules.Any())
        {
            context.AlertRules.Add(new AlertRule
            {
                Name = "SSH Brute Force Attempt",
                EventType = "security",
                ConditionExpression = "{\"severity\":\"Warning\",\"category\":\"auth\",\"source\":\"ssh\",\"keyword\":\"failed\",\"threshold\":3,\"timeWindowSeconds\":60}",
                AlertSeverity = "critical",
                IsEnabled = true,
                Priority = 1,
                Category = "Authentication",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            var progLogger = services.GetRequiredService<ILogger<Program>>();
            progLogger.LogInformation("Seeded default SSH Brute Force AlertRule.");
            var cacheService = services.GetRequiredService<IRuleCacheService>();
            await cacheService.ReloadAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();