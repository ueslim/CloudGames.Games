namespace CloudGames.Games.Domain.Entities;

public class Promotion
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Game? Game { get; set; }
}

