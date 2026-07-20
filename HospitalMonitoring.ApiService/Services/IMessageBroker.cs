using HospitalMonitoring.Shared.Models;

namespace HospitalMonitoring.ApiService.Services;

public interface IMessageBroker
{
    Task<bool> PublishAlertAsync(MedicalAlertDetectedEvent alertEvent, CancellationToken cancellationToken);
}
