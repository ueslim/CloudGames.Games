namespace CloudGames.Games.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public string TagsJson { get; set; } = "[]";
}


