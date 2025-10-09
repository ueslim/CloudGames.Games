using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Infrastructure.Data;
using CloudGames.Games.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Register DbContext with SQL Server
builder.Services.AddDbContext<GamesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GamesDb")));

// Conditionally register SearchService based on Elastic configuration
var elasticEndpoint = builder.Configuration["Elastic:Endpoint"];
if (!string.IsNullOrEmpty(elasticEndpoint))
{
    // Use Elasticsearch for search (local development)
    builder.Services.AddSingleton<ISearchService, ElasticSearchService>();
}
else
{
    // Use EF Core for search (production/Azure)
    builder.Services.AddScoped<ISearchService, EfSearchService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Prometheus metrics middleware
app.UseHttpMetrics();

// Health check endpoint
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapMetrics();

// Map controllers
app.MapControllers();

app.Run();
