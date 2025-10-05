using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public interface IGamesSearch
{
    Task<IEnumerable<object>> SearchAsync(string query);
    Task<IEnumerable<PopularResult>> GetPopularAsync(int top);
    Task<IEnumerable<RecommendationResult>> GetRecommendationsByProfileAsync(IEnumerable<string> genres, IEnumerable<string> tags, IEnumerable<string> excludeIds, int top);
}

public class AzureSearchGamesSearch : IGamesSearch
{
    private readonly SearchClient _client;
    public AzureSearchGamesSearch(IConfiguration configuration)
    {
        var endpoint = new Uri(configuration["Search:Endpoint"]!);
        var apiKey = new AzureKeyCredential(configuration["Search:ApiKey"]!);
        var indexName = configuration["Search:IndexName"] ?? "games";
        _client = new SearchClient(endpoint, indexName, apiKey);
    }

    public async Task<IEnumerable<object>> SearchAsync(string query)
    {
        var resp = await _client.SearchAsync<SearchDocument>(string.IsNullOrWhiteSpace(query) ? "*" : query);
        var results = new List<object>();
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            results.Add(r.Document);
        }
        return results;
    }

    public async Task<IEnumerable<PopularResult>> GetPopularAsync(int top)
    {
        var options = new SearchOptions
        {
            Size = top
        };
        options.OrderBy.Add("purchaseCount desc");
        options.Select.Add("id");
        options.Select.Add("title");
        options.Select.Add("genre");
        options.Select.Add("price");
        options.Select.Add("purchaseCount");

        var resp = await _client.SearchAsync<SearchDocument>("*", options);
        var list = new List<PopularResult>();
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            var doc = r.Document;
            list.Add(new PopularResult(
                doc.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("title", out var title) ? title?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("genre", out var genre) ? genre?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("price", out var price) ? ConvertToDecimal(price) : null,
                doc.TryGetValue("purchaseCount", out var pc) ? ConvertToInt(pc) : null,
                r.Score
            ));
        }
        return list;
    }

    public async Task<IEnumerable<RecommendationResult>> GetRecommendationsByProfileAsync(IEnumerable<string> genres, IEnumerable<string> tags, IEnumerable<string> excludeIds, int top)
    {
        var searchTextParts = new List<string>();
        if (genres.Any()) searchTextParts.Add(string.Join(" OR ", genres.Select(g => $"\"{g}\"")));
        if (tags.Any()) searchTextParts.Add(string.Join(" OR ", tags.Select(t => $"\"{t}\"")));
        var searchText = searchTextParts.Count == 0 ? "*" : string.Join(" OR ", searchTextParts);

        var options = new SearchOptions
        {
            Size = top
        };
        options.SearchFields.Add("genre");
        options.SearchFields.Add("tags");
        if (excludeIds.Any())
        {
            var exclude = string.Join(",", excludeIds.Select(id => $"'{id}'"));
            options.Filter = $"not(id in ({exclude}))";
        }
        options.OrderBy.Add("purchaseCount desc");
        options.Select.Add("id");
        options.Select.Add("title");
        options.Select.Add("genre");
        options.Select.Add("price");
        options.Select.Add("purchaseCount");
        options.Select.Add("tags");

        var resp = await _client.SearchAsync<SearchDocument>(searchText, options);
        var list = new List<RecommendationResult>();
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            var doc = r.Document;
            var reason = ReasonFromMatch(doc, genres, tags);
            list.Add(new RecommendationResult(
                doc.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("title", out var title) ? title?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("genre", out var genre) ? genre?.ToString() ?? string.Empty : string.Empty,
                doc.TryGetValue("price", out var price) ? ConvertToDecimal(price) : null,
                doc.TryGetValue("purchaseCount", out var pc) ? ConvertToInt(pc) : null,
                r.Score,
                reason
            ));
        }
        return list;
    }

    private static decimal? ConvertToDecimal(object value)
    {
        if (value == null) return null;
        if (value is decimal d) return d;
        if (value is double dbl) return (decimal)dbl;
        if (decimal.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static int? ConvertToInt(object value)
    {
        if (value == null) return null;
        if (value is int i) return i;
        if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static string ReasonFromMatch(SearchDocument doc, IEnumerable<string> genres, IEnumerable<string> tags)
    {
        var matched = new List<string>();
        if (doc.TryGetValue("genre", out var genre) && genres.Contains(genre?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            matched.Add($"genre:{genre}");
        }
        if (doc.TryGetValue("tags", out var tagsObj) && tagsObj is IEnumerable<object> tagArr)
        {
            var docTags = tagArr.Select(t => t?.ToString() ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var overlap = tags.Where(t => docTags.Contains(t));
            if (overlap.Any()) matched.Add($"tags:{string.Join(',', overlap)}");
        }
        return matched.Count > 0 ? string.Join("; ", matched) : "popular";
    }
}

public class NoopGamesSearch : IGamesSearch
{
    private readonly GamesDbContext _db;
    public NoopGamesSearch(GamesDbContext db) { _db = db; }

    public async Task<IEnumerable<object>> SearchAsync(string query)
    {
        var q = (query ?? string.Empty).Trim();
        var baseQuery = _db.Games.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(g => g.Title.Contains(q) || g.Description.Contains(q));
        }
        var items = await baseQuery
            .Select(g => new { g.Id, g.Title, g.Price, g.Genre })
            .ToListAsync();
        return items;
    }

    public async Task<IEnumerable<PopularResult>> GetPopularAsync(int top)
    {
        // Fallback: approximate popularity by most recent events count (requires joins)
        var counts = await _db.StoredEvents
            .Where(e => e.Type == GameEventTypes.GamePurchased)
            .GroupBy(e => e.AggregateId)
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync();

        var ids = counts.Select(c => c.GameId).ToList();
        var games = await _db.Games.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id);

        return counts.Select(c =>
        {
            var g = games[c.GameId];
            return new PopularResult(c.GameId.ToString(), g.Title, g.Genre, g.Price, c.Count, null);
        });
    }

    public async Task<IEnumerable<RecommendationResult>> GetRecommendationsByProfileAsync(IEnumerable<string> genres, IEnumerable<string> tags, IEnumerable<string> excludeIds, int top)
    {
        var exclude = excludeIds.Select(id => Guid.Parse(id)).ToHashSet();
        var query = _db.Games.AsNoTracking().Where(g => !exclude.Contains(g.Id));
        if (genres.Any()) query = query.Where(g => genres.Contains(g.Genre));
        // tags naive filter
        if (tags.Any()) query = query.Where(g => tags.Any(t => g.TagsJson.Contains(t)));
        var list = await query.OrderByDescending(g => g.Price).Take(top).ToListAsync();
        return list.Select(g => new RecommendationResult(g.Id.ToString(), g.Title, g.Genre, g.Price, null, null, "content-match"));
    }
}
