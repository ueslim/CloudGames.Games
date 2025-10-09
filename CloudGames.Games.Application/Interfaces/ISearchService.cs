using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Application.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<Game>> SearchGamesAsync(string query, CancellationToken cancellationToken = default);
}

