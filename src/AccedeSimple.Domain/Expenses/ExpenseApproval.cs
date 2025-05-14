using System;

namespace AccedeSimple.Domain;

public class ExpenseApproval
{
    public string ExpenseReportId { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime ApprovalDate { get; set; }
    public string Status { get; set; }
    public string Comments { get; set; }
    public List<string> RejectedReceiptIds { get; set; } = new();
}