using CloudGames.Games.Infra.Persistence.StoredEvents;

namespace CloudGames.Games.Infra.EventStore;

public interface IEventStore
{
    Task SaveAsync(StoredEvent @event, CancellationToken ct = default);
}


