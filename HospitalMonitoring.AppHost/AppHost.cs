var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("telemetrydb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var api1Telemetry = builder.AddProject<Projects.HospitalMonitoring_ApiService>("api1-telemetry")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

var api2Alerts = builder.AddProject<Projects.HospitalMonitoring_Api2_Alerts>("api2-alerts")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.HospitalMonitoring_Web>("client2-nursing-station")
    .WithExternalHttpEndpoints()
    .WithReference(api2Alerts)
    .WaitFor(api2Alerts)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.HospitalMonitoring_Client1_Simulator>("client1-simulator")
    .WithReference(api1Telemetry)
    .WaitFor(api1Telemetry);

builder.Build().Run();
