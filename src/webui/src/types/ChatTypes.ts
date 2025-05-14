import { TripRequestResult } from './AdminTypes';
import { TripOption, Flight, Hotel, CarRental } from './TripTypes';

// Base message interface
export interface BaseMessage {
    id: string;
    text: string;
    role: string;
    type: string;
    attachments?: FileAttachment[];
}

// User message type
export interface UserMessage extends BaseMessage {
    role: 'user';
    type: 'user';
}

// Assistant message type
export interface AssistantMessage extends BaseMessage {
    role: 'assistant';
    type: 'assistant';
    isFinal?: boolean;
}

// Preference updated message type
export interface PreferenceUpdatedMessage extends BaseMessage {
    role: 'assistant';
    type: 'preference-updated';
}

// Candidate itineraries message type
export interface CandidateItinerariesMessage extends BaseMessage {
    role: 'assistant';
    type: 'candidate-itineraries';
    options: TripOption[];
}

// Trip request updated message type
export interface TripRequestUpdatedMessage extends BaseMessage {
    role: 'assistant';
    type: 'trip-request-updated';
}

// Trip approval result message type
export interface TripApprovalResultMessage extends BaseMessage {
    role: 'assistant';
    type: 'trip-approval-result';
    result: TripRequestResult;
}

// Union type for all message types
export type Message = 
    | UserMessage 
    | AssistantMessage 
    | PreferenceUpdatedMessage 
    | CandidateItinerariesMessage
    | TripRequestUpdatedMessage
    | TripApprovalResultMessage
    | BaseMessage; // Fallback for other message types

export interface FileAttachment {
    uri: string;
    contentType: string;
}
