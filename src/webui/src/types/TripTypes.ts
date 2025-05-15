// filepath: c:\dev\Accede\src\webui\src\types\TripTypes.ts
// TypeScript interfaces corresponding to C# models in TripOption.cs
export interface TripOption {
    optionId: string;
    flights: Flight[];
    hotel?: Hotel;
    car?: CarRental;
    totalCost: number;
    description: string;
}

export interface Flight {
    flightNumber: string;
    airline: string;
    origin: string;
    destination: string;
    departureDateTime : string;
    arrivalDateTime: string;
    price: number;
    duration: string;
    hasLayovers: boolean;
    cabinClass?: string;
}

export interface Hotel {
    hotelName: string;
    chain: string;
    address: string;
    checkIn: string;
    checkOut: string;
    numberOfNights: number;
    pricePerNight: number;
    totalPrice: number;
    roomType: string;
    breakfastIncluded: boolean;
}

export interface CarRental {
    company: string;
    carType: string;
    pickupLocation: string;
    dropoffLocation: string;
    pickupDateTime: string;
    dropoffDateTime: string;
    dailyRate: number;
    totalPrice: number;
    unlimitedMileage: boolean;
}