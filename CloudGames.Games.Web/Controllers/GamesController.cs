using CloudGames.Games.Application.Abstractions;
using CloudGames.Games.Application.Purchases;
using CloudGames.Games.Application.Search;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infra.Persistence;
using CloudGames.Games.Infra.Persistence.Outbox;
using CloudGames.Games.Infra.Persistence.StoredEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CloudGames.Games.Web.Controllers;

[ApiController]
[Route("/api/games")]
public class GamesController : ControllerBase
{
    private readonly GamesDbContext _db;
    private readonly IGamesSearch _search;
    private readonly IPurchaseService _purchaseService;
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(GamesDbContext db, IGamesSearch search, IPurchaseService purchaseService, IRecommendationService recommendationService, ILogger<GamesController> logger)
    {
        _db = db; _search = search; _purchaseService = purchaseService; _recommendationService = recommendationService; _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var games = await _db.Games.AsNoTracking().Select(g => new { g.Id, g.Title, g.Price, g.Genre }).ToListAsync();
        return Ok(games);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var game = await _db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (game == null) return NotFound();
        return Ok(game);
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var results = await _search.SearchAsync(q);
        return Ok(results);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameDto dto)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Developer = dto.Developer,
            Publisher = dto.Publisher,
            ReleaseDate = dto.ReleaseDate,
            Genre = dto.Genre,
            Price = dto.Price,
            CoverImageUrl = dto.CoverImageUrl,
            TagsJson = JsonSerializer.Serialize(dto.Tags ?? Array.Empty<string>())
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        await AppendEventAndOutboxAsync("GameCreated", game);
        return CreatedAtAction(nameof(Get), new { id = game.Id }, game);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGameDto dto)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();
        if (dto.Title != null) game.Title = dto.Title;
        if (dto.Description != null) game.Description = dto.Description;
        if (dto.Developer != null) game.Developer = dto.Developer;
        if (dto.Publisher != null) game.Publisher = dto.Publisher;
        if (dto.ReleaseDate.HasValue) game.ReleaseDate = dto.ReleaseDate.Value;
        if (dto.Genre != null) game.Genre = dto.Genre;
        if (dto.Price.HasValue) game.Price = dto.Price.Value;
        if (dto.CoverImageUrl != null) game.CoverImageUrl = dto.CoverImageUrl;
        if (dto.Tags != null) game.TagsJson = JsonSerializer.Serialize(dto.Tags);
        await _db.SaveChangesAsync();
        await AppendEventAndOutboxAsync("GameUpdated", game);
        return Ok(game);
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        await AppendEventAndOutboxAsync("GameDeleted", new { id });
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:guid}/purchase")]
    public async Task<IActionResult> Purchase(Guid id)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        try
        {
            _logger.LogInformation("User {UserId} initiating purchase for game {GameId}.", userId, id);
            var paymentId = await _purchaseService.StartPurchaseAsync(id, userId);
            return Accepted(new { paymentId });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadGateway || ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(ex, "Payments service unavailable when purchasing game {GameId} for user {UserId}.", id, userId);
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { error = "Payments service unavailable" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Payments for game {GameId} and user {UserId}.", id, userId);
            return StatusCode(502, new { error = "Payments service unavailable" });
        }
    }

    [Authorize]
    [HttpGet("{id:guid}/purchase/{purchaseId:guid}/status")]
    public async Task<IActionResult> PurchaseStatus(Guid id, Guid purchaseId)
    {
        var status = await _purchaseService.GetPurchaseStatusAsync(purchaseId);
        return Ok(new { paymentId = purchaseId, status });
    }

    [AllowAnonymous]
    [HttpGet("popular")]
    public async Task<IActionResult> Popular([FromQuery] int top = 10)
    {
        var items = await _recommendationService.GetPopularAsync(top);
        return Ok(items);
    }

    [Authorize]
    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations([FromQuery] Guid userId)
    {
        var items = await _recommendationService.GetRecommendationsAsync(userId);
        return Ok(items);
    }

    private async Task AppendEventAndOutboxAsync(string type, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        _db.StoredEvents.Add(new StoredEvent { Type = type, Payload = json, OccurredAt = DateTime.UtcNow });
        _db.OutboxMessages.Add(new OutboxMessage { Type = type, Payload = json, OccurredAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }
}

public record CreateGameDto(string Title, string Description, string Developer, string Publisher, DateTime ReleaseDate, string Genre, decimal Price, string CoverImageUrl, string[]? Tags);
public record UpdateGameDto(string? Title, string? Description, string? Developer, string? Publisher, DateTime? ReleaseDate, string? Genre, decimal? Price, string? CoverImageUrl, string[]? Tags);


