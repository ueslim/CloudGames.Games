using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Data;
using CloudGames.Games.Infrastructure.Metrics;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Infrastructure.Services;

public class PromotionService : IPromotionService
{
    private readonly GamesDbContext _context;

    public PromotionService(GamesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .Include(p => p.Game)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .ToListAsync();
    }

    public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
    {
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Métricas
        ApplicationMetrics.PromotionsCreated.Inc();

        return promotion;
    }
}

