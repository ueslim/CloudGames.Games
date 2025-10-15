using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudGames.Games.Api.Controllers;

[ApiController]
[Route("api/users")]
public class LibraryController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ILogger<LibraryController> _logger;

    public LibraryController(IGameService gameService, ILogger<LibraryController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    /// <summary>
    /// Retorna a biblioteca de jogos do usuário
    /// </summary>
    /// <remarks>
    /// A biblioteca é construída a partir do histórico de eventos de compra (event sourcing).
    /// </remarks>
    [HttpGet("{userId}/library")]
    [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Game>>> GetUserLibrary(string userId)
    {
        _logger.LogInformation("Buscando biblioteca do usuário {UserId}", userId);
        var games = await _gameService.GetUserLibraryAsync(userId);
        _logger.LogInformation("Usuário {UserId} possui {Count} jogos na biblioteca", userId, games.Count());
        return Ok(games);
    }
}

