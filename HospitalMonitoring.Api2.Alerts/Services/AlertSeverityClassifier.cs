using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.Api2.Alerts.Services;

public static class AlertSeverityClassifier
{
    public static AlertSeverity Classify(MedicalAlertDetectedEvent alertEvent)
    {
        if (alertEvent.AnomalyType is "LowOxygenSaturation" or "HeartRateAnomaly"
            && (alertEvent.OxygenSaturation < 85 || alertEvent.HeartRateBpm is < 35 or > 130))
        {
            return AlertSeverity.Critical;
        }

        return alertEvent.AnomalyType switch
        {
            "LowOxygenSaturation" => AlertSeverity.High,
            "HeartRateAnomaly" => AlertSeverity.High,
            "HypertensionCrisis" => AlertSeverity.Medium,
            "TemperatureAnomaly" => AlertSeverity.Medium,
            "InvalidReading" => AlertSeverity.Low,
            _ => AlertSeverity.Low
        };
    }
}
