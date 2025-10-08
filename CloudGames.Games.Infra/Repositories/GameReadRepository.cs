using CloudGames.Games.Application.Models;
using CloudGames.Games.Application.Repositories;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infra.Persistence;
using CloudGames.Games.Infra.Persistence.StoredEvents;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Infra.Repositories;

public class GameReadRepository : IGameReadRepository
{
    private readonly GamesDbContext _db;

    public GameReadRepository(GamesDbContext db)
    {
        _db = db;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<IReadOnlyList<StoredEventDto>> GetRecentPurchasedEventsAsync(int take, CancellationToken ct = default)
    {
        var events = await _db.Set<StoredEvent>()
            .Where(e => e.Type == "GamePurchased")
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .ToListAsync(ct);

        return events.Select(e => new StoredEventDto(e.Type, e.Payload, e.OccurredAt)).ToList();
    }

    public async Task<Dictionary<Guid, Game>> GetGamesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        return await _db.Games.AsNoTracking().Where(g => ids.Contains(g.Id)).ToDictionaryAsync(g => g.Id, ct);
    }
}


