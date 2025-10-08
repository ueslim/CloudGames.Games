using CloudGames.Games.Application.Abstractions;
using CloudGames.Games.Infra.Persistence;
using CloudGames.Games.Infra.Persistence.Outbox;
using CloudGames.Games.Infra.Persistence.StoredEvents;

namespace CloudGames.Games.Infra.Services;

public class EventOutboxService : IEventOutboxService
{
    private readonly GamesDbContext _db;

    public EventOutboxService(GamesDbContext db)
    {
        _db = db;
    }

    public async Task AddStoredEventAsync(string type, string payload, DateTime occurredAt, CancellationToken ct = default)
    {
        await _db.Set<StoredEvent>().AddAsync(new StoredEvent { Type = type, Payload = payload, OccurredAt = occurredAt }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddOutboxMessageAsync(string type, string payload, DateTime occurredAt, CancellationToken ct = default)
    {
        await _db.Set<OutboxMessage>().AddAsync(new OutboxMessage { Type = type, Payload = payload, OccurredAt = occurredAt }, ct);
        await _db.SaveChangesAsync(ct);
    }
}


