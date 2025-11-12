using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

namespace CloudGames.Games.Infrastructure.Services;

public class ElasticSearchService : ISearchService
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ElasticSearchService> _logger;
    private readonly string _indexName;
    private bool _isAvailable;

    public ElasticSearchService(IConfiguration configuration, ILogger<ElasticSearchService> logger)
    {
        _logger = logger;
        var endpoint = configuration["Elastic:Endpoint"];
        _indexName = configuration["Elastic:IndexName"] ?? "games";
        _isAvailable = false;

        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("Elastic:Endpoint configuration is missing. ElasticSearch features will be disabled.");
            _elasticClient = null!;
            return;
        }

        try
        {
            var settings = new ConnectionSettings(new Uri(endpoint))
                .DefaultIndex(_indexName)
                .DefaultMappingFor<Game>(m => m
                    .IndexName(_indexName)
                    .IdProperty(p => p.Id)
                )
                .DisableDirectStreaming() // Helps with debugging
                .RequestTimeout(TimeSpan.FromSeconds(30)); // Timeout increased to wait for Elasticsearch startup

            _elasticClient = new ElasticClient(settings);
            
            // Ensure the index exists with proper mappings (with retry logic)
            EnsureIndexExistsAsync().GetAwaiter().GetResult();
            _logger.LogInformation("ElasticSearch service initialized successfully with endpoint: {Endpoint}, index: {IndexName}", endpoint, _indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ElasticSearch service. Search features will be unavailable.");
            _elasticClient = null!;
            _isAvailable = false;
        }
    }

    private async Task EnsureIndexExistsAsync()
    {
        const int maxRetries = 10;
        const int delaySeconds = 3;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Checking Elasticsearch availability... (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                var pingResponse = await _elasticClient.PingAsync();
                if (!pingResponse.IsValid)
                {
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("Failed to ping Elasticsearch (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s...", attempt, maxRetries, delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        continue;
                    }
                    _logger.LogWarning("Failed to ping Elasticsearch after {MaxRetries} attempts: {Error}", maxRetries, pingResponse.DebugInformation);
                    _isAvailable = false;
                    return;
                }
                _logger.LogInformation("Elasticsearch ping successful");
                break;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Error pinging Elasticsearch (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s...", attempt, maxRetries, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    continue;
                }
                _logger.LogError(ex, "Error pinging Elasticsearch after {MaxRetries} attempts", maxRetries);
                _isAvailable = false;
                return;
            }
        }
        
        try
        {
            var indexExists = await _elasticClient.Indices.ExistsAsync(_indexName);
            _logger.LogInformation("Index '{IndexName}' exists: {Exists}", _indexName, indexExists.Exists);
            
            if (!indexExists.Exists)
            {
                _logger.LogInformation("Creating Elasticsearch index: {IndexName}", _indexName);
                
                var createIndexResponse = await _elasticClient.Indices.CreateAsync(_indexName, c => c
                    .Map<Game>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t.Name(n => n.Title).Analyzer("standard"))
                            .Text(t => t.Name(n => n.Description).Analyzer("standard"))
                            .Keyword(k => k.Name(n => n.Genre))
                            .Keyword(k => k.Name(n => n.Publisher))
                            .Date(d => d.Name(n => n.ReleaseDate))
                            .Number(n => n.Name(n => n.Price).Type(NumberType.ScaledFloat).ScalingFactor(100))
                            .Text(t => t.Name(n => n.CoverImageUrl))
                        )
                    )
                );

                if (!createIndexResponse.IsValid)
                {
                    _logger.LogError("Failed to create Elasticsearch index: {Error}", createIndexResponse.DebugInformation);
                    _isAvailable = false;
                    return;
                }
                
                _logger.LogInformation("Successfully created Elasticsearch index: {IndexName}", _indexName);
            }
            else
            {
                _logger.LogInformation("Elasticsearch index '{IndexName}' already exists", _indexName);
            }
            
            _isAvailable = true;
            _logger.LogInformation("Elasticsearch marked as AVAILABLE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring Elasticsearch index exists");
            _isAvailable = false;
        }
    }

    public async Task<IEnumerable<Game>> SearchGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Game>();
        }

        if (!_isAvailable || _elasticClient == null)
        {
            _logger.LogWarning("Elasticsearch is not available. Search cannot be performed.");
            return Enumerable.Empty<Game>();
        }

        try
        {
            var searchResponse = await _elasticClient.SearchAsync<Game>(s => s
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query)
                        .Fields(f => f
                            .Field(p => p.Title, boost: 2.0)
                            .Field(p => p.Description)
                            .Field(p => p.Genre)
                            .Field(p => p.Publisher)
                        )
                        .Type(TextQueryType.BestFields)
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .Size(50)
            , cancellationToken);

            if (!searchResponse.IsValid)
            {
                _logger.LogError("Elasticsearch query failed: {Error}", searchResponse.DebugInformation);
                return Enumerable.Empty<Game>();
            }

            return searchResponse.Documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching games in Elasticsearch");
            return Enumerable.Empty<Game>();
        }
    }

    public async Task IndexGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        if (!_isAvailable || _elasticClient == null)
        {
            _logger.LogWarning("Elasticsearch is not available. Cannot index game: {GameId}", game.Id);
            return;
        }

        try
        {
            var response = await _elasticClient.IndexDocumentAsync(game, cancellationToken);
            
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index game {GameId}: {Error}", game.Id, response.DebugInformation);
            }
            else
            {
                _logger.LogDebug("Successfully indexed game {GameId}", game.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing game {GameId}", game.Id);
        }
    }

    public async Task IndexGamesAsync(IEnumerable<Game> games, CancellationToken cancellationToken = default)
    {
        if (!_isAvailable || _elasticClient == null)
        {
            _logger.LogWarning("Elasticsearch is not available. Cannot bulk index games.");
            return;
        }

        try
        {
            var gamesList = games.ToList();
            var response = await _elasticClient.IndexManyAsync(gamesList, _indexName, cancellationToken);
            
            if (!response.IsValid)
            {
                _logger.LogError("Failed to bulk index games: {Error}", response.DebugInformation);
            }
            else
            {
                _logger.LogInformation("Successfully indexed {Count} games", gamesList.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing games");
        }
    }

    public async Task DeleteGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        if (!_isAvailable || _elasticClient == null)
        {
            _logger.LogWarning("Elasticsearch is not available. Cannot delete game: {GameId}", gameId);
            return;
        }

        try
        {
            var response = await _elasticClient.DeleteAsync<Game>(gameId, d => d.Index(_indexName), cancellationToken);
            
            if (!response.IsValid && response.Result != Result.NotFound)
            {
                _logger.LogError("Failed to delete game {GameId} from index: {Error}", gameId, response.DebugInformation);
            }
            else
            {
                _logger.LogDebug("Successfully deleted game {GameId} from index", gameId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game {GameId} from index", gameId);
        }
    }
}

