using CloudGames.Games.Api.Middleware;
using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Infrastructure.Data;
using CloudGames.Games.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtAuthority = builder.Configuration["Jwt:Authority"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;

        if (!string.IsNullOrWhiteSpace(jwtKey))
        {
            // Development Mode: Symmetric Key Validation
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
        else if (!string.IsNullOrWhiteSpace(jwtAuthority))
        {
            // Production Mode: Azure AD Validation
            options.RequireHttpsMetadata = true;
            options.Authority = jwtAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
        }
        else
        {
            throw new InvalidOperationException(
                "JWT configuration is missing. Please configure either:\n" +
                "  - Development: Jwt:Key, Jwt:Issuer, Jwt:Audience (symmetric key)\n" +
                "  - Production: Jwt:Authority, Jwt:Audience (Azure AD)");
        }
    });

builder.Services.AddAuthorization();

// Swagger Configuration with JWT Bearer Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CloudGames - API de Jogos",
        Version = "v1",
        Description = "Microserviço de gerenciamento de jogos com autenticação JWT (desenvolvimento e produção via Azure AD)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação JWT usando o esquema Bearer. Insira 'Bearer' [espaço] e então seu token no campo abaixo.\n\n" +
                      "Exemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Register SearchService - ElasticSearch in Development, EF fallback in Production or if Elastic is not configured
var elasticEndpoint = builder.Configuration["Elastic:Endpoint"];
var elasticIndexName = builder.Configuration["Elastic:IndexName"];

if (!string.IsNullOrEmpty(elasticEndpoint))
{
    // ElasticSearch is configured - register it
    // The service itself will handle connection failures gracefully
    builder.Services.AddSingleton<ISearchService, ElasticSearchService>();
    Log.Information("Search Service: ElasticSearchService configured");
    Log.Information("Endpoint: {ElasticEndpoint}", elasticEndpoint);
    Log.Information("Index: {IndexName}", elasticIndexName ?? "games");
    Log.Information("Requires Elasticsearch running (Docker: docker-compose up -d)");
}
else
{
    // No ElasticSearch configured - use EF-based search as fallback
    builder.Services.AddScoped<ISearchService, EfSearchService>();
    Log.Information("Search Service: EfSearchService (Entity Framework)");
    Log.Information("Using SQL Server for search queries");
    Log.Information("No additional infrastructure required");
}

// Register application services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

#if DEBUG
// Register Elasticsearch auto-sync service (Development only)
builder.Services.AddHostedService<CloudGames.Games.Api.Services.ElasticsearchSyncService>();
#endif

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();

app.MapHealthChecks("/health");
app.MapMetrics();
app.MapControllers();

app.Run();
