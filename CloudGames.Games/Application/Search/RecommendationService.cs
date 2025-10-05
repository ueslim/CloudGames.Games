using Microsoft.EntityFrameworkCore;
using System.Linq;

public interface IRecommendationService
{
    Task<IEnumerable<PopularResult>> GetPopularAsync(int top, CancellationToken ct = default);
    Task<IEnumerable<RecommendationResult>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default);
}

public class RecommendationService : IRecommendationService
{
    private readonly IGamesSearch _search;
    private readonly GamesDbContext _db;

    public RecommendationService(IGamesSearch search, GamesDbContext db)
    {
        _search = search; _db = db;
    }

    public async Task<IEnumerable<PopularResult>> GetPopularAsync(int top, CancellationToken ct = default)
    {
        var list = await _search.GetPopularAsync(top);
        return list;
    }

    public async Task<IEnumerable<RecommendationResult>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        // Fetch user purchase history from StoredEvents
        var events = await _db.StoredEvents
            .Where(e => e.Type == GameEventTypes.GamePurchased)
            .OrderByDescending(e => e.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        var userPurchases = events
            .Select(e => System.Text.Json.JsonDocument.Parse(e.Data))
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
            var purchasedGames = await _db.Games.AsNoTracking()
                .Where(g => guids.Contains(g.Id))
                .ToListAsync(ct);
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


