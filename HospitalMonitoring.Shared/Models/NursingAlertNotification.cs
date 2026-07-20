namespace HospitalMonitoring.Shared.Models;

public record NursingAlertNotification(
    Guid EventId,
    string PatientId,
    AlertSeverity Severity,
    string AnomalyType,
    string Description,
    int HeartRateBpm,
    double OxygenSaturation,
    DateTime ReceivedAtUtc);
