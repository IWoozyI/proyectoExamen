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
            _logger.LogInformation("Conexión ya existente. Estado: {State}", _connection.State);
            
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
            else
            {
                return;
            }
        }

        var hubUrl = ResolveHubUrl();
        _logger.LogInformation("Conectando a SignalR Hub: {HubUrl}", hubUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // 🔥 CONFIGURACIÓN DE TRANSPORTE
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransports.All;
                
                // 🔥 DESACTIVAR SKIP NEGOTIATION PARA QUE FUNCIONE CON ALL TRANSPORTS
                options.SkipNegotiation = false;
            })
            .WithAutomaticReconnect(new[] 
            { 
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _connection.On<NursingAlertNotification>("ReceiveAlert", alert =>
        {
            _logger.LogInformation("✅ Alerta recibida: Paciente {PatientId}, Severidad {Severity}", 
                alert.PatientId, alert.Severity);
            OnAlertReceived?.Invoke(alert);
        });

        _connection.Reconnecting += _ =>
        {
            _logger.LogWarning("🔄 Reconectando con API 2...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            _logger.LogInformation("✅ Reconectado exitosamente a API 2.");
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            _logger.LogWarning("⚠️ Conexión cerrada con API 2.");
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("✅ Estación de enfermería conectada a {HubUrl}", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al conectar con el Hub: {Message}", ex.Message);
            throw;
        }
    }

    private string ResolveHubUrl()
    {
        var hubUrl = _configuration["SignalR:HubUrl"];
        if (!string.IsNullOrEmpty(hubUrl))
        {
            return hubUrl;
        }

        // 🔥 USAR EL PUERTO CORRECTO DE api2-alerts
        return "http://localhost:5002/hubs/alerts";
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}