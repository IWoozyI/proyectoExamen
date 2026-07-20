using System.Text.Json;
using HospitalMonitoring.ApiService.Data;
using HospitalMonitoring.ApiService.Services;
using HospitalMonitoring.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalMonitoring.ApiService.BackgroundServices;

public sealed class PendingMessageRetryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingMessageRetryWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al procesar mensajes pendientes.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();

        var pendingMessages = await dbContext.PendingMessages
            .Where(m => !m.IsProcessed)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingMessages)
        {
            var alertEvent = JsonSerializer.Deserialize<MedicalAlertDetectedEvent>(pending.PayloadJson, JsonOptions);
            if (alertEvent is null)
            {
                pending.IsProcessed = true;
                pending.ProcessedAtUtc = DateTime.UtcNow;
                continue;
            }

            var published = await messageBroker.PublishAlertAsync(alertEvent, cancellationToken);
            if (!published)
            {
                logger.LogWarning("Broker aún no disponible. Se detiene reenvío para mantener orden cronológico.");
                break;
            }

            pending.IsProcessed = true;
            pending.ProcessedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Mensaje pendiente {MessageId} reenviado con éxito.", pending.Id);
        }

        if (pendingMessages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
