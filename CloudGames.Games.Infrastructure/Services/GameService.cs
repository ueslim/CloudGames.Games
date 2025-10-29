using System.Text.Json;
using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Data;
using CloudGames.Games.Infrastructure.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudGames.Games.Infrastructure.Services;

public class GameService : IGameService
{
    private readonly GamesDbContext _context;
    private readonly ISearchService? _searchService;
    private readonly ILogger<GameService> _logger;

    public GameService(
        GamesDbContext context, 
        ILogger<GameService> logger,
        ISearchService? searchService = null)
    {
        _context = context;
        _logger = logger;
        _searchService = searchService;
    }

    public async Task<IEnumerable<Game>> GetAllGamesAsync()
    {
        return await _context.Games.ToListAsync();
    }

    public async Task<Game?> GetGameByIdAsync(Guid id)
    {
        return await _context.Games.FindAsync(id);
    }

    public async Task<Game> CreateGameAsync(Game game)
    {
        _context.Games.Add(game);

        // Criar evento GameCreated
        var gameCreatedEvent = new
        {
            GameId = game.Id,
            Title = game.Title,
            Price = game.Price,
            CreatedAt = DateTime.UtcNow
        };

        var eventData = JsonSerializer.Serialize(gameCreatedEvent);

        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "GameCreated",
            Payload = eventData,
            OccurredAt = DateTime.UtcNow
        });

        _context.StoredEvents.Add(new StoredEvent
        {
            Id = Guid.NewGuid(),
            Type = "GameCreated",
            Data = eventData,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Métricas
        ApplicationMetrics.GamesCreated.Inc();
        ApplicationMetrics.TotalGames.Inc();

        // Index in Elasticsearch (Development only)
        await IndexGameInElasticsearchAsync(game);

        return game;
    }

    public async Task<Game?> UpdateGameAsync(Guid id, Game game)
    {
        var existing = await _context.Games.FindAsync(id);
        if (existing == null) return null;

        existing.Title = game.Title;
        existing.Description = game.Description;
        existing.Genre = game.Genre;
        existing.Publisher = game.Publisher;
        existing.ReleaseDate = game.ReleaseDate;
        existing.Price = game.Price;
        existing.CoverImageUrl = game.CoverImageUrl;

        await _context.SaveChangesAsync();

        // Re-index in Elasticsearch (Development only)
        await IndexGameInElasticsearchAsync(existing);

        return existing;
    }

    public async Task<bool> DeleteGameAsync(Guid id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null) return false;

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();

        // Métricas
        ApplicationMetrics.TotalGames.Dec();

        // Remove from Elasticsearch index (Development only)
        await DeleteGameFromElasticsearchAsync(id);

        return true;
    }

    public async Task BuyGameAsync(Guid gameId, string userId, decimal paidAmount)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null)
            throw new InvalidOperationException("Jogo não encontrado");

        // Criar evento GamePurchased com o valor efetivamente pago (com desconto se aplicável)
        var gamePurchasedEvent = new
        {
            GameId = gameId,
            UserId = userId,
            PurchasedAt = DateTime.UtcNow,
            Price = paidAmount
        };

        var eventData = JsonSerializer.Serialize(gamePurchasedEvent);

        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "GamePurchased",
            Payload = eventData,
            OccurredAt = DateTime.UtcNow
        });

        _context.StoredEvents.Add(new StoredEvent
        {
            Id = Guid.NewGuid(),
            Type = "GamePurchased",
            Data = eventData,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Métricas
        ApplicationMetrics.GamesPurchased.Inc();
    }

    public async Task<IEnumerable<Game>> GetUserLibraryAsync(string userId)
    {
        // Buscar todos os eventos GamePurchased do usuário
        var purchaseEvents = await _context.StoredEvents
            .Where(e => e.Type == "GamePurchased")
            .ToListAsync();

        // Filtrar por userId e extrair gameIds
        var gameIds = new List<Guid>();
        foreach (var evt in purchaseEvents)
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<JsonElement>(evt.Data);
                if (eventData.TryGetProperty("UserId", out var userIdProp) &&
                    userIdProp.GetString() == userId)
                {
                    if (eventData.TryGetProperty("GameId", out var gameIdProp))
                    {
                        if (gameIdProp.TryGetGuid(out var gameId))
                        {
                            gameIds.Add(gameId);
                        }
                    }
                }
            }
            catch
            {
                // Ignora eventos com JSON inválido
            }
        }

        // Buscar os jogos
        return await _context.Games
            .Where(g => gameIds.Contains(g.Id))
            .ToListAsync();
    }

    // Index game in Elasticsearch if available
    private async Task IndexGameInElasticsearchAsync(Game game)
    {
        if (_searchService is ElasticSearchService elasticService)
        {
            try
            {
                await elasticService.IndexGameAsync(game);
                _logger.LogDebug("Jogo {GameId} indexado no Elasticsearch", game.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao indexar jogo {GameId} no Elasticsearch. A operação continuará normalmente.", game.Id);
            }
        }
    }

    // Delete game from Elasticsearch index if available
    private async Task DeleteGameFromElasticsearchAsync(Guid gameId)
    {
        if (_searchService is ElasticSearchService elasticService)
        {
            try
            {
                await elasticService.DeleteGameAsync(gameId.ToString());
                _logger.LogDebug("Jogo {GameId} removido do índice do Elasticsearch", gameId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao remover jogo {GameId} do índice do Elasticsearch. A operação continuará normalmente.", gameId);
            }
        }
    }
}

