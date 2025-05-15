using System.ComponentModel;
using AccedeSimple.Domain;
using ModelContextProtocol.Server;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddChatClient(modelName: "gpt-4o-mini");

var app = builder.Build();

app.MapMcp();

app.Run();


[McpServerToolType]
public sealed class FlightTool
{
    [McpServerTool, Description("Returns a list of available trip options based on the provided parameters")]
    public static async ValueTask<List<TripOption>> SearchTripOptions(
        [FromServices] IChatClient chatClient,
        TripParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            // Simulate an external API call
            var res = await chatClient.GetResponseAsync<List<TripOption>>(
                    new ChatMessage(
                        role: ChatRole.User,
                        content: GetPrompt(parameters)));

            res.TryGetResult(out var flights);

            return flights ?? [];
        }
        catch //(Exception ex)
        {
            //logger.LogError(ex, "Error loading flights from JSON file");
            return [];
        }
    }

    private static string GetPrompt(TripParameters parameters)
    {
        return
            $$$"""
            You are a travel agent. Your task is to provide a list of available trip options based on the provided parameters. 
            
            - Make sure that the parameters conform to the user's request. 
            - Make sure to fill in all the required fields.
            - ALWAYS include a human-readable description of the trip option.
            - ALWAYS include a return flight.

            Query: I need to travel from NYC to Seattle on May 19th for 3 days.

            ## Example Output

            {
            "OptionId": "TO123456",
            "Flights": [
                {
                "FlightNumber": "DL245",
                "Airline": "Delta Airlines",
                "Origin": "JFK",
                "Destination": "SEA",
                "DepartureDateTime": "2025-05-19T08:00:00-04:00",
                "ArrivalDateTime": "2025-05-19T11:15:00-07:00",
                "CabinClass": "Economy",
                "Price": 320.0,
                "Duration": "06:15",
                "HasLayovers": false
                },
                {
                "FlightNumber": "DL678",
                "Airline": "Delta Airlines",
                "Origin": "SEA",
                "Destination": "JFK",
                "DepartureDateTime": "2025-05-22T14:30:00-07:00",
                "ArrivalDateTime": "2025-05-22T22:45:00-04:00",
                "CabinClass": "Economy",
                "Price": 310.0,
                "Duration": "05:15",
                "HasLayovers": false
                }
            ],
            "Hotel": {
                "HotelName": "Seattle Downtown Inn",
                "HotelChain": "Hilton",
                "Address": "123 Pike Street, Seattle, WA 98101",
                "CheckIn": "2025-05-19T15:00:00-07:00",
                "CheckOut": "2025-05-22T11:00:00-07:00",
                "NumberOfNights": 3,
                "NumberOfGuests": 1,
                "PricePerNight": 150.0,
                "TotalPrice": 450.0,
                "RoomType": "Single",
                "BreakfastIncluded": true
            },
            "Car": null,
            "TotalCost": 1080.0,
            "Description": "Round-trip Delta flights from JFK to SEA with a 3-night stay at Hilton Seattle Downtown Inn, breakfast included"
            }

            ## Parameters

            {JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true })}

            ## Generated Output
            
            """;
    }

}