using System.Text.Json;
using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Infrastructure.Services;

public class GameService : IGameService
{
    private readonly GamesDbContext _context;

    public GameService(GamesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Game>> GetAllGamesAsync()
    {
        return await _context.Games.ToListAsync();
    }

    public async Task<Game?> GetGameByIdAsync(string id)
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
        return game;
    }

    public async Task<Game?> UpdateGameAsync(string id, Game game)
    {
        var existing = await _context.Games.FindAsync(id);
        if (existing == null) return null;

        existing.Title = game.Title;
        existing.Description = game.Description;
        existing.Genre = game.Genre;
        existing.Publisher = game.Publisher;
        existing.ReleaseDate = game.ReleaseDate;
        existing.Price = game.Price;
        existing.Rating = game.Rating;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteGameAsync(string id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null) return false;

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task BuyGameAsync(string gameId, string userId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null)
            throw new InvalidOperationException("Jogo não encontrado");

        // Criar evento GamePurchased
        var gamePurchasedEvent = new
        {
            GameId = gameId,
            UserId = userId,
            PurchasedAt = DateTime.UtcNow,
            Price = game.Price
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
    }

    public async Task<IEnumerable<Game>> GetUserLibraryAsync(string userId)
    {
        // Buscar todos os eventos GamePurchased do usuário
        var purchaseEvents = await _context.StoredEvents
            .Where(e => e.Type == "GamePurchased")
            .ToListAsync();

        // Filtrar por userId e extrair gameIds
        var gameIds = new List<string>();
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
                        var gameId = gameIdProp.GetString();
                        if (!string.IsNullOrEmpty(gameId))
                            gameIds.Add(gameId);
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
}

