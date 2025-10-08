using CloudGames.Games.Application.Abstractions;
using CloudGames.Games.Application.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudGames.Games.Application.Purchases;

public class PurchaseService : IPurchaseService
{
    private readonly IGameReadRepository _games;
    private readonly IEventOutboxService _outbox;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(IGameReadRepository games, IEventOutboxService outbox, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<PurchaseService> logger)
    {
        _games = games;
        _outbox = outbox;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Guid> StartPurchaseAsync(Guid gameId, Guid userId, CancellationToken ct = default)
    {
        var game = await _games.GetByIdAsync(gameId, ct);
        if (game == null) throw new KeyNotFoundException("Game not found");
        var payload = JsonSerializer.Serialize(new { GameId = gameId, UserId = userId, Amount = game.Price });

        await _outbox.AddOutboxMessageAsync("GamePurchased", payload, DateTime.UtcNow, ct);
        await _outbox.AddStoredEventAsync("GamePurchased", payload, DateTime.UtcNow, ct);

        var client = _httpClientFactory.CreateClient("payments");
        TryForwardAuthorizationHeader(client);

        try
        {
            _logger.LogInformation("Calling Payments API to create payment for game {GameId} with price {Price}.", gameId, game.Price);
            using var response = await client.PostAsJsonAsync($"/api/payments", new { GameId = gameId, UserId = userId, Amount = game.Price }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await SafeReadContentAsync(response, ct);
                _logger.LogError("Payments API returned non-success status {StatusCode}. Body: {Body}", (int)response.StatusCode, errorBody);
                throw CreateHttpException("Payments API returned non-success status.", response.StatusCode);
            }

            var created = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(cancellationToken: ct);
            if (created == null)
            {
                _logger.LogError("Payments API response could not be deserialized into CreatePaymentResponse.");
                throw new HttpRequestException("Invalid response from Payments API", null, HttpStatusCode.BadGateway);
            }
            _logger.LogInformation("Payments API created payment {PaymentId} for game {GameId}.", created.PaymentId, gameId);
            return created.PaymentId;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Payments API request timed out when creating payment for game {GameId}.", gameId);
            throw new HttpRequestException("Payments service timeout", ex, HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when calling Payments API for game {GameId}.", gameId);
            throw new HttpRequestException("Payments service error", ex, ex.StatusCode ?? HttpStatusCode.BadGateway);
        }
    }

    public async Task<string> GetPurchaseStatusAsync(Guid paymentId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("payments");
        TryForwardAuthorizationHeader(client);

        try
        {
            _logger.LogInformation("Querying Payments API for status of payment {PaymentId}.", paymentId);
            using var response = await client.GetAsync($"/api/payments/{paymentId}/status", ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await SafeReadContentAsync(response, ct);
                _logger.LogError("Payments API returned non-success status {StatusCode} for status check. Body: {Body}", (int)response.StatusCode, errorBody);
                throw CreateHttpException("Payments API returned non-success status.", response.StatusCode);
            }

            var status = await response.Content.ReadFromJsonAsync<PaymentStatusResponse>(cancellationToken: ct);
            var result = status?.Status ?? "unknown";
            _logger.LogInformation("Payments status for {PaymentId} is {Status}.", paymentId, result);
            return result;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Payments API request timed out when querying status for {PaymentId}.", paymentId);
            throw new HttpRequestException("Payments service timeout", ex, HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when calling Payments API for status {PaymentId}.", paymentId);
            throw new HttpRequestException("Payments service error", ex, ex.StatusCode ?? HttpStatusCode.BadGateway);
        }
    }

    private static HttpRequestException CreateHttpException(string message, HttpStatusCode statusCode)
    {
        return new HttpRequestException(message, null, statusCode);
    }

    private void TryForwardAuthorizationHeader(HttpClient client)
    {
        var authHeader = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            if (AuthenticationHeaderValue.TryParse(authHeader, out var header))
            {
                client.DefaultRequestHeaders.Authorization = header;
            }
        }
    }

    private static async Task<string> SafeReadContentAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }
}

public record CreatePaymentResponse(Guid PaymentId);
public record PaymentStatusResponse(string Status);


