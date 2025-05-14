import { TripOption } from './TripTypes';

// Enum that matches the C# TripRequestStatus enum
// Using numeric values to match how C# serializes enums
export enum TripRequestStatus {
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

// Interface that matches the C# TripRequest record
export interface TripRequest {
    requestId: string;
    tripOption: TripOption;
    additionalNotes?: string;
}

// Interface that matches the C# TripRequestResult record
export interface TripRequestResult {
    requestId: string;
    status: TripRequestStatus;
    approvalNotes: string | null;
    processedTime: string;
}