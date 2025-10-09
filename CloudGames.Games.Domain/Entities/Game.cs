namespace CloudGames.Games.Domain.Entities;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public string? Publisher { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public decimal? Price { get; set; }
    public double? Rating { get; set; }
}

