using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudGames.Games.Api.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionsController> _logger;

    public PromotionsController(IPromotionService promotionService, ILogger<PromotionsController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as promoções ativas no momento
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Promotion>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Promotion>>> GetActivePromotions()
    {
        var promotions = await _promotionService.GetActivePromotionsAsync();
        _logger.LogInformation("Listando {Count} promoções ativas", promotions.Count());
        return Ok(promotions);
    }

    /// <summary>
    /// Cria uma nova promoção
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Promotion), StatusCodes.Status201Created)]
    public async Task<ActionResult<Promotion>> CreatePromotion([FromBody] Promotion promotion)
    {
        if (promotion.Id == Guid.Empty)
            promotion.Id = Guid.NewGuid();

        var created = await _promotionService.CreatePromotionAsync(promotion);
        _logger.LogInformation("Promoção criada para o jogo {GameId} com {Discount}% de desconto", 
            promotion.GameId, promotion.DiscountPercentage);
        return CreatedAtAction(nameof(GetActivePromotions), new { id = created.Id }, created);
    }
}

