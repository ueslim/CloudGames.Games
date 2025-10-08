using CloudGames.Games.Application.Abstractions;
using CloudGames.Games.Application.Models;
using CloudGames.Games.Application.Repositories;

namespace CloudGames.Games.Application.Search;

public class RecommendationService : IRecommendationService
{
    private readonly IGamesSearch _search;
    private readonly IGameReadRepository _games;

    public RecommendationService(IGamesSearch search, IGameReadRepository games)
    {
        _search = search; _games = games;
    }

    public async Task<IEnumerable<PopularResult>> GetPopularAsync(int top, CancellationToken ct = default)
    {
        var list = await _search.GetPopularAsync(top);
        return list;
    }

    public async Task<IEnumerable<RecommendationResult>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        var events = await _games.GetRecentPurchasedEventsAsync(500, ct);

        var userPurchases = events
            .Select(e => System.Text.Json.JsonDocument.Parse(e.Payload))
            .Where(doc => doc.RootElement.TryGetProperty("UserId", out var uid) && uid.GetGuid() == userId)
            .ToList();

        var purchasedGameIds = userPurchases
            .Select(doc => doc.RootElement.GetProperty("GameId").GetGuid().ToString())
            .Distinct()
            .ToList();

        var genres = new List<string>();
        var tags = new List<string>();
        if (purchasedGameIds.Count > 0)
        {
            var guids = purchasedGameIds.Select(Guid.Parse).ToList();
            var purchasedGamesDict = await _games.GetGamesByIdsAsync(guids, ct);
            var purchasedGames = purchasedGamesDict.Values.ToList();
            genres = purchasedGames.Select(g => g.Genre).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            foreach (var g in purchasedGames)
            {
                try
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(g.TagsJson) ?? Array.Empty<string>();
                    foreach (var t in arr) if (!string.IsNullOrWhiteSpace(t)) tags.Add(t);
                }
                catch { }
            }
            tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        var recs = await _search.GetRecommendationsByProfileAsync(genres, tags, purchasedGameIds, 10);
        return recs;
    }
}


