namespace CloudGames.Games.Infrastructure.Metrics;

public static class ApplicationMetrics
{
    // Contador de jogos criados
    public static readonly Prometheus.Counter GamesCreated = Prometheus.Metrics
        .CreateCounter("cloudgames_games_created_total", "Total de jogos criados no sistema");

    // Contador de compras realizadas
    public static readonly Prometheus.Counter GamesPurchased = Prometheus.Metrics
        .CreateCounter("cloudgames_games_purchased_total", "Total de compras de jogos realizadas");

    // Contador de promoções criadas
    public static readonly Prometheus.Counter PromotionsCreated = Prometheus.Metrics
        .CreateCounter("cloudgames_promotions_created_total", "Total de promoções criadas");

    // Gauge do total de jogos no sistema
    public static readonly Prometheus.Gauge TotalGames = Prometheus.Metrics
        .CreateGauge("cloudgames_games_total", "Número total de jogos no catálogo");

    // Contador de erros
    public static readonly Prometheus.Counter Errors = Prometheus.Metrics
        .CreateCounter("cloudgames_errors_total", "Total de erros da aplicação", new Prometheus.CounterConfiguration
        {
            LabelNames = new[] { "tipo" }
        });
}

