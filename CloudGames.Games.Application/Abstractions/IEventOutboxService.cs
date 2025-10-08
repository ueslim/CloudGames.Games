namespace CloudGames.Games.Application.Abstractions;

public interface IEventOutboxService
{
    Task AddStoredEventAsync(string type, string payload, DateTime occurredAt, CancellationToken ct = default);
    Task AddOutboxMessageAsync(string type, string payload, DateTime occurredAt, CancellationToken ct = default);
}


