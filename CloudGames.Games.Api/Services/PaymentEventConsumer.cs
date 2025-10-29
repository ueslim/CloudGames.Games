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
                        var eventData = message.Body.ToObjectFromJson<PaymentApprovedEvent>();

                        if (eventData != null)
                        {
                            _logger.LogInformation(
                                "Evento recebido - Usuario: {UserId}, Jogo: {GameId}, Valor pago: {Amount}",
                                eventData.UserId, eventData.GameId, eventData.Amount);

                            using var scope = _serviceProvider.CreateScope();
                            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                            
                            // Passa o valor efetivamente pago (com desconto se aplicável)
                            await gameService.BuyGameAsync(eventData.GameId, eventData.UserId.ToString(), eventData.Amount);

                            _logger.LogInformation(
                                "Jogo {GameId} adicionado à biblioteca do usuario {UserId} com valor {Amount}",
                                eventData.GameId, eventData.UserId, eventData.Amount);

                            await _queueClient.DeleteMessageAsync(
                                message.MessageId,
                                message.PopReceipt,
                                stoppingToken);
                        }
                        else
                        {
                            _logger.LogWarning("Mensagem inválida, removendo da fila: {MessageId}", message.MessageId);
                            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar evento. MessageId: {MessageId}", message.MessageId);
                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
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
            public Guid PaymentId { get; set; }
            public Guid UserId { get; set; }
            public Guid GameId { get; set; }
            public decimal Amount { get; set; }
        }
}

