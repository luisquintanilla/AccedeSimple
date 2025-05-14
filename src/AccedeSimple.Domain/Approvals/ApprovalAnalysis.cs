namespace AccedeSimple.Domain;

public class ApprovalAnalysis
{
    public bool IsCompliant { get; set; }
    public bool WithinBudget { get; set; }
    public string? Rationale { get; set; }
    public List<string> PolicyViolations { get; set; } = new();
    public Dictionary<string, decimal> CostBreakdown { get; set; } = new();
}