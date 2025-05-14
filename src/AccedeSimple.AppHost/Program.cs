#pragma warning disable
var builder = DistributedApplication.CreateBuilder(args);

var azureStorage = builder.AddAzureStorage("storage").RunAsEmulator(c => {
    c.WithDataBindMount();
    c.WithLifetime(ContainerLifetime.Persistent);
});

var mcpServer = builder.AddProject<Projects.AccedeSimple_MCPServer>("mcpserver");

var pythonApp = builder.AddPythonApp("localguide", "../localguide", "main.py")
    .WithHttpEndpoint(env: "PORT", port: 8000, isProxied: false)
    .WithEnvironment("OPENAI_API_KEY", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .WithOtlpExporter();
    // .PublishAsDockerFile();

var backend = builder.AddProject<Projects.AccedeSimple_Service>("backend")
    .WithReference(mcpServer)
    .WithReference(pythonApp)
    .WithReference(azureStorage.AddBlobs("uploads"));

builder.AddNpmApp("webui","../webui")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "PORT", port: 35_369, isProxied: false)
    .WithEnvironment("BACKEND_URL", backend.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .PublishAsDockerFile();

builder.Build().Run();
#pragma warning restore