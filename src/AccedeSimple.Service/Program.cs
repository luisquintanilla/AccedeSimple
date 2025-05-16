#pragma warning disable
using System.ComponentModel;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using AccedeSimple.Domain;
using AccedeSimple.Service.ProcessSteps;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AccedeSimple.Service;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Concurrent;
using AccedeSimple.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("LocalGuide", c =>
    {
        c.BaseAddress = new Uri("http://localguide");
    });

// Load configuration
builder.Services.Configure<UserSettings>(builder.Configuration.GetSection("UserSettings"));

// Add state stores
builder.Services.AddSingleton<StateStore>();
builder.Services.AddKeyedSingleton<ConcurrentDictionary<string,List<ChatItem>>>("history");

// Add storage
builder.AddKeyedAzureBlobClient("uploads");

// Configure logging
builder.Services.AddLogging();

// Chat message stream for SSE
builder.Services.AddSingleton<ChatStream>();

// In-memory storage for trip requests
builder.Services.AddSingleton<IList<TripRequest>>(new List<TripRequest>());

builder.AddServiceDefaults();

builder.Services.AddMcpClient();

var kernel = builder.Services.AddKernel();

kernel.Services.AddChatClient(modelName: "gpt-4.1");

kernel.Services.AddTransient<ProcessService>();
kernel.Services.AddTransient<MessageService>();

builder.Services.AddTravelProcess();

var app = builder.Build();

app.MapEndpoints();

app.Run();

public class UserSettings
{
    public string UserId { get; set; }
    public string AdminUserId { get; set; }

}