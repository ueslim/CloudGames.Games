public interface IEventStore
{
    Task SaveAsync(StoredEvent @event, CancellationToken ct = default);
}


