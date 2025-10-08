using CloudGames.Games.Application.Purchases;
using CloudGames.Games.Application.Search;
using CloudGames.Games.Infra.EventStore;
using CloudGames.Games.Infra.Outbox;
using CloudGames.Games.Infra.Persistence;
using CloudGames.Games.Infra.Search;
using CloudGames.Games.Infra.Repositories;
using CloudGames.Games.Infra.Services;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Web.Configurations;

public static class ApiConfig
{
    public static void AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GamesDbContext>(opt =>
            opt.UseSqlServer(configuration.GetConnectionString("GamesDb")));

        services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        services.AddCors(options =>
        {
            options.AddPolicy("frontend",
                builder => builder
                    .WithOrigins("http://localhost:4200", "https://*.azurestaticapps.net")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddHostedService<OutboxPublisher>();
        services.AddScoped<CloudGames.Games.Application.Repositories.IGameReadRepository, GameReadRepository>();
        services.AddScoped<CloudGames.Games.Application.Abstractions.IEventOutboxService, EventOutboxService>();

        // Search abstraction registration is configured in Program based on configuration
    }

    public static void UseApiConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors("frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}


