namespace CloudGames.Games.Api.DTOs;

public class GameDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public string? Publisher { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public decimal? Price { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
}
