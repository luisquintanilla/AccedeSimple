#pragma warning disable
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);


// Configure Azure Services
var azureStorage = builder.AddAzureStorage("storage");


if (builder.Environment.IsDevelopment())
{
    azureStorage.RunAsEmulator(c => {
        c.WithDataBindMount();
        c.WithLifetime(ContainerLifetime.Persistent);
    });    
}

// Use existing resources
var azureOpenAIResource = builder.AddParameterFromConfiguration("AzureOpenAIResourceName", "AzureOpenAI:ResourceName");
var azureOpenAIResourceGroup = builder.AddParameterFromConfiguration("AzureOpenAIResourceGroup","AzureOpenAI:ResourceGroup");
var azureOpenAIEndpoint = builder.AddParameterFromConfiguration("AzureOpenAIEndpoint", "AzureOpenAI:Endpoint");

var openai = 
    builder.AddAzureOpenAI("openai")
        .AsExisting(azureOpenAIResource, azureOpenAIResourceGroup);

var mcpServer = 
    builder.AddProject<Projects.AccedeSimple_MCPServer>("mcpserver")
        .WithReference(openai)
        .WaitFor(openai);
    

var pythonApp =
    builder.AddPythonApp("localguide", "../localguide", "main.py")
        .WithHttpEndpoint(env: "PORT", port: 8000, isProxied: false)
        .WithEnvironment("AZURE_OPENAI_ENDPOINT", azureOpenAIEndpoint)
        .WithOtlpExporter();
    // .PublishAsDockerFile();

var backend = 
    builder
        .AddProject<Projects.AccedeSimple_Service>("backend")
        .WithReference(openai)
        .WithReference(mcpServer)
        .WithReference(pythonApp)
        .WithReference(azureStorage.AddBlobs("uploads"))
        .WaitFor(openai);

builder.AddNpmApp("webui","../webui")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "PORT", port: 35_369, isProxied: false)
    .WithEnvironment("BACKEND_URL", backend.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .PublishAsDockerFile();

builder.Build().Run();
#pragma warning restore