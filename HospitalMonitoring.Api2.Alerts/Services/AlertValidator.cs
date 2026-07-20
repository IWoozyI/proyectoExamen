using System.Text.Json;
using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.Api2.Alerts.Services;

public static class AlertValidator
{
    private static readonly HashSet<string> KnownPatients = ["P-001", "P-002", "P-003", "P-004", "P-005"];

    public static (bool IsValid, string Reason) Validate(MedicalAlertDetectedEvent? alertEvent, JsonException? jsonException = null)
    {
        if (jsonException is not null)
        {
            return (false, "Payload JSON corrupto o mal formado.");
        }

        if (alertEvent is null)
        {
            return (false, "El mensaje no contiene una alerta válida.");
        }

        if (string.IsNullOrWhiteSpace(alertEvent.PatientId) || !KnownPatients.Contains(alertEvent.PatientId))
        {
            return (false, $"Paciente desconocido: '{alertEvent.PatientId}'.");
        }

        if (alertEvent.HeartRateBpm < 0 || alertEvent.OxygenSaturation < 0 || alertEvent.TemperatureCelsius < 0)
        {
            return (false, "Valores vitales negativos no permitidos.");
        }

        if (alertEvent.SystolicBp < alertEvent.DiastolicBp)
        {
            return (false, "Presión arterial inconsistente (sistólica menor que diastólica).");
        }

        return (true, string.Empty);
    }
}
