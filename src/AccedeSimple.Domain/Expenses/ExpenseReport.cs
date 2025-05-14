using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace AccedeSimple.Domain;

[Description("A report of travel-related expenses")]
public record ExpenseReport(
    [Description("Unique identifier for the report")] string ReportId,
    [Description("Reference to the associated trip")] string TripId,
    [Description("User who submitted the expenses")] string UserId,
    [Description("Total amount of all expenses")] decimal TotalExpenses,
    [Description("Individual expense items")] IReadOnlyList<ExpenseItem> Items,
    [Description("Current status of the report")] ExpenseReportStatus Status,
    [Description("Comments from the approver")] string? ApproverNotes = null
);

[Description("An individual expense item within a report")]
public record ExpenseItem(
    [Description("Description of the expense")] string Description,
    [Description("Amount spent")] decimal Amount,
    [Description("Category of the expense")] ExpenseCategory Category,
    [Description("Date the expense was incurred")] DateTime Date,
    [Description("Reference to the receipt data")] string ReceiptReference,
    [Description("Additional notes about the expense")] string? Notes = null
);

[Description("Status of an expense report")]
public enum ExpenseReportStatus
{
    [Description("Report is being drafted")]
    Draft,
    [Description("Report has been submitted for approval")]
    Submitted,
    [Description("Report is under review")]
    InReview,
    [Description("Report has been approved")]
    Approved,
    [Description("Report has been rejected")]
    Rejected,
    [Description("Report has been processed for payment")]
    Processed,
    [Description("Payment has been completed")]
    Paid
}