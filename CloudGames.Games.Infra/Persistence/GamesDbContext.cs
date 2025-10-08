using CloudGames.Games.Domain.Entities;
using CloudGames.Games.Infra.Persistence.Outbox;
using CloudGames.Games.Infra.Persistence.StoredEvents;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Games.Infra.Persistence;

public class GamesDbContext : DbContext
{
    public GamesDbContext(DbContextOptions<GamesDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(b =>
        {
            b.ToTable("Games");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.TagsJson);
        });

        modelBuilder.Entity<StoredEvent>(b =>
        {
            b.ToTable("StoredEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(200);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.OccurredAt).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(200);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.OccurredAt).IsRequired();
            b.Property(x => x.ProcessedAt);
        });
    }
}


