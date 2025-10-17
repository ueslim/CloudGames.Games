using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CloudGames.Games.Application.Interfaces;

namespace CloudGames.Games.Api.Services;

public class PaymentEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private readonly QueueClient _queueClient;

    public PaymentEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<PaymentEventConsumer> logger,
        QueueClient queueClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueClient = queueClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumidor de eventos de pagamento iniciado - aguardando eventos na fila");

        // Aguarda um pouco antes de começar
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Cria a fila se não existir
                await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

                // Recebe mensagens da fila (até 10 por vez)
                QueueMessage[]? messages = null;
                try
                {
                    var response = await _queueClient.ReceiveMessagesAsync(
                        maxMessages: 10,
                        cancellationToken: stoppingToken);
                    messages = response.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao receber mensagens da fila - fila pode não existir ainda");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                if (messages == null || messages.Length == 0)
                {
                    // Sem mensagens, aguarda antes de tentar novamente
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    try
                    {
                        // Deserializa evento
                        var eventData = JsonSerializer.Deserialize<PaymentApprovedEvent>(message.MessageText);

                        if (eventData?.EventType == "PaymentApproved")
                        {
                            _logger.LogInformation(
                                "Evento PaymentApproved recebido - Usuario: {UserId}, Jogo: {GameId}",
                                eventData.UserId, eventData.GameId);

                            // Adiciona jogo à biblioteca do usuário
                            using var scope = _serviceProvider.CreateScope();
                            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                            
                            // Usa UserId como string (converte Guid para string)
                            await gameService.BuyGameAsync(eventData.GameId, eventData.UserId.ToString());

                            _logger.LogInformation(
                                "Jogo {GameId} adicionado à biblioteca do usuario {UserId}",
                                eventData.GameId, eventData.UserId);

                            // Remove mensagem da fila (confirma processamento)
                            await _queueClient.DeleteMessageAsync(
                                message.MessageId,
                                message.PopReceipt,
                                stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Erro ao processar evento de pagamento. MessageId: {MessageId}",
                            message.MessageId);
                        
                        // Mensagem volta para a fila automaticamente após timeout de visibilidade
                    }
                }

                _logger.LogInformation("Processadas {Count} mensagem(ns) da fila de pagamentos", messages.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no consumidor de eventos de pagamento");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Consumidor de eventos de pagamento parado");
    }

        private class PaymentApprovedEvent
        {
            public string EventType { get; set; } = string.Empty;
            public Guid PaymentId { get; set; }
            public Guid UserId { get; set; }
            public Guid GameId { get; set; }
            public decimal Amount { get; set; }
            public DateTime Timestamp { get; set; }
        }
}

