using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public interface IPurchaseService
{
    Task<Guid> StartPurchaseAsync(Guid gameId, Guid userId, CancellationToken ct = default);
    Task<string> GetPurchaseStatusAsync(Guid purchaseId, CancellationToken ct = default);
}

public class PurchaseService : IPurchaseService
{
    private readonly GamesDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public PurchaseService(GamesDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db; _httpClientFactory = httpClientFactory;
    }

    public async Task<Guid> StartPurchaseAsync(Guid gameId, Guid userId, CancellationToken ct = default)
    {
        var purchaseId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new { PurchaseId = purchaseId, GameId = gameId, UserId = userId });

        _db.OutboxMessages.Add(new OutboxMessage
        {
            Type = GameEventTypes.GamePurchased,
            Payload = payload,
            OccurredAt = DateTime.UtcNow
        });

        _db.StoredEvents.Add(new StoredEvent
        {
            AggregateId = gameId,
            Type = GameEventTypes.GamePurchased,
            Data = payload,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var client = _httpClientFactory.CreateClient("payments");
        var response = await client.PostAsJsonAsync($"/api/payments/purchase", new { purchaseId, gameId, userId }, ct);
        response.EnsureSuccessStatusCode();

        return purchaseId;
    }

    public async Task<string> GetPurchaseStatusAsync(Guid purchaseId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("payments");
        var response = await client.GetAsync($"/api/payments/purchase/{purchaseId}/status", ct);
        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<PurchaseResponseDto>(cancellationToken: ct);
        return status?.Status ?? "unknown";
    }
}


