# CloudGames.Games

A clean .NET 8 solution following Clean Architecture principles.

## Solution Structure

The solution consists of the following projects:

### CloudGames.Games.Api
ASP.NET Core Web API project that serves as the entry point for the application.
- **References**: Application, Domain
- **Features**:
  - Swagger/OpenAPI documentation available at `/swagger`
  - Health check endpoint at `/health`
  - Sample weather forecast endpoint at `/weatherforecast`

### CloudGames.Games.Application
Class library containing application business logic and use cases.
- **References**: Domain

### CloudGames.Games.Domain
Class library containing domain entities, value objects, and domain logic.
- **References**: None (core domain layer)

### CloudGames.Games.Infrastructure
Class library containing infrastructure concerns (data access, external services, etc.).
- **References**: Application, Domain

### CloudGames.Games.Tests
xUnit test project for testing the application.
- **References**: Application, Domain, Infrastructure

## Project Dependencies

```
CloudGames.Games.Api
├── CloudGames.Games.Application
│   └── CloudGames.Games.Domain
└── CloudGames.Games.Domain

CloudGames.Games.Infrastructure
├── CloudGames.Games.Application
│   └── CloudGames.Games.Domain
└── CloudGames.Games.Domain

CloudGames.Games.Tests
├── CloudGames.Games.Application
│   └── CloudGames.Games.Domain
├── CloudGames.Games.Domain
└── CloudGames.Games.Infrastructure
```

## Getting Started

### Prerequisites
- .NET 8 SDK

### Building the Solution
```bash
dotnet build
```

### Running the API
```bash
dotnet run --project CloudGames.Games.Api
```

### Running Tests
```bash
dotnet test
```

### Accessing Swagger UI
Once the API is running, navigate to:
- `https://localhost:<port>/swagger` (HTTPS)
- `http://localhost:<port>/swagger` (HTTP)

### Health Check
Check the application health at:
- `/health`
