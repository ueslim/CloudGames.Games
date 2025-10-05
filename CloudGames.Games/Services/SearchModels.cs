public record PopularResult(string Id, string Title, string Genre, decimal? Price, int? PurchaseCount, double? Score);
public record RecommendationResult(string Id, string Title, string Genre, decimal? Price, int? PurchaseCount, double? Score, string Reason);


