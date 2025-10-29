using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Api.DTOs;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Services;
using CloudGames.Games.Api.Services;
using Microsoft.AspNetCore.Mvc;
using CloudGames.Games.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Api.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ISearchService _searchService;
    private readonly ILogger<GamesController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GamesDbContext _context;

    public GamesController(
        IGameService gameService,
        ISearchService searchService,
        ILogger<GamesController> logger,
        IHttpClientFactory httpClientFactory,
        GamesDbContext context)
    {
        _gameService = gameService;
        _searchService = searchService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    /// <summary>
    /// Lista todos os jogos disponíveis
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GameDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GameDto>>> GetAll()
    {
        var games = await _gameService.GetAllGamesAsync();
        var gameDtos = GameMappingService.ToDto(games).ToList();
        
        // Aplicar promoções ativas
        await ApplyActivePromotionsAsync(gameDtos);
        
        return Ok(gameDtos);
    }

    /// <summary>
    /// Busca um jogo por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetById(Guid id)
    {
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        var gameDto = GameMappingService.ToDto(game);
        
        // Aplicar promoção ativa se existir
        await ApplyActivePromotionsAsync(new List<GameDto> { gameDto });
        
        return Ok(gameDto);
    }

    /// <summary>
    /// Cria um novo jogo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameDto>> Create([FromBody] CreateGameDto createGameDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var game = GameMappingService.ToEntity(createGameDto);
        var created = await _gameService.CreateGameAsync(game);
        _logger.LogInformation("Jogo criado: {GameId} - {Title}", created.Id, created.Title);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, GameMappingService.ToDto(created));
    }

    /// <summary>
    /// Atualiza um jogo existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameDto>> Update(Guid id, [FromBody] UpdateGameDto updateGameDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var game = GameMappingService.ToEntity(updateGameDto, id);
        var updated = await _gameService.UpdateGameAsync(id, game);
        if (updated == null)
            return NotFound(new { mensagem = "Jogo não encontrado" });
        
        _logger.LogInformation("Jogo atualizado: {GameId}", id);
        return Ok(GameMappingService.ToDto(updated));
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
        // Em produção, APIM valida o usuário e passa informações via headers
        // Em desenvolvimento, userId pode ser passado via header para testes
        if (string.IsNullOrEmpty(userId))
        {
            // Para desenvolvimento/testes, usa um ID de usuário padrão
            userId = "dev-user-001";
            _logger.LogWarning("Nenhum userId fornecido, usando padrão para desenvolvimento: {UserId}", userId);
        }

        // Obtém detalhes do jogo
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
        {
            _logger.LogWarning("Jogo {GameId} não encontrado para compra", id);
            return NotFound(new { mensagem = "Jogo não encontrado" });
        }

        try
        {
            // Verificar se há promoção ativa
            var now = DateTime.UtcNow;
            var activePromotion = await _context.Promotions
                .Where(p => p.GameId == id && p.StartDate <= now && p.EndDate >= now)
                .FirstOrDefaultAsync();
            
            // Calcular preço final (com promoção ou sem)
            var finalPrice = game.Price;
            if (activePromotion != null && game.Price.HasValue)
            {
                finalPrice = game.Price.Value * (1 - activePromotion.DiscountPercentage / 100);
                _logger.LogInformation("Promoção ativa aplicada: {Discount}% de desconto. Preço original: {OriginalPrice}, Preço final: {FinalPrice}", 
                    activePromotion.DiscountPercentage, game.Price, finalPrice);
            }
            
            // Chama API de Pagamentos para iniciar pagamento
            var httpClient = _httpClientFactory.CreateClient("PaymentsApi");
            
            var paymentRequest = new
            {
                gameId = id,
                amount = finalPrice
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
            {
                Content = JsonContent.Create(paymentRequest)
            };
            
            // Encaminha header userId (padrão APIM)
            request.Headers.Add("userId", userId);

            _logger.LogInformation("Iniciando pagamento para usuario {UserId}, jogo {GameId}, valor {Amount}", 
                userId, id, finalPrice);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha ao iniciar pagamento. Status: {StatusCode}", response.StatusCode);
                return StatusCode(500, new { mensagem = "Falha ao iniciar pagamento" });
            }

            // Extrai paymentId do header Location da resposta
            var location = response.Headers.Location?.ToString();
            string? paymentId = null;
            
            if (!string.IsNullOrEmpty(location))
            {
                // Extrai ID do pagamento da location: /api/payments/{id}/status
                var segments = location.Split('/');
                paymentId = segments.Length >= 4 ? segments[^2] : null;
            }

            if (string.IsNullOrEmpty(paymentId))
            {
                _logger.LogError("Pagamento iniciado mas nenhum paymentId foi retornado");
                return StatusCode(500, new { mensagem = "Erro ao processar pagamento" });
            }

            _logger.LogInformation("Pagamento {PaymentId} criado para usuario {UserId} e jogo {GameId}", 
                paymentId, userId, id);

            return Ok(new { paymentId });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao chamar API de Pagamentos para jogo {GameId}", id);
            return StatusCode(500, new { mensagem = "Erro ao conectar com serviço de pagamentos" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante compra do jogo {GameId}", id);
            return StatusCode(500, new { mensagem = "Erro inesperado ao processar compra" });
        }
    }

    /// <summary>
    /// Busca jogos por termo de pesquisa
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<GameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<GameDto>>> Search(
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
            
            var gameDtos = GameMappingService.ToDto(games).ToList();
            
            // Aplicar promoções ativas
            await ApplyActivePromotionsAsync(gameDtos);
            
            return Ok(gameDtos);
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
    
    /// <summary>
    /// Aplica promoções ativas aos jogos
    /// </summary>
    private async Task ApplyActivePromotionsAsync(List<GameDto> gameDtos)
    {
        if (gameDtos == null || !gameDtos.Any())
            return;
        
        var gameIds = gameDtos.Select(g => g.Id).ToList();
        var now = DateTime.UtcNow;
        
        // Buscar promoções ativas para os jogos
        var activePromotions = await _context.Promotions
            .Where(p => gameIds.Contains(p.GameId) && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync();
        
        // Aplicar promoções aos DTOs
        foreach (var gameDto in gameDtos)
        {
            var promotion = activePromotions.FirstOrDefault(p => p.GameId == gameDto.Id);
            if (promotion != null && gameDto.Price.HasValue)
            {
                gameDto.DiscountPercentage = promotion.DiscountPercentage;
                gameDto.PromotionalPrice = gameDto.Price.Value * (1 - promotion.DiscountPercentage / 100);
            }
        }
    }
}

