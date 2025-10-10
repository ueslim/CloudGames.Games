using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Infrastructure.Services;

namespace CloudGames.Games.Api.Services;

#if DEBUG
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
        await Task.Delay(2000, stoppingToken); // Wait for API to start

        using var scope = _serviceProvider.CreateScope();
        var searchService = scope.ServiceProvider.GetService<ISearchService>();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

        if (searchService is not ElasticSearchService elasticService)
        {
            _logger.LogInformation("Elasticsearch não configurado. Sync automático ignorado.");
            return;
        }

        try
        {
            var games = await gameService.GetAllGamesAsync();
            var gamesList = games.ToList();

            if (gamesList.Count == 0)
            {
                _logger.LogInformation("Nenhum jogo no banco de dados. Sync ignorado.");
                return;
            }

            await elasticService.IndexGamesAsync(gamesList, stoppingToken);
            _logger.LogInformation("Elasticsearch sync automático: {Count} jogos indexados", gamesList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no sync automático do Elasticsearch");
        }
    }
}
#endif

