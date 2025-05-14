using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AccedeSimple.Domain;

[Description("A travel option presented to the user for selection")]
public record TripOption(
    [Required]
    [Description("Unique identifier for this travel option")]
    string OptionId,
    
    [Required]
    [Description("List of flights included in this option")]
    IReadOnlyList<Flight> Flights,
    
    [Description("Hotel details if included in this option")]
    Hotel? Hotel,
    
    [Description("Car rental details if included in this option")]
    CarRental? Car,
    
    [Range(0, double.MaxValue)]
    [Description("Total cost for all components of this travel option")]
    decimal TotalCost,
    
    [Required]
    [Description("Human-readable description of this travel option")]
    string Description
    
    // [Description("Whether this option meets all user requirements")]
    // bool MeetsRequirements = true,
    
    // [Description("Any special conditions or restrictions")]
    // List<string>? Conditions = null
);
