namespace CloudGames.Games.Application.Purchases;

public interface IPurchaseService
{
    Task<Guid> StartPurchaseAsync(Guid gameId, Guid userId, CancellationToken ct = default);
    Task<string> GetPurchaseStatusAsync(Guid paymentId, CancellationToken ct = default);
}


