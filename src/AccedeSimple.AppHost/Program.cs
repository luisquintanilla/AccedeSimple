#pragma warning disable
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Define parameters for Azure OpenAI
var azureOpenAIResource = builder.AddParameterFromConfiguration("AzureOpenAIResourceName", "AzureOpenAI:ResourceName");
var azureOpenAIResourceGroup = builder.AddParameterFromConfiguration("AzureOpenAIResourceGroup","AzureOpenAI:ResourceGroup");
var azureOpenAIEndpoint = builder.AddParameterFromConfiguration("AzureOpenAIEndpoint", "AzureOpenAI:Endpoint");
var modelName = "gpt-4.1";


// Configure Azure Services
var azureStorage = builder.AddAzureStorage("storage");
var openai = 
    builder.AddAzureOpenAI("openai")
        .AsExisting(azureOpenAIResource, azureOpenAIResourceGroup);

if (builder.Environment.IsDevelopment())
{
    azureStorage.RunAsEmulator(c => {
        c.WithDataBindMount();
        c.WithLifetime(ContainerLifetime.Persistent);
    });    
}

// Configure projects
var mcpServer =
    builder.AddProject<Projects.AccedeSimple_MCPServer>("mcpserver")
        .WithReference(openai)
        .WithEnvironment("MODEL_NAME", modelName)
        .WaitFor(openai);


var pythonApp =
    builder.AddPythonApp("localguide", "../localguide", "main.py")
        .WithHttpEndpoint(env: "PORT", port: 8000, isProxied: false)
        .WithEnvironment("AZURE_OPENAI_ENDPOINT", azureOpenAIEndpoint)
        .WithEnvironment("MODEL_NAME", modelName)
        .WithOtlpExporter()
        .WaitFor(openai);

var azureSubscriptionId = builder.AddParameterFromConfiguration("AzureSubscriptionId", "Azure:SubscriptionId");
var azureResourceGroup = builder.AddParameterFromConfiguration("AzureResourceGroup", "Azure:ResourceGroup");
var azureAIFoundryProject = builder.AddParameterFromConfiguration("AzureAIFoundryProject", "AzureAIFoundry:Project");

var backend =
    builder
        .AddProject<Projects.AccedeSimple_Service>("backend")
        .WithReference(openai)
        .WithReference(mcpServer)
        .WithReference(pythonApp)
        .WithReference(azureStorage.AddBlobs("uploads"))
        .WithEnvironment("MODEL_NAME", modelName)
        .WithEnvironment("AZURE_SUBSCRIPTION_ID", azureSubscriptionId)
        .WithEnvironment("AZURE_RESOURCE_GROUP", azureOpenAIResourceGroup)
        .WithEnvironment("AZURE_AI_FOUNDRY_PROJECT", azureAIFoundryProject)
        .WaitFor(openai);

builder.AddNpmApp("webui", "../webui")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "PORT", port: 35_369, isProxied: false)
    .WithEnvironment("BACKEND_URL", backend.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WaitFor(backend)
    .PublishAsDockerFile();

builder.Build().Run();
#pragma warning restore