#pragma warning disable
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;
using AccedeSimple.Domain;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Linq;
using System.ComponentModel;
using System.Text.Json.Schema;
using System.Text.Json;
using AccedeSimple.Service.Services;
using Microsoft.Extensions.Options;

namespace AccedeSimple.Service.ProcessSteps;

public class ExpenseReportStep : KernelProcessStep
{
    private readonly IChatClient _chatClient;
     private StateStore _state;
    private MessageService _messageService;
    private UserSettings _userSettings;

    public ExpenseReportStep(IChatClient chatClient, StateStore state, MessageService messageService, IOptions<UserSettings> userSettings)
    {
        _chatClient = chatClient;
        _state = state;
        _messageService = messageService;
        _userSettings = userSettings.Value;
    }

    [KernelFunction("GenerateExpenseReportAsync")]
    [Description("Generate an expense report based on receipts and trip details.")]
    public async Task GenerateExpenseReportAsync(
        KernelProcessStepContext context)
    {

        var receipts = _state.Get("receipts").Value as List<ReceiptData>;

        // Convert receipts to expense items
        var expenseItems = receipts.Select(r => new ExpenseItem(
            Description: r.Description,
            Amount: r.Amount,
            Category: r.Category,
            Date: r.Date,
            ReceiptReference: r.Id,
            Notes: null
        )).ToList();

        // Calculate total expenses
        var totalExpenses = expenseItems.Sum(e => e.Amount);

        // Expense report
        var report = new ExpenseReport(
            ReportId: Guid.NewGuid().ToString(),
            TripId: "My Trip ID",
            UserId: "My Employee Id",
            TotalExpenses: totalExpenses,
            Items: expenseItems,
            Status: ExpenseReportStatus.Draft
        );

        // Generate a summary of the expense report
        var summaryPrompt =
            $"""
            You are an expense report assistant. Your task is to generate a summary of the expense report.

            Make sure to include the following information:
            - Total Expenses
            - Items (Description, Amount, Category, Date, Receipt Reference)
            
            The user has provided the following information:
            
            {JsonSerializer.Serialize(report)}
            
            Today's date is: {DateTime.Now.ToString()}
            
            Generate a summary of the expense report:
            """;

        var summaryResponse = await _chatClient.GetResponseAsync(summaryPrompt);
    
        await _messageService.AddMessageAsync(new AssistantResponse(summaryResponse.Text), _userSettings.UserId);
    }
}