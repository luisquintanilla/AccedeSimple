using System;
using System.ComponentModel;

namespace AccedeSimple.Domain
{
    [Description("Details about a travel trip")]
    public record TripDetails(
        [Description("Unique identifier for the trip")] string TripId,
        [Description("ID of employee taking the trip")] string EmployeeId,
        [Description("Description of the trip")] string Description,
        [Description("Trip start date")] DateTime StartDate,
        [Description("Trip end date")] DateTime EndDate,
        [Description("Purpose of travel")] string Purpose,
        [Description("Destination location")] string Destination
    );
}