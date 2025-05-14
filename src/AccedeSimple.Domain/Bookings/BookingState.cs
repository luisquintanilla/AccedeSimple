namespace AccedeSimple.Domain;

// NOTE: I don't think we need this class. We can just use the TripRequest class directly.
public class BookingState
{
    public string? CurrentBookingId { get; set; }
    public TripRequest? Request { get; set; }
    public Flight[]? FoundFlights { get; set; }
    public Hotel[]? FoundHotels { get; set; }
    public CarRental[]? FoundCarRentals { get; set; }
    // public BookingConfirmation? Confirmation { get; set; }
}