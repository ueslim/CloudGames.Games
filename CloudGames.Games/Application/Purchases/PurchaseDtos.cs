public record PurchaseRequestDto(Guid GameId, Guid UserId);
public record PurchaseResponseDto(Guid PurchaseId, string Status);


