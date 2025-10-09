using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Infrastructure.Services;

public class EfSearchService : ISearchService
{
    private readonly GamesDbContext _context;

    public EfSearchService(GamesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Game>> SearchGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Game>();
        }

        try
        {
            // Search games by title, description, genre, or publisher
            var games = await _context.Games
                .Where(g => 
                    EF.Functions.Like(g.Title, $"%{query}%") ||
                    EF.Functions.Like(g.Description ?? "", $"%{query}%") ||
                    EF.Functions.Like(g.Genre ?? "", $"%{query}%") ||
                    EF.Functions.Like(g.Publisher ?? "", $"%{query}%"))
                .Take(50)
                .ToListAsync(cancellationToken);

            return games;
        }
        catch (Exception ex)
        {
            // Log the exception in production
            throw new InvalidOperationException($"Error searching games: {ex.Message}", ex);
        }
    }
}

