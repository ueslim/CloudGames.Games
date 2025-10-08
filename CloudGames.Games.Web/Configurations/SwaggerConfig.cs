using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace CloudGames.Games.Web.Configurations;

public static class SwaggerConfig
{
    public static void AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CloudGames Games API",
                Description = "CloudGames Games microservice",
                Version = "v1"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Paste only the JWT. The 'Bearer ' prefix will be added automatically.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
        });
    }

    public static void UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger(c =>
        {
            // Ensure correct server URL is emitted when behind reverse proxies like Azure App Service
            c.PreSerializeFilters((swagger, httpReq) =>
            {
                var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
                var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
                var basePath = httpReq.Headers["X-Forwarded-PathBase"].FirstOrDefault() ?? httpReq.PathBase.Value;
                swagger.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{scheme}://{host}{basePath}" }
                };
            });
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudGames Games API v1");
            c.RoutePrefix = "swagger"; // UI at /swagger, spec at /swagger/v1/swagger.json
        });
    }
}


