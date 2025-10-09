# Elasticsearch Setup for CloudGames.Games

## Overview

The CloudGames.Games project supports two search implementations:
- **ElasticSearchService**: Uses Elasticsearch for advanced search capabilities (local development only)
- **EfSearchService**: Uses Entity Framework Core with SQL Server for basic search (production/Azure)

The implementation is automatically selected based on the presence of `Elastic:Endpoint` configuration.

## Local Development with Elasticsearch

### Prerequisites
- Docker Desktop installed and running
- .NET 8.0 SDK

### Starting Elasticsearch and Kibana

1. Start the containers using docker-compose:
```bash
docker-compose up -d
```

2. Verify the services are running:
   - Elasticsearch: http://localhost:9200
   - Kibana: http://localhost:5601

3. Check Elasticsearch health:
```bash
curl http://localhost:9200/_cluster/health
```

### Running the API with Elasticsearch

The API will automatically use Elasticsearch when running in Development mode because `appsettings.Development.json` contains:

```json
{
  "Elastic": {
    "Endpoint": "http://localhost:9200",
    "IndexName": "games"
  }
}
```

Simply run the API:
```bash
dotnet run --project CloudGames.Games.Api
```

### Indexing Games

The Elasticsearch index is automatically created when the application starts. To populate the index with games, you have a few options:

1. **Manual indexing via ElasticSearchService methods** (if you add endpoints for it):
   - `IndexGameAsync(Game game)` - Index a single game
   - `IndexGamesAsync(IEnumerable<Game> games)` - Bulk index multiple games
   - `DeleteGameAsync(string gameId)` - Remove a game from the index

2. **Direct Elasticsearch API** (using curl or Kibana Dev Tools):
```bash
curl -X POST "localhost:9200/games/_doc/1" -H 'Content-Type: application/json' -d'
{
  "id": "1",
  "title": "The Legend of Zelda",
  "description": "An epic adventure game",
  "genre": "Action-Adventure",
  "publisher": "Nintendo",
  "releaseDate": "1986-02-21",
  "price": 59.99,
  "rating": 9.5
}
'
```

### Using Kibana for Development

Access Kibana at http://localhost:5601 to:
- View and manage indices
- Execute search queries
- Monitor Elasticsearch performance
- Debug search relevance

Navigate to **Dev Tools** to execute Elasticsearch queries directly.

### Stopping the Services

```bash
docker-compose down
```

To also remove the data volumes:
```bash
docker-compose down -v
```

## Production/Azure Deployment

In production (Azure), the API uses **EfSearchService** because `appsettings.json` does not contain the `Elastic:Endpoint` configuration.

The search functionality falls back to SQL Server using Entity Framework Core, ensuring the API works without Elasticsearch dependency.

## Configuration Details

### appsettings.Development.json (Local)
```json
{
  "Elastic": {
    "Endpoint": "http://localhost:9200",
    "IndexName": "games"
  }
}
```

### appsettings.json (Production/Azure)
No `Elastic` section - automatically uses EF Core search.

## Search Service Selection Logic

In `Program.cs`:
```csharp
var elasticEndpoint = builder.Configuration["Elastic:Endpoint"];
if (!string.IsNullOrEmpty(elasticEndpoint))
{
    // Use Elasticsearch for search (local development)
    builder.Services.AddSingleton<ISearchService, ElasticSearchService>();
}
else
{
    // Use EF Core for search (production/Azure)
    builder.Services.AddScoped<ISearchService, EfSearchService>();
}
```

## Testing Search

Use the search endpoint:
```bash
curl "https://localhost:7XXX/api/games/search?query=zelda"
```

## Troubleshooting

### Elasticsearch not starting
- Ensure Docker Desktop is running
- Check if ports 9200 and 9300 are available
- View logs: `docker-compose logs elasticsearch`

### Connection errors
- Verify Elasticsearch is healthy: `curl http://localhost:9200/_cluster/health`
- Check the `Elastic:Endpoint` configuration matches the container URL

### Index creation issues
- Check application logs for detailed error messages
- Verify Elasticsearch has sufficient memory (512MB minimum configured)

## Architecture

```
┌─────────────────────────────────────┐
│      GamesController                │
└──────────────┬──────────────────────┘
               │
               ▼
      ┌────────────────┐
      │ ISearchService │
      └────────┬───────┘
               │
        ┌──────┴──────┐
        ▼             ▼
┌──────────────┐  ┌─────────────────┐
│ EfSearchSer. │  │ ElasticSearchSer│
│ (Production) │  │   (Local Dev)   │
└──────┬───────┘  └────────┬────────┘
       │                   │
       ▼                   ▼
  ┌─────────┐      ┌──────────────┐
  │SQL Server│      │ Elasticsearch│
  └──────────┘      └──────────────┘
```

## Benefits

- **Local Development**: Advanced search with fuzzy matching, relevance scoring, and fast performance
- **Production**: No additional infrastructure required, works with existing SQL Server
- **Seamless Switch**: Same API interface, automatic selection based on configuration
- **Cost Effective**: Only pay for Elasticsearch if you choose to use it in production

