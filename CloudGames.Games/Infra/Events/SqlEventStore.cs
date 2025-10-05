using System.Text.Json;

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


