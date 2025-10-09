namespace CloudGames.Games.Domain.Entities;

public class Promotion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Game? Game { get; set; }
}

