using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Application.Interfaces;

public interface IGameService
{
    Task<IEnumerable<Game>> GetAllGamesAsync();
    Task<Game?> GetGameByIdAsync(Guid id);
    Task<Game> CreateGameAsync(Game game);
    Task<Game?> UpdateGameAsync(Guid id, Game game);
    Task<bool> DeleteGameAsync(Guid id);
    Task BuyGameAsync(Guid gameId, string userId, decimal paidAmount);
    Task<IEnumerable<Game>> GetUserLibraryAsync(string userId);
}

