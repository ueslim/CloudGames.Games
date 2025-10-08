using CloudGames.Games.Application.Models;

namespace CloudGames.Games.Application.Abstractions;

public interface IGamesSearch
{
    Task<IEnumerable<object>> SearchAsync(string query);
    Task<IEnumerable<PopularResult>> GetPopularAsync(int top);
    Task<IEnumerable<RecommendationResult>> GetRecommendationsByProfileAsync(IEnumerable<string> genres, IEnumerable<string> tags, IEnumerable<string> excludeIds, int top);
}


