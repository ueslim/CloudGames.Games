using System.ComponentModel.DataAnnotations;
using CloudGames.Games.Api.Validation;

namespace CloudGames.Games.Api.DTOs;

public class CreateGameDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Genre { get; set; }

    [MaxLength(200)]
    public string? Publisher { get; set; }

    public DateTime? ReleaseDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal? Price { get; set; }

    [MaxLength(500)]
    [OptionalUrl]
    public string? CoverImageUrl { get; set; }
}
