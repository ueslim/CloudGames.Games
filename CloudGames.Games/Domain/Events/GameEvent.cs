public class GameEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class GameEventTypes
{
    public const string GameCreated = "GameCreated";
    public const string GameUpdated = "GameUpdated";
    public const string GameDeleted = "GameDeleted";
    public const string GamePurchased = "GamePurchased";
}


