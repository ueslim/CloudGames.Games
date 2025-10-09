namespace CloudGames.Games.Domain.Entities;

public class StoredEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

