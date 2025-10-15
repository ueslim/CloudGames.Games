using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CloudGames.Games.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        public static async Task EnsureDataBaseMigratedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
            var creator = db.GetService<IRelationalDatabaseCreator>();

            if (!await creator.ExistsAsync())
            {
                Log.Warning("Banco não existe. Criando...");
                await creator.CreateAsync();
                await db.Database.MigrateAsync();
                Log.Information("Banco criado e migrations aplicadas");
                return;
            }

            var applied = await db.Database.GetAppliedMigrationsAsync();
            if (!applied.Any())
            {
                Log.Warning("Banco existe, mas nenhuma migration aplicada. Aplicando todas...");
                await db.Database.MigrateAsync();
                Log.Information("Migrations aplicadas");
                return;
            }

            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                Log.Information($"Aplicando {pending.Count()} migrations pendentes...");
                await db.Database.MigrateAsync();
                Log.Information("Migrations aplicadas com sucesso");
            }
            else
            {
                Log.Information("Banco atualizado, nenhuma migration pendente");
            }
        }
    }
}