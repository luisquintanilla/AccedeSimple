using System.ComponentModel;

namespace AccedeSimple.Domain;

[Description("User intent for the travel and expense process")]
public enum UserIntent
{
    [Description("General inquiry or request")]
    General,

    [Description("Ask about attractions or activities in a specific location")]
    AskLocalGuide,

    [Description("Request for travel planning and find trip options")]
    StartTravelPlanning,
    
    [Description("Request for trip approval from admin")]
    StartTripApproval,
    
    [Description("Extract information from receipts")]
    ProcessReceipts,

    [Description("Generate an expense report based on receipts")]
    GenerateExpenseReport,
}