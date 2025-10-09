using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Application.Interfaces;

public interface IGameService
{
    Task<IEnumerable<Game>> GetAllGamesAsync();
    Task<Game?> GetGameByIdAsync(string id);
    Task<Game> CreateGameAsync(Game game);
    Task<Game?> UpdateGameAsync(string id, Game game);
    Task<bool> DeleteGameAsync(string id);
    Task BuyGameAsync(string gameId, string userId);
    Task<IEnumerable<Game>> GetUserLibraryAsync(string userId);
}

