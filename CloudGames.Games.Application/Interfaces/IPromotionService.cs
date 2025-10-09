using CloudGames.Games.Domain.Entities;

namespace CloudGames.Games.Application.Interfaces;

public interface IPromotionService
{
    Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
    Task<Promotion> CreatePromotionAsync(Promotion promotion);
}

