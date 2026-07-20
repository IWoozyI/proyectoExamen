using System.Net.Http.Json;
using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.Client1.Simulator;

public sealed class DeviceSimulatorWorker(
    IHttpClientFactory httpClientFactory,
    ILogger<DeviceSimulatorWorker> logger) : BackgroundService
{
    private static readonly string[] Patients = ["P-001", "P-002", "P-003", "P-004", "P-005"];
    private static readonly string[] Devices = ["MON-A1", "MON-B2", "MON-C3", "MON-D4", "MON-E5"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulador de dispositivos médicos iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var burstSize = Random.Shared.Next(3, 8);
            logger.LogInformation("Enviando ráfaga de {Count} lecturas...", burstSize);

            for (var i = 0; i < burstSize; i++)
            {
                await SendReadingAsync(stoppingToken);
                await Task.Delay(Random.Shared.Next(200, 600), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(4, 8)), stoppingToken);
        }
    }

    private async Task SendReadingAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("telemetry-api");
        var patientIndex = Random.Shared.Next(Patients.Length);
        var reading = GenerateReading(Patients[patientIndex], Devices[patientIndex]);

        try
        {
            var response = await client.PostAsJsonAsync("/api/telemetry", reading, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Telemetría enviada: Paciente {PatientId}, FC={HeartRate}, SpO2={SpO2}",
                    reading.PatientId,
                    reading.HeartRateBpm,
                    reading.OxygenSaturation);
            }
            else
            {
                logger.LogWarning("Error HTTP {StatusCode} al enviar telemetría.", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al enviar telemetría del paciente {PatientId}.", reading.PatientId);
        }
    }

    private static VitalSignsReading GenerateReading(string patientId, string deviceId)
    {
        var generateAnomaly = Random.Shared.NextDouble() < 0.25;

        if (generateAnomaly)
        {
            return Random.Shared.Next(3) switch
            {
                0 => new VitalSignsReading(patientId, deviceId, Random.Shared.Next(130, 160), 120, 80, 98, 36.8, DateTime.UtcNow),
                1 => new VitalSignsReading(patientId, deviceId, 75, 130, 85, Random.Shared.Next(70, 88), 37.0, DateTime.UtcNow),
                _ => new VitalSignsReading(patientId, deviceId, 72, 118, 76, 97, Random.Shared.Next(39, 41), DateTime.UtcNow)
            };
        }

        return new VitalSignsReading(
            patientId,
            deviceId,
            Random.Shared.Next(60, 95),
            Random.Shared.Next(110, 130),
            Random.Shared.Next(70, 85),
            Random.Shared.Next(95, 100),
            Random.Shared.NextDouble() * 1.5 + 36.0,
            DateTime.UtcNow);
    }
}
