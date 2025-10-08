using Azure.Search.Documents.Indexes;
using Azure.Storage.Queues;
using CloudGames.Games.Web.Configurations;
using CloudGames.Games.Application.Abstractions;
using CloudGames.Games.Infra.Search;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
// Bind to localhost in development only; let Azure/App Service configure ports/urls in production
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5002");
}
var config = builder.Configuration;

builder.ConfigureSerilogWithOpenTelemetry("CloudGames.Games");

builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins("http://localhost:4200", "https://*.azurestaticapps.net")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddApiConfiguration(config);

builder.Services.AddJwtConfiguration(config);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();

builder.Services.AddHttpContextAccessor();

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

// Queue publisher for game events
builder.Services.AddSingleton(sp =>
{
    var cs = config.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true";
    var queueName = config["Queues:Games"] ?? "games-events";
    return new QueueClient(cs, queueName);
});

var app = builder.Build();

// Ensure correct scheme/host when behind Azure App Service / reverse proxies
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseSwaggerConfiguration();

app.MapGet("/health", () => Results.Ok("ok"));

app.UseApiConfiguration(app.Environment);

app.Run();
