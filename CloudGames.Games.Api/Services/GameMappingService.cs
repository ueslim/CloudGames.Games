using CloudGames.Games.Api.DTOs;
using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Api.Services;

public static class GameMappingService
{
    public static Game ToEntity(CreateGameDto dto)
    {
        return new Game
        {
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            Publisher = dto.Publisher,
            ReleaseDate = dto.ReleaseDate,
            Price = dto.Price,
            CoverImageUrl = dto.CoverImageUrl
        };
    }

    public static Game ToEntity(UpdateGameDto dto, Guid id)
    {
        return new Game
        {
            Id = id,
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            Publisher = dto.Publisher,
            ReleaseDate = dto.ReleaseDate,
            Price = dto.Price,
            CoverImageUrl = dto.CoverImageUrl
        };
    }

    public static GameDto ToDto(Game entity)
    {
        return new GameDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Genre = entity.Genre,
            Publisher = entity.Publisher,
            ReleaseDate = entity.ReleaseDate,
            Price = entity.Price,
            CoverImageUrl = entity.CoverImageUrl
        };
    }

    public static IEnumerable<GameDto> ToDto(IEnumerable<Game> entities)
    {
        return entities.Select(ToDto);
    }
}
