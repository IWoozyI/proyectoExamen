namespace HospitalMonitoring.ApiService.Data;

public class VitalSignRecord
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int HeartRateBpm { get; set; }
    public int SystolicBp { get; set; }
    public int DiastolicBp { get; set; }
    public double OxygenSaturation { get; set; }
    public double TemperatureCelsius { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsAnomaly { get; set; }
}
