using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueClient _queue;

    public OutboxPublisher(IServiceProvider serviceProvider, QueueClient queue)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
            var pending = await db.OutboxMessages
                .Where(x => x.ProcessedAt == null)
                .OrderBy(x => x.OccurredAt)
                .Take(50)
                .ToListAsync(stoppingToken);

            foreach (var msg in pending)
            {
                await _queue.SendMessageAsync(JsonSerializer.Serialize(new { Type = msg.Type, Data = JsonSerializer.Deserialize<object>(msg.Payload) }), cancellationToken: stoppingToken);
                msg.ProcessedAt = DateTime.UtcNow;
            }

            if (pending.Count > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}


