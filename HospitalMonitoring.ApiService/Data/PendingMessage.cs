namespace HospitalMonitoring.ApiService.Data;

public class PendingMessage
{
    public Guid Id { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public bool IsProcessed { get; set; }
}
