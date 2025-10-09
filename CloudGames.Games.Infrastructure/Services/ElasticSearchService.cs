using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Nest;

namespace CloudGames.Games.Infrastructure.Services;

public class ElasticSearchService : ISearchService
{
    private readonly IElasticClient _elasticClient;
    private readonly string _indexName;

    public ElasticSearchService(IConfiguration configuration)
    {
        var endpoint = configuration["Elastic:Endpoint"];
        _indexName = configuration["Elastic:IndexName"] ?? "games";

        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("Elastic:Endpoint configuration is required for ElasticSearchService");
        }

        var settings = new ConnectionSettings(new Uri(endpoint))
            .DefaultIndex(_indexName)
            .DefaultMappingFor<Game>(m => m
                .IndexName(_indexName)
                .IdProperty(p => p.Id)
            );

        _elasticClient = new ElasticClient(settings);
        
        // Ensure the index exists with proper mappings
        EnsureIndexExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexExistsAsync()
    {
        var indexExists = await _elasticClient.Indices.ExistsAsync(_indexName);
        
        if (!indexExists.Exists)
        {
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
                        .Number(n => n.Name(n => n.Rating).Type(NumberType.Double))
                    )
                )
            );

            if (!createIndexResponse.IsValid)
            {
                throw new InvalidOperationException($"Failed to create Elasticsearch index: {createIndexResponse.DebugInformation}");
            }
        }
    }

    public async Task<IEnumerable<Game>> SearchGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
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
                throw new InvalidOperationException($"Elasticsearch query failed: {searchResponse.DebugInformation}");
            }

            return searchResponse.Documents;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error searching games in Elasticsearch: {ex.Message}", ex);
        }
    }

    public async Task IndexGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.IndexDocumentAsync(game, cancellationToken);
        
        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to index game: {response.DebugInformation}");
        }
    }

    public async Task IndexGamesAsync(IEnumerable<Game> games, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.IndexManyAsync(games, _indexName, cancellationToken);
        
        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to bulk index games: {response.DebugInformation}");
        }
    }

    public async Task DeleteGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.DeleteAsync<Game>(gameId, d => d.Index(_indexName), cancellationToken);
        
        if (!response.IsValid && response.Result != Result.NotFound)
        {
            throw new InvalidOperationException($"Failed to delete game from index: {response.DebugInformation}");
        }
    }
}

