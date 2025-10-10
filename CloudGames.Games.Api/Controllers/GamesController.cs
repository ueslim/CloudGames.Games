using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudGames.Games.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ISearchService _searchService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(
        IGameService gameService,
        ISearchService searchService,
        ILogger<GamesController> logger)
    {
        _gameService = gameService;
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os jogos disponíveis
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Game>>> GetAll()
    {
        var games = await _gameService.GetAllGamesAsync();
        return Ok(games);
    }

    /// <summary>
    /// Busca um jogo por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Game>> GetById(Guid id)
    {
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        return Ok(game);
    }

    /// <summary>
    /// Cria um novo jogo (apenas administradores)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Game>> Create([FromBody] Game game)
    {
        var created = await _gameService.CreateGameAsync(game);
        _logger.LogInformation("Jogo criado: {GameId} - {Title}", created.Id, created.Title);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Atualiza um jogo existente (apenas administradores)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Game>> Update(Guid id, [FromBody] Game game)
    {
        var updated = await _gameService.UpdateGameAsync(id, game);
        if (updated == null)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        _logger.LogInformation("Jogo atualizado: {GameId}", id);
        return Ok(updated);
    }

    /// <summary>
    /// Remove um jogo (apenas administradores)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _gameService.DeleteGameAsync(id);
        if (!deleted)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        _logger.LogInformation("Jogo removido: {GameId}", id);
        return NoContent();
    }

    /// <summary>
    /// Compra um jogo para o usuário autenticado
    /// </summary>
    [HttpPost("{id}/purchase")]
    [Authorize(Roles = "User,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Buy(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value
                     ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { mensagem = "Usuário não identificado no token" });

        try
        {
            await _gameService.BuyGameAsync(id, userId);
            _logger.LogInformation("Usuário {UserId} comprou o jogo {GameId}", userId, id);
            return Ok(new { mensagem = "Jogo comprado com sucesso!" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Busca jogos por termo de pesquisa
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Game>>> Search(
        [FromQuery] string query, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { mensagem = "O parâmetro de busca é obrigatório" });

        try
        {
            _logger.LogInformation("Buscando jogos com query: {Query}", query);
            var games = await _searchService.SearchGamesAsync(query, cancellationToken);
            _logger.LogInformation("Encontrados {Count} jogos para query: {Query}", games.Count(), query);
            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar jogos com query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { mensagem = "Ocorreu um erro ao buscar jogos" });
        }
    }

#if DEBUG
    /// <summary>
    /// Sincroniza todos os jogos do banco de dados para o Elasticsearch (apenas Development)
    /// </summary>
    [HttpPost("sync-search-index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncSearchIndex()
    {
        if (_searchService is not ElasticSearchService elasticService)
        {
            return Ok(new { mensagem = "Elasticsearch não está configurado" });
        }

        var games = await _gameService.GetAllGamesAsync();
        await elasticService.IndexGamesAsync(games);
        
        return Ok(new { count = games.Count() });
    }
#endif
}

