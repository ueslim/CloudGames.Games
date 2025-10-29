using CloudGames.Games.Api.Middleware;
using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Infrastructure.Data;
using CloudGames.Games.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// Authentication is handled by API Management (APIM) in production
// No authentication middleware is required in the microservice
Log.Information("Authentication: Disabled - APIM handles authentication and authorization");
Log.Information("Security: All endpoints are accessible without JWT validation in this microservice");

// Swagger Configuration - No authentication required (APIM handles it)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CloudGames - API de Jogos",
        Version = "v1",
        Description = "Microserviço de gerenciamento de jogos. Autenticação é gerenciada pelo API Management (APIM)."
    });

    // Incluir comentários XML
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register DbContext with SQL Server
builder.Services.AddDbContext<GamesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GamesDb")));

// Register SearchService - Elasticsearch only in Development, EF in Production
var elasticEndpoint = builder.Configuration["Elastic:Endpoint"];
var isDevelopment = builder.Environment.IsDevelopment();

if (isDevelopment && !string.IsNullOrEmpty(elasticEndpoint))
{
    // Development with Elasticsearch
    builder.Services.AddSingleton<ISearchService, ElasticSearchService>();
    builder.Services.AddHostedService<CloudGames.Games.Api.Services.ElasticsearchSyncService>();
    Log.Information("Search: Elasticsearch - {Endpoint}", elasticEndpoint);
}
else
{
    // Production or Development without Elasticsearch
    builder.Services.AddScoped<ISearchService, EfSearchService>();
    
    if (!isDevelopment && !string.IsNullOrEmpty(elasticEndpoint))
    {
        Log.Warning("Elasticsearch config ignored in Production - using EF Search");
    }
    else
    {
        Log.Information("Search: Entity Framework (SQL Server)");
    }
}

// Register application services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

// Register HttpClient for Payments API
builder.Services.AddHttpClient("PaymentsApi", client =>
{
    var paymentsUrl = builder.Configuration["Services:PaymentsApi"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(paymentsUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Azure Storage Queue for payment events
builder.Services.AddSingleton(sp =>
{
    var storageConn = builder.Configuration.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true";
    var queueName = builder.Configuration["Queues:Payments"] ?? "payments-events";
    return new Azure.Storage.Queues.QueueClient(storageConn, queueName, new Azure.Storage.Queues.QueueClientOptions
    {
        MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64
    });
});

// Register Payment Event Consumer background service
builder.Services.AddHostedService<CloudGames.Games.Api.Services.PaymentEventConsumer>();

var app = builder.Build();

await DatabaseInitializer.EnsureDataBaseMigratedAsync(app.Services);

// Configure middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

// Enable Swagger in all environments for APIM integration
app.UseSwagger();

// Swagger UI only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHttpMetrics();

app.MapHealthChecks("/health");
app.MapMetrics();
app.MapControllers();

app.Run();
