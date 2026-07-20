using System.Text;
using System.Text.Json;
using HospitalMonitoring.Api2.Alerts.Hubs;
using HospitalMonitoring.Api2.Alerts.Services;
using HospitalMonitoring.Shared;
using HospitalMonitoring.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HospitalMonitoring.Api2.Alerts.BackgroundServices;

public sealed class MedicalAlertConsumerService(
    IConnection connection,
    INursingAlertNotifier notifier,
    ILogger<MedicalAlertConsumerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en consumidor de alertas. Reintentando en 5 segundos...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await EnsureTopologyAsync(channel, cancellationToken);

        await channel.BasicQosAsync(0, 1, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await HandleMessageAsync(channel, args, cancellationToken);
        };

        await channel.BasicConsumeAsync(MessagingConstants.AlertQueue, autoAck: false, consumer, cancellationToken);
        logger.LogInformation("Consumidor de alertas médicas iniciado.");

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        MedicalAlertDetectedEvent? alertEvent = null;
        JsonException? jsonException = null;

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            alertEvent = JsonSerializer.Deserialize<MedicalAlertDetectedEvent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            jsonException = ex;
        }

        var (isValid, reason) = AlertValidator.Validate(alertEvent, jsonException);
        if (!isValid)
        {
            logger.LogWarning("Mensaje inválido enviado a DLQ. Motivo: {Reason}", reason);
            await PublishToDeadLetterQueueAsync(channel, args.Body.ToArray(), reason, cancellationToken);
            await channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);
            return;
        }

        var severity = AlertSeverityClassifier.Classify(alertEvent!);
        var notification = new NursingAlertNotification(
            alertEvent!.EventId,
            alertEvent.PatientId,
            severity,
            alertEvent.AnomalyType,
            alertEvent.Description,
            alertEvent.HeartRateBpm,
            alertEvent.OxygenSaturation,
            DateTime.UtcNow);

        await notifier.NotifyAsync(notification, cancellationToken);
        await channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);

        logger.LogInformation(
            "Alerta procesada para paciente {PatientId} con severidad {Severity}.",
            alertEvent.PatientId,
            severity);
    }

    private static async Task PublishToDeadLetterQueueAsync(
        IChannel channel,
        byte[] body,
        string reason,
        CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = new Dictionary<string, object?> { ["x-death-reason"] = reason }
        };

        await channel.BasicPublishAsync(
            MessagingConstants.DeadLetterExchange,
            MessagingConstants.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            MessagingConstants.DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            MessagingConstants.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            MessagingConstants.DeadLetterQueue,
            MessagingConstants.DeadLetterExchange,
            MessagingConstants.RoutingKey,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            MessagingConstants.AlertExchange,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            MessagingConstants.AlertQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = MessagingConstants.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = MessagingConstants.RoutingKey
            },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            MessagingConstants.AlertQueue,
            MessagingConstants.AlertExchange,
            MessagingConstants.RoutingKey,
            cancellationToken: cancellationToken);
    }
}
