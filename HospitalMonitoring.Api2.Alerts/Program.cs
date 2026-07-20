using HospitalMonitoring.Api2.Alerts.BackgroundServices;
using HospitalMonitoring.Api2.Alerts.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddSignalR();
builder.Services.AddSingleton<INursingAlertNotifier, SignalRNursingAlertNotifier>();
builder.Services.AddHostedService<MedicalAlertConsumerService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "API 2 - Procesamiento de Alertas",
    signalRHub = "/hubs/alerts"
}));

app.MapHub<AlertHub>("/hubs/alerts");
app.MapDefaultEndpoints();
app.Run();
