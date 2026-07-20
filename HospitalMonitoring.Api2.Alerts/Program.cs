using HospitalMonitoring.Api2.Alerts.BackgroundServices;
using HospitalMonitoring.Api2.Alerts.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

// 🔥 AGREGAR CORS ANTES DE builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
        // NOTA: No uses AllowCredentials con AllowAnyOrigin
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<INursingAlertNotifier, SignalRNursingAlertNotifier>();
builder.Services.AddHostedService<MedicalAlertConsumerService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// 🔥 USAR CORS ANTES DE MapHub
app.UseCors("AllowAll");

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