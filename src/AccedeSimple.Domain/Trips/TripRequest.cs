using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AccedeSimple.Domain;

[Description("A request for trip booking based on selected options")]
public record TripRequest(
    [Required]
    [StringLength(50)]
    [Description("Unique identifier for the request")]
    string RequestId,
    
    [Required]
    [Description("Selected trip option to be booked")]
    TripOption TripOption,
    
    [Description("Additional booking instructions or requirements")]
    string? AdditionalNotes = null);

[Description("Status of a trip request in its approval and booking lifecycle")]
public enum TripRequestStatus
{
    [Description("Request is awaiting approval")]
    Pending = 0,
    
    [Description("Request has been approved")]
    Approved = 1,
    
    [Description("Request has been rejected")]
    Rejected = 2,
    
    [Description("Trip request has been cancelled")]
    Cancelled = 3
}

[Description("Result of processing a trip request")]
public record TripRequestResult(
    [Required]
    [StringLength(50)]
    [Description("Reference to the original trip request")]
    string RequestId,
    
    [Required]
    [Description("Current status of the trip request")]
    TripRequestStatus Status,
    
    [Description("Notes from the approval process")]
    string? ApprovalNotes,
    
    [Required]
    [Description("Date and time when the request was processed")]
    DateTime ProcessedDateTime);
