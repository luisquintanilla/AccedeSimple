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
    departureTime: string;
    arrivalTime: string;
    price: number;
    duration: string;
    hasLayovers: boolean;
    cabinClass?: string;
}

export interface Hotel {
    propertyName: string;
    chain: string;
    address: string;
    checkIn: string;
    checkOut: string;
    nightCount: number;
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
    pickupTime: string;
    dropoffTime: string;
    dailyRate: number;
    totalPrice: number;
    unlimitedMileage: boolean;
}