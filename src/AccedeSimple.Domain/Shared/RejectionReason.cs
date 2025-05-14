using System;

namespace AccedeSimple.Domain;

public class RejectionReason
{
    public string RejectionId { get; set; } = string.Empty;
    public string RejecterId { get; set; } = string.Empty;
    public DateTime RejectionDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public List<string> PolicyViolations { get; set; } = new();
    public Dictionary<string, string> RequiredCorrections { get; set; } = new();
}