using CloudGames.Games.Infra.Persistence;
using CloudGames.Games.Infra.Persistence.StoredEvents;

namespace CloudGames.Games.Infra.EventStore;

public class SqlEventStore : IEventStore
{
    private readonly GamesDbContext _db;
    public SqlEventStore(GamesDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(StoredEvent @event, CancellationToken ct = default)
    {
        _db.StoredEvents.Add(@event);
        await _db.SaveChangesAsync(ct);
    }
}


