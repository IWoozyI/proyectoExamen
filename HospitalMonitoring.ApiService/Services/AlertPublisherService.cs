using System.Text.Json;
using HospitalMonitoring.ApiService.Data;
using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.ApiService.Services;

public sealed class AlertPublisherService(
    IMessageBroker messageBroker,
    TelemetryDbContext dbContext,
    ILogger<AlertPublisherService> logger) : IAlertPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> TryPublishAsync(MedicalAlertDetectedEvent alertEvent, CancellationToken cancellationToken)
    {
        var published = await messageBroker.PublishAlertAsync(alertEvent, cancellationToken);
        if (published)
        {
            return true;
        }

        dbContext.PendingMessages.Add(new PendingMessage
        {
            Id = Guid.NewGuid(),
            PayloadJson = JsonSerializer.Serialize(alertEvent, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow,
            IsProcessed = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Alerta {EventId} guardada en Mensajes Pendientes.", alertEvent.EventId);
        return false;
    }
}
