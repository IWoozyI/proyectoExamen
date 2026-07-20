using System.Text;
using System.Text.Json;
using HospitalMonitoring.Shared;
using HospitalMonitoring.Shared.Models;
using RabbitMQ.Client;

namespace HospitalMonitoring.ApiService.Services;

public sealed class RabbitMqMessageBroker(
    IConnection connection,
    ILogger<RabbitMqMessageBroker> logger) : IMessageBroker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _topologyInitialized;

    public async Task<bool> PublishAlertAsync(MedicalAlertDetectedEvent alertEvent, CancellationToken cancellationToken)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await EnsureTopologyAsync(channel, cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(alertEvent, JsonOptions));
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

            await channel.BasicPublishAsync(
                MessagingConstants.AlertExchange,
                MessagingConstants.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation("Alerta {EventId} publicada en RabbitMQ.", alertEvent.EventId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo publicar la alerta {EventId}.", alertEvent.EventId);
            return false;
        }
    }

    private async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_topologyInitialized)
        {
            return;
        }

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

        _topologyInitialized = true;
    }
}
