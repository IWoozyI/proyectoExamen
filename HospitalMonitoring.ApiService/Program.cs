using HospitalMonitoring.ApiService.BackgroundServices;
using HospitalMonitoring.ApiService.Data;
using HospitalMonitoring.ApiService.Services;
using HospitalMonitoring.Shared.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<TelemetryDbContext>("telemetrydb");
builder.AddRabbitMQClient("messaging");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
builder.Services.AddScoped<IAlertPublisher, AlertPublisherService>();
builder.Services.AddHostedService<PendingMessageRetryWorker>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "API 1 - Recepción de Telemetría",
    endpoints = new[] { "POST /api/telemetry", "GET /api/telemetry/{patientId}", "GET /api/pending-messages" }
}));

app.MapPost("/api/telemetry", async (VitalSignsReading reading, TelemetryDbContext db, IAlertPublisher publisher, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(reading.PatientId) || string.IsNullOrWhiteSpace(reading.DeviceId))
    {
        return Results.BadRequest(new { error = "PatientId y DeviceId son obligatorios." });
    }

    var (isAnomaly, anomalyType, description) = AlertDetector.Detect(reading);

    var record = new VitalSignRecord
    {
        Id = Guid.NewGuid(),
        PatientId = reading.PatientId.Trim(),
        DeviceId = reading.DeviceId.Trim(),
        HeartRateBpm = reading.HeartRateBpm,
        SystolicBp = reading.SystolicBp,
        DiastolicBp = reading.DiastolicBp,
        OxygenSaturation = reading.OxygenSaturation,
        TemperatureCelsius = reading.TemperatureCelsius,
        RecordedAtUtc = reading.RecordedAtUtc == default ? DateTime.UtcNow : reading.RecordedAtUtc,
        IsAnomaly = isAnomaly
    };

    db.VitalSignRecords.Add(record);
    await db.SaveChangesAsync(ct);

    if (!isAnomaly)
    {
        return Results.Ok(new { stored = true, alertGenerated = false, recordId = record.Id });
    }

    var alertEvent = new MedicalAlertDetectedEvent(
        Guid.NewGuid(),
        record.PatientId,
        record.DeviceId,
        anomalyType,
        description,
        record.HeartRateBpm,
        record.SystolicBp,
        record.DiastolicBp,
        record.OxygenSaturation,
        record.TemperatureCelsius,
        record.RecordedAtUtc);

    var published = await publisher.TryPublishAsync(alertEvent, ct);

    return Results.Ok(new
    {
        stored = true,
        alertGenerated = true,
        recordId = record.Id,
        alertEventId = alertEvent.EventId,
        queuedImmediately = published,
        queuedAsPending = !published
    });
});

app.MapGet("/api/telemetry/{patientId}", async (string patientId, TelemetryDbContext db, CancellationToken ct) =>
{
    var history = await db.VitalSignRecords
        .Where(r => r.PatientId == patientId)
        .OrderByDescending(r => r.RecordedAtUtc)
        .Take(50)
        .ToListAsync(ct);

    return Results.Ok(history);
});

app.MapGet("/api/pending-messages", async (TelemetryDbContext db, CancellationToken ct) =>
{
    var pending = await db.PendingMessages
        .Where(m => !m.IsProcessed)
        .OrderBy(m => m.CreatedAtUtc)
        .ToListAsync(ct);

    return Results.Ok(new { count = pending.Count, messages = pending });
});

app.MapDefaultEndpoints();
app.Run();
