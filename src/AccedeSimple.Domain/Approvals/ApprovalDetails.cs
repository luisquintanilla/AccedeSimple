namespace AccedeSimple.Domain;

public class ApprovalDetails
{
    public string RequestId { get; set; } = "";
    public bool IsApproved { get; set; }
    public string Reason { get; set; } = "";
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public string? ApproverId { get; set; }
}