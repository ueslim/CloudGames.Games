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
    private readonly IHttpClientFactory _httpClientFactory;

    public GamesController(
        IGameService gameService,
        ISearchService searchService,
        ILogger<GamesController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _gameService = gameService;
        _searchService = searchService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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
    /// Compra um jogo para o usuário (inicia processo de pagamento)
    /// </summary>
    [HttpPost("{id}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        // Get game details
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found for purchase", id);
            return NotFound(new { mensagem = "Jogo não encontrado" });
        }

        try
        {
            // Call Payments API to initiate payment
            var httpClient = _httpClientFactory.CreateClient("PaymentsApi");
            
            var paymentRequest = new
            {
                gameId = id,
                amount = game.Price
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
            {
                Content = JsonContent.Create(paymentRequest)
            };
            
            // Forward userId header (APIM pattern)
            request.Headers.Add("userId", userId);

            _logger.LogInformation("Initiating payment for user {UserId}, game {GameId}, amount {Amount}", 
                userId, id, game.Price);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Payment initiation failed. Status: {StatusCode}", response.StatusCode);
                return StatusCode(500, new { mensagem = "Falha ao iniciar pagamento" });
            }

            // Parse response to get paymentId from Location header or response body
            var location = response.Headers.Location?.ToString();
            string? paymentId = null;
            
            if (!string.IsNullOrEmpty(location))
            {
                // Extract payment ID from location: /api/payments/{id}/status
                var segments = location.Split('/');
                paymentId = segments.Length >= 4 ? segments[^2] : null;
            }

            if (string.IsNullOrEmpty(paymentId))
            {
                _logger.LogError("Payment initiated but no paymentId returned");
                return StatusCode(500, new { mensagem = "Erro ao processar pagamento" });
            }

            _logger.LogInformation("Payment {PaymentId} created for user {UserId} and game {GameId}", 
                paymentId, userId, id);

            return Ok(new { paymentId });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Payments API for game {GameId}", id);
            return StatusCode(500, new { mensagem = "Erro ao conectar com serviço de pagamentos" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during purchase for game {GameId}", id);
            return StatusCode(500, new { mensagem = "Erro inesperado ao processar compra" });
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

