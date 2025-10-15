using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudGames.Games.Api.Controllers;

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
    /// Cria um novo jogo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Game), StatusCodes.Status201Created)]
    public async Task<ActionResult<Game>> Create([FromBody] Game game)
    {
        var created = await _gameService.CreateGameAsync(game);
        _logger.LogInformation("Jogo criado: {GameId} - {Title}", created.Id, created.Title);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Atualiza um jogo existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Game>> Update(Guid id, [FromBody] Game game)
    {
        var updated = await _gameService.UpdateGameAsync(id, game);
        if (updated == null)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        _logger.LogInformation("Jogo atualizado: {GameId}", id);
        return Ok(updated);
    }

    /// <summary>
    /// Remove um jogo
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _gameService.DeleteGameAsync(id);
        if (!deleted)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        _logger.LogInformation("Jogo removido: {GameId}", id);
        return NoContent();
    }

    /// <summary>
    /// Compra um jogo para o usuário
    /// </summary>
    [HttpPost("{id}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buy(Guid id, [FromHeader] string? userId)
    {
        // In production, APIM validates the user and passes user info via headers
        // In development, userId can be passed via header for testing
        if (string.IsNullOrEmpty(userId))
        {
            // For development/testing, use a default user ID
            userId = "dev-user-001";
            _logger.LogWarning("No userId provided, using default for development: {UserId}", userId);
        }

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

    /// <summary>
    /// Sincroniza jogos para o Elasticsearch (apenas quando configurado)
    /// </summary>
    [HttpPost("sync-search-index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncSearchIndex()
    {
        if (_searchService is not ElasticSearchService elasticService)
        {
            return Ok(new { mensagem = "Elasticsearch não configurado" });
        }

        try
        {
            var games = await _gameService.GetAllGamesAsync();
            var gamesList = games.ToList();
            await elasticService.IndexGamesAsync(gamesList);
            
            _logger.LogInformation("Elasticsearch manual sync: {Count} games", gamesList.Count);
            return Ok(new { count = gamesList.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch sync failed");
            return StatusCode(500, new { mensagem = "Erro ao sincronizar" });
        }
    }
}

