using Microsoft.EntityFrameworkCore;

public class GamesDbContext : DbContext
{
    public GamesDbContext(DbContextOptions<GamesDbContext> options) : base(options) { }
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
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

        modelBuilder.Entity<GameEvent>(b =>
        {
            b.ToTable("GameEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(50);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<StoredEvent>(b =>
        {
            b.ToTable("StoredEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.AggregateId).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.Data).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.OccurredAt).IsRequired();
            b.Property(x => x.ProcessedAt);
        });
    }
}


