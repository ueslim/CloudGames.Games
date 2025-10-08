using CloudGames.Games.Application.Models;
using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Application.Repositories;

public interface IGameReadRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StoredEventDto>> GetRecentPurchasedEventsAsync(int take, CancellationToken ct = default);
    Task<Dictionary<Guid, Game>> GetGamesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}


