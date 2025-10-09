using CloudGames.Games.Application.Interfaces;
using CloudGames.Games.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudGames.Games.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(ISearchService searchService, ILogger<GamesController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for games by query string
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of games matching the search query</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Game>>> Search(
        [FromQuery] string query, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required.");
        }

        try
        {
            _logger.LogInformation("Searching games with query: {Query}", query);
            
            var games = await _searchService.SearchGamesAsync(query, cancellationToken);
            
            _logger.LogInformation("Found {Count} games for query: {Query}", 
                games.Count(), query);
            
            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching games with query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while searching for games.");
        }
    }
}

