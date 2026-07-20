namespace HospitalMonitoring.Shared.Models;

public record VitalSignsReading(
    string PatientId,
    string DeviceId,
    int HeartRateBpm,
    int SystolicBp,
    int DiastolicBp,
    double OxygenSaturation,
    double TemperatureCelsius,
    DateTime RecordedAtUtc);
