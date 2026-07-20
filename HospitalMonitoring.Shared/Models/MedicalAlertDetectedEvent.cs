namespace HospitalMonitoring.Shared.Models;

public record MedicalAlertDetectedEvent(
    Guid EventId,
    string PatientId,
    string DeviceId,
    string AnomalyType,
    string Description,
    int HeartRateBpm,
    int SystolicBp,
    int DiastolicBp,
    double OxygenSaturation,
    double TemperatureCelsius,
    DateTime DetectedAtUtc);
