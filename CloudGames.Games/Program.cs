using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.ConfigureSerilogWithOpenTelemetry("CloudGames.Games");

builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins("http://localhost:4200", "https://*.azurestaticapps.net")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddApiConfiguration(config);

// Queue publisher for game events
builder.Services.AddSingleton(sp =>
{
    var cs = config.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true";
    return new QueueClient(cs, config["Queues:Games"] ?? "games-events");
});

builder.Services.AddJwtConfiguration(config);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("CloudGames.Games"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation());

// Search abstraction registration
var endpointStr = config["Search:Endpoint"] ?? string.Empty;
var apiKeyStr = config["Search:ApiKey"] ?? string.Empty;
if (Uri.TryCreate(endpointStr, UriKind.Absolute, out var endpoint) && endpoint.Scheme == Uri.UriSchemeHttps && !string.IsNullOrWhiteSpace(apiKeyStr))
{
    builder.Services.AddSingleton<IGamesSearch, AzureSearchGamesSearch>();
}
else
{
    builder.Services.AddScoped<IGamesSearch, NoopGamesSearch>();
}

builder.Services.AddHttpClient("payments", c =>
{
    c.BaseAddress = new Uri(config["Payments:BaseUrl"] ?? "http://localhost:5003");
});

builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IEventStore, SqlEventStore>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwaggerConfiguration();

app.MapGet("/health", () => Results.Ok("ok"));

app.UseApiConfiguration(app.Environment);

app.Run();

// moved to Domain and Infra
