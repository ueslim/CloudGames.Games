using CloudGames.Games.Application.Models;

namespace CloudGames.Games.Application.Search;

public interface IRecommendationService
{
    Task<IEnumerable<PopularResult>> GetPopularAsync(int top, CancellationToken ct = default);
    Task<IEnumerable<RecommendationResult>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default);
}


