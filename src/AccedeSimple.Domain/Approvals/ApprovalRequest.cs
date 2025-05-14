using System;
using System.ComponentModel;

namespace AccedeSimple.Domain;

[Description("Request for approval of a business action")]
public record ApprovalRequest(
    [Description("Unique identifier for the request")] string RequestId,
    [Description("User requesting approval")] string RequesterId,
    [Description("Date request was submitted")] DateTime RequestDate,
    [Description("Type of approval being requested")] ApprovalType Type,
    [Description("Current status of the request")] ApprovalRequestStatus Status,
    [Description("Amount requiring approval if applicable")] decimal AmountToApprove,
    [Description("Justification for the request")] string Justification,
    [Description("Detailed information about what's being approved")] object RequestDetails
);

[Description("Types of approval requests")]
public enum ApprovalType
{
    [Description("Approval for travel arrangements")]
    Travel,
    [Description("Approval for expense reimbursement")]
    Expense,
    [Description("Approval for budget allocation")]
    Budget,
    [Description("Approval for policy exception")]
    PolicyException
}

[Description("Status of an approval request")]
public enum ApprovalRequestStatus
{
    [Description("Request is pending review")]
    Pending,
    [Description("Request is under review")]
    InReview,
    [Description("Request has been approved")]
    Approved,
    [Description("Request has been rejected")]
    Rejected,
    [Description("Request has been cancelled")]
    Cancelled
}