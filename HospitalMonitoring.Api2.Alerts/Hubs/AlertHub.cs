using Microsoft.AspNetCore.SignalR;

namespace HospitalMonitoring.Api2.Alerts.Hubs;

public interface INursingAlertNotifier
{
    Task NotifyAsync(Shared.Models.NursingAlertNotification notification, CancellationToken cancellationToken);
}

public sealed class SignalRNursingAlertNotifier(IHubContext<AlertHub> hubContext) : INursingAlertNotifier
{
    public Task NotifyAsync(Shared.Models.NursingAlertNotification notification, CancellationToken cancellationToken)
        => hubContext.Clients.All.SendAsync("ReceiveAlert", notification, cancellationToken);
}

public class AlertHub : Hub;
