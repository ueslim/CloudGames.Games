using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CloudGames.Games.Infra.Persistence;

public class GamesDbContextFactory : IDesignTimeDbContextFactory<GamesDbContext>
{
    public GamesDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("GamesDb")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__GamesDb")
                 ?? "Server=(localdb)\\MSSQLLocalDB;Database=CloudGames.Games;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<GamesDbContext>();
        optionsBuilder.UseSqlServer(cs);
        return new GamesDbContext(optionsBuilder.Options);
    }
}


