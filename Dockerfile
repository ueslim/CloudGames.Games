# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CloudGames.Games.Api/CloudGames.Games.Api.csproj CloudGames.Games.Api/
COPY CloudGames.Games.Application/CloudGames.Games.Application.csproj CloudGames.Games.Application/
COPY CloudGames.Games.Domain/CloudGames.Games.Domain.csproj CloudGames.Games.Domain/
COPY CloudGames.Games.Infrastructure/CloudGames.Games.Infrastructure.csproj CloudGames.Games.Infrastructure/
RUN dotnet restore CloudGames.Games.Api/CloudGames.Games.Api.csproj
COPY . .
RUN dotnet publish CloudGames.Games.Api/CloudGames.Games.Api.csproj -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD wget -qO- http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "CloudGames.Games.Api.dll"]

