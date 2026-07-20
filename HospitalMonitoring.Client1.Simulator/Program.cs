using HospitalMonitoring.Client1.Simulator;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("telemetry-api", client =>
{
    client.BaseAddress = new("https+http://api1-telemetry");
});

builder.Services.AddHostedService<DeviceSimulatorWorker>();

var host = builder.Build();
host.Run();
