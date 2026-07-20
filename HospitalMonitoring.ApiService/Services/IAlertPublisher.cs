using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.ApiService.Services;

public interface IAlertPublisher
{
    Task<bool> TryPublishAsync(MedicalAlertDetectedEvent alertEvent, CancellationToken cancellationToken);
}
