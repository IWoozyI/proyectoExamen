using HospitalMonitoring.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace HospitalMonitoring.Web.Services;

public sealed class NursingAlertService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NursingAlertService> _logger;

    public event Action<NursingAlertNotification>? OnAlertReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public NursingAlertService(IConfiguration configuration, ILogger<NursingAlertService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        var hubUrl = ResolveHubUrl();
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NursingAlertNotification>("ReceiveAlert", alert =>
        {
            _logger.LogInformation("Alerta recibida en estación de enfermería: {PatientId}", alert.PatientId);
            OnAlertReceived?.Invoke(alert);
        });

        _connection.Reconnecting += _ =>
        {
            _logger.LogWarning("Reconectando con API 2...");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(cancellationToken);
        _logger.LogInformation("Estación de enfermería conectada a {HubUrl}", hubUrl);
    }

    private string ResolveHubUrl()
    {
        var baseUrl = _configuration["services:api2-alerts:http:0"]
            ?? _configuration["Services:api2-alerts:http:0"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "http://localhost:5000/hubs/alerts";
        }

        return $"{baseUrl.TrimEnd('/')}/hubs/alerts";
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
