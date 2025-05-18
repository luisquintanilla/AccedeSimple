#pragma warning disable
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using AccedeSimple.Domain;
using AccedeSimple.Service.ProcessSteps;
using AccedeSimple.Service.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using TextContent = Microsoft.Extensions.AI.TextContent;

namespace AccedeSimple.Service;
public static class Endpoints
{

    public static void MapEndpoints(this WebApplication app)
    {
        app.MapEndpoints("/api/admin", group => group.MapAdminEndpoints());
        app.MapEndpoints("/api/chat", group => group.MapChatEndpoints());
    }

    private static void MapEndpoints(this WebApplication app, string basePath, Action<RouteGroupBuilder> configure)
    {
        var group = app.MapGroup(basePath);
        configure(group);
    }

    private static void MapAdminEndpoints(this RouteGroupBuilder group)
    {
        // var group = app.MapGroup("/api/admin");

        // Get pending trip approval requests
        group.MapGet("/requests", async (
            [FromServices] StateStore store,
            [FromServices] ILogger<Program> logger, 
            CancellationToken cancellationToken) =>
        {
            try
            {
                var requests = store.Get("trip-requests").Value as List<TripRequest>;
                return Results.Ok(requests);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching trip requests");
                return Results.Problem("Error fetching trip requests", statusCode: 500);
            }
        });

        // Approve or reject a trip request
        group.MapPost("/requests/approval", async (
            [FromServices] Kernel kernel, 
            [FromServices] KernelProcess process, 
            [FromServices] ILogger<Program> logger,
            TripRequestResult result, 
            CancellationToken cancellationToken) =>
        {
            try
            {
                await process.StartAsync(kernel, new KernelProcessEvent {Id = nameof(ApprovalStep.HandleApprovalResponseAsync), Data = result});
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error submitting trip request result");
                return Results.Problem("Error submitting trip request result", statusCode: 500);
            }
        });        
    }

    private static void MapChatEndpoints(this RouteGroupBuilder group)
    {

        // Stream responses back to the client
        group.MapGet("/stream", async (
            int? startIndex, 
            [FromServices] ChatStream chatStream, 
            HttpResponse response, 
            CancellationToken cancellationToken) => 
        {
            // Create a channel to handle incoming messages
            var channel = Channel.CreateUnbounded<ChatItem>();

            // Disable response buffering
            var bufferingFeature = response.HttpContext.Features.Get<IHttpResponseBodyFeature>();
            bufferingFeature?.DisableBuffering();    

            // Set SSE headers
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache, no-store");
            response.Headers.Add("Connection", "keep-alive");

            // Initialize connected event
            await response.WriteAsync($"event: connected\ndata: {{\"connected\": true}}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
            try
            {
                // Subscribe to the chat stream
                var subscription = 
                    chatStream
                        .Messages
                        .Subscribe(msg => channel.Writer.TryWrite(msg));

                // Read from the channel and write to the response
                await foreach (var chatItem in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    // Check if the message is visible to the user
                    if (chatItem.IsUserVisible)
                    {
                        // Handle the message
                        await HandleMessageAsync(chatItem, response, cancellationToken);
                    }
                }

                await response.WriteAsync($"event: complete\ndata: {{}}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);               
            
                // Wait for the cancellation token to be triggered
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception ex) {
                // Handle any exceptions that occur during streaming
                await response.WriteAsync($"event: error\ndata: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        });


        // Handle incoming messages
        group.MapPost("/messages", async (
            HttpRequest request, 
            [FromKeyedServices("uploads")] BlobServiceClient blobServiceClient, 
            [FromServices] IChatClient chatClient, 
            [FromServices] MessageService messageService,
            [FromServices] ProcessService processService, 
            [FromServices] ChatStream chatStream,
            [FromServices] IOptions<UserSettings> userSettings, 
            CancellationToken cancellationToken) => 
        {

            // Read request body
            var bodyText = request.Form["Text"].FirstOrDefault() ?? "";
            var uploads = await GetFileUploads(userSettings.Value.UserId, request, blobServiceClient, cancellationToken);

            // Create user message
            var userMessage = new UserMessage(bodyText)
            {
                Attachments = uploads,
                Id = Guid.NewGuid().ToString(),
            };
            
            await messageService.AddMessageAsync(userMessage, userSettings.Value.UserId);

            // Identify the user's intent
            var reason = await chatClient.GetResponseAsync<UserIntent>($"Get the user intent: {userMessage.Text}");
            
            reason.TryGetResult(out var intentResult);

            // Add the message to the chat stream
            await processService.ActAsync(intentResult, userMessage);

            return Results.Ok();
        });

        // TODO: This should enable chat to be restored from history
        group.MapGet("/messages", async (
            [FromKeyedServices("history")] ConcurrentDictionary<string, List<ChatItem>> history,
            [FromServices] ChatStream chatStream,
            [FromQuery] string? userId) =>
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Results.BadRequest("User ID is required");
            }

            if (!history.TryGetValue(userId, out var messages))
            {
                return Results.NotFound("No messages found for the specified user ID");
            }

            var visibleMessages = messages.Where(m => m.IsUserVisible).ToList();
            foreach (var message in visibleMessages)
            {
                chatStream.AddMessage(message);
            }

            return Results.Ok(visibleMessages);
        });

        group.MapPost("/stream/cancel", async () =>
        {
            // TODO: Cancel the stream

            return Results.Ok();
        });

        
        group.MapPost("/messages/clear", async (
            [FromKeyedServices("history")] ConcurrentDictionary<string, List<ChatItem>> history,
            [FromQuery] string? userId) =>
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Results.BadRequest("User ID is required");
            }

            // Clear history for the specified user ID
            if (history.TryRemove(userId, out _))
            {
                return Results.Ok("History cleared");
            }
            else
            {
                return Results.NotFound("No history found for the specified user ID");
            }
        });

        // Select an itinerary option
        group.MapPost("/select-itinerary", async (
            [FromServices] MessageService messageService,
            [FromServices] ProcessService processService,
            [FromServices] ILogger<Program> logger,
            [FromServices] IOptions<UserSettings> userSettings,
            SelectItineraryRequest request,
            CancellationToken cancellationToken) =>
        {
            try 
            {

                var input = new ItinerarySelectedChatItem($"I have selected an itinerary option. {request.OptionId}")
                {
                    MessageId = request.MessageId,
                    OptionId = request.OptionId
                };
                
                await messageService.AddMessageAsync(input, userSettings.Value.UserId);

                await processService.ActAsync(UserIntent.StartTripApproval, input);

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error selecting itinerary");
                return Results.Problem("Error selecting itinerary", statusCode: 500);
            }
        });                        
    }

    private static ChatMessage CreateChatMessageWithAttachments(List<UriAttachment> attachments, string text)
    {
        var content = new List<AIContent>(attachments.Count + 1) { new TextContent(text) };
        foreach (var attachment in attachments)
        {
            if (attachment.Uri.StartsWith("data:"))
            {
                content.Add(new DataContent(attachment.Uri, attachment.ContentType));
            }
            else
            {
                content.Add(new UriContent(attachment.Uri, attachment.ContentType));
            }
        }

        return new ChatMessage(ChatRole.User, content);
    }

    private static async Task HandleMessageAsync(ChatItem chatItem, HttpResponse response, CancellationToken cancellationToken)
    {
        try 
        {        

            // Handle the message based on its type
            if (chatItem is AssistantResponse assistantResponse)
            {
                // Handle assistant response
                await response.WriteAsync($"data: {JsonSerializer.Serialize(assistantResponse, JsonSerializerOptions.Web)}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
            else if (chatItem is CandidateItineraryChatItem itineraryItem)
            {
                // Handle candidate itinerary
                var serializedMessage = JsonSerializer.Serialize(itineraryItem, JsonSerializerOptions.Web);
                await response.WriteAsync($"data: {serializedMessage}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }

            // Handle completion
            await response.WriteAsync($"event: complete\ndata: {{}}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);    
        }
        catch(OperationCanceledException) {}
    }

    private static async Task<List<UriAttachment>> GetFileUploads(string userId, HttpRequest request, BlobServiceClient blobServiceClient, CancellationToken cancellationToken)
    {
        List<UriAttachment> results = [];
        if (request.HasFormContentType && request.Form.Files.Count > 0)
        {
            foreach (var file in request.Form.Files)
            {
                if (blobServiceClient.AccountName == "devstoreaccount1")
                {
                    // These URIs are sent to LLM APIs, which don't have access to the local storage emulator, so we need to convert
                    // them to data URIs in that case.
                    results.Add(new(await ConvertToDataUri(file.OpenReadStream(), file.ContentType, cancellationToken), file.ContentType));
                }
                else
                {
                    results.Add(await UploadToBlobContainerAsync(userId, blobServiceClient, file, cancellationToken));
                }
            }
        }

        return results;
    }

    private static async Task<UriAttachment> UploadToBlobContainerAsync(string userId, BlobServiceClient blobServiceClient, IFormFile file, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient("user-content");
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var extension = Path.GetExtension(file.FileName) ?? "jpg";
        var blobClient = containerClient.GetBlobClient($"{userId}/{Guid.CreateVersion7():N}.{extension}");
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.MaxValue,
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        if (blobClient.CanGenerateSasUri)
        {
            return new(blobClient.GenerateSasUri(sasBuilder).ToString(), file.ContentType);
        }
        else
        {
            var userDelegationKey = blobServiceClient.GetUserDelegationKey(DateTimeOffset.UtcNow,
                                                                        DateTimeOffset.UtcNow.AddHours(2));
            var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
            };

            return new(blobUriBuilder.ToUri().ToString(), file.ContentType);
        }

        // return new(blobClient.GenerateSasUri(sasBuilder).ToString(), file.ContentType);
    }

    private static async ValueTask<string> ConvertToDataUri(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        return $"data:{contentType};base64,{base64}";
    }

    public record SelectItineraryRequest(string MessageId, string OptionId);    

    public readonly record struct UriAttachment(string Uri, string ContentType);
}
#pragma warning restore