using System;

namespace AccedeSimple.Domain;

public class ApprovalState
{
    public string RequestId { get; set; } = "";
    public TripRequestStatus Status { get; set; } = TripRequestStatus.Pending;
}