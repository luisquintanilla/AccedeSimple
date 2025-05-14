namespace AccedeSimple.Domain;

public class ApprovalResponse
{
    public string RequestId { get; set; } = "";
    public bool IsApproved { get; set; }
    public string Reason { get; set; } = "";
    public string ApproverId { get; set; } = "";
    public ApprovalAnalysis? Analysis { get; set; }
    public DateTime ResponseDate { get; set; } = DateTime.UtcNow;
}