using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AccedeSimple.Domain;

public enum BookingStatus
{
    [Description("Booking is temporarily held but not confirmed")]
    Reserved = 0,

    [Description("Booking is fully confirmed with all providers")]
    Confirmed = 1,
    
    [Description("Some parts of the booking are confirmed while others are pending")]
    PartiallyConfirmed = 2,

    [Description("Booking process has failed")]
    Failed = 3,
    
    [Description("Booking has been cancelled")]
    Cancelled = 4,

    [Description("Trip has been completed")]
    Completed = 5
}

public record Flight(
    [Required]
    [Description("Unique identifier for the flight")]
    string FlightNumber,
    
    [Required]
    [Description("Name of the operating airline")]
    string Airline,
        
    [Required]
    [Description("Departure airport code")]
    string Origin,
    
    [Required]
    [Description("Arrival airport code")]
    string Destination,

    [Description("Scheduled departure time. Use ISO 8601 format.")]
    DateTime DepartureDateTime,

    [Description("Scheduled arrival time. Use ISO 8601 format.")]
    DateTime ArrivalDateTime,

    [Required]
    [Description("Flight class (e.g., Economy, Business)")]
    string CabinClass,
    
    [Range(0, Single.MaxValue)]
    [Description("Price of the flight ticket")]
    float Price,
    
    [Description("Duration of the flight in hours and minutes (HH:MM)")]
    string Duration,
    
    [Description("Whether the flight is direct or has layovers")]
    bool HasLayovers);

public record Hotel(
    // [Required]
    // string HotelId,
    
    [Required]
    [Description("Name of the hotel property")]
    string HotelName,
    
    [Required]
    [Description("Name of the hotel chain or brand")]
    string HotelChain,
    
    [Required]
    [Description("Physical location of the hotel")]
    string Address,
    
    [Required]
    [Description("Date and time of check-in. Use ISO 8601 format.")]
    DateTime CheckIn,
    
    [Required]
    [Description("Date and time of check-out. Use ISO 8601 format.")]
    DateTime CheckOut,
    
    [Required]
    [Description("Number of nights for the booking")]
    int NumberOfNights,

    [Required]
    [Description("Number of guests for the booking")]
    int NumberOfGuests,

    [Range(0, Single.MaxValue)]
    [Description("Price per night for the room")]
    float PricePerNight,
    
    [Range(0, Single.MaxValue)]
    [Description("Total price for the hotel stay")]
    float TotalPrice,

    [Description("Type of room booked (e.g., Single, Double)")]
    string RoomType,

    [Description("Whether breakfast is included (breakfast)")]
    bool BreakfastIncluded);

public record CarRental(
    [Required]
    [Description("Name of car rental company")]
    string Company,

    [Required]
    [Description("Type of car (e.g., SUV, Sedan)")]
    string CarType,

    [Required]
    string PickupLocation,

    [Required]

    string DropoffLocation,
    

    [Required]
    [Description("Date and time of car pickup. Use ISO 8601 format.")]
    DateTime PickupDateTime,

    [Required]
    [Description("Date and time of car drop-off. Use ISO 8601 format.")]
    DateTime ReturnDateTime,
    
    [Range(0, Single.MaxValue)]
    [Description("Price per day for the car rental")]
    float DailyRate,

    [Range(0, Single.MaxValue)]
    [Description("Total price for the car rental")]
    float TotalPrice,
    
    [Description("Whether rental inclueds unlimited mileage")]
    bool UnlimitedMileage);

public record TravelItinerary(
    Flight[] Flights,
    Hotel[] Hotels,
    CarRental[] CarRentals,
    string RequestId);

public record BookingConfirmation(
    [Required]
    [StringLength(50)]
    string BookingId,
    
    [Range(0, double.MaxValue)]
    decimal TotalPrice,
    
    DateTime BookingDateTime,
    
    BookingStatus Status,
    
    [Required]
    string[] ConfirmationNumbers,
    
    BookedServices Services);
    
    // PaymentDetails? Payment);

public record BookedServices(
    Flight[]? Flights,
    Hotel? Hotel,
    CarRental? CarRental);

// public record PaymentDetails(
//     [Required]
//     string PaymentMethod,
    
//     [Required]
//     [StringLength(16, MinimumLength = 15)]
//     string CardNumber,
    
//     [Required]
//     string CardHolder,
    
//     DateTime ExpiryDate,
    
//     [Range(0, double.MaxValue)]
//     decimal Amount,
    
//     [Required]
//     [StringLength(3)]
//     string Currency);