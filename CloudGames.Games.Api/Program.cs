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
        Title = "CloudGames Games API",
        Version = "v1",
        Description = "Games microservice with dual JWT authentication support"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
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
});

// Register DbContext with SQL Server
builder.Services.AddDbContext<GamesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GamesDb")));

// Conditionally register SearchService based on Elastic configuration
var elasticEndpoint = builder.Configuration["Elastic:Endpoint"];
if (!string.IsNullOrEmpty(elasticEndpoint))
{
    builder.Services.AddSingleton<ISearchService, ElasticSearchService>();
}
else
{
    builder.Services.AddScoped<ISearchService, EfSearchService>();
}

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
