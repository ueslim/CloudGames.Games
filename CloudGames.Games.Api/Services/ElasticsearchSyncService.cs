using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Infrastructure.Services;

namespace CloudGames.Games.Api.Services;

public class ElasticsearchSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ElasticsearchSyncService> _logger;

    public ElasticsearchSyncService(
        IServiceProvider serviceProvider,
        ILogger<ElasticsearchSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var searchService = scope.ServiceProvider.GetService<ISearchService>();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

        if (searchService is not ElasticSearchService elasticService)
        {
            _logger.LogInformation("Elasticsearch not configured - sync skipped");
            return;
        }

        try
        {
            var games = await gameService.GetAllGamesAsync();
            var gamesList = games.ToList();

            if (gamesList.Count == 0)
            {
                _logger.LogInformation("No games in database - sync skipped");
                return;
            }

            await elasticService.IndexGamesAsync(gamesList, stoppingToken);
            _logger.LogInformation("Elasticsearch auto-sync completed: {Count} games indexed", gamesList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch auto-sync failed");
        }
    }
}

