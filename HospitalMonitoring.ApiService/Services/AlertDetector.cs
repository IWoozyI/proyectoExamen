using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.ApiService.Services;

public static class AlertDetector
{
    public static (bool IsAnomaly, string AnomalyType, string Description) Detect(VitalSignsReading reading)
    {
        if (reading.HeartRateBpm < 0 || reading.SystolicBp < 0 || reading.DiastolicBp < 0
            || reading.OxygenSaturation < 0 || reading.TemperatureCelsius < 0)
        {
            return (true, "InvalidReading", "Valores negativos detectados en signos vitales.");
        }

        if (reading.HeartRateBpm is < 40 or > 120)
        {
            return (true, "HeartRateAnomaly",
                $"Frecuencia cardiaca fuera de rango: {reading.HeartRateBpm} bpm.");
        }

        if (reading.OxygenSaturation < 90)
        {
            return (true, "LowOxygenSaturation",
                $"Saturación de oxígeno crítica: {reading.OxygenSaturation:F1}%.");
        }

        if (reading.SystolicBp > 180 || reading.DiastolicBp > 120)
        {
            return (true, "HypertensionCrisis",
                $"Presión arterial elevada: {reading.SystolicBp}/{reading.DiastolicBp} mmHg.");
        }

        if (reading.TemperatureCelsius is < 35 or > 39)
        {
            return (true, "TemperatureAnomaly",
                $"Temperatura fuera de rango: {reading.TemperatureCelsius:F1}°C.");
        }

        return (false, string.Empty, string.Empty);
    }
}
