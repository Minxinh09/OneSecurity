using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.Infrastructure;
using OneSecurity.Collector.Services;
using OneSecurity.Collector.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add controllers support
builder.Services.AddControllers();

// Configure Options Pattern
builder.Services.Configure<CollectorOptions>(builder.Configuration.GetSection("Collector"));

// Register Memory Cache (for Deduplication Service)
builder.Services.AddMemoryCache();

// Register HTTP Client Factory
builder.Services.AddHttpClient();

// Register Services (Singletons as they manage application-wide state / channels)
builder.Services.AddSingleton<IAgentRegistryService, AgentRegistryService>();
builder.Services.AddSingleton<IBatchQueue, ChannelBatchQueue>();
builder.Services.AddSingleton<IDlqService, DlqService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<INormalizationService, NormalizationService>();
builder.Services.AddSingleton<IEnrichmentService, EnrichmentService>();
builder.Services.AddSingleton<IDeduplicationService, DeduplicationService>();
builder.Services.AddSingleton<ICollectorCacheService, CollectorCacheService>();
builder.Services.AddSingleton<CollectorHealthService>();

// Register ForwardService with HttpClient
builder.Services.AddHttpClient<IForwardService, ForwardService>();

// Register Hosted Services (Workers)
builder.Services.AddHostedService<HeartbeatForwardWorker>();
builder.Services.AddHostedService<MetricForwardWorker>();
builder.Services.AddHostedService<SecurityEventForwardWorker>();
builder.Services.AddHostedService<CollectorCacheSyncWorker>();

var app = builder.Build();

// Enable Response Compression if needed, but for simple APIs standard routing is sufficient
app.MapControllers();

app.Run();
