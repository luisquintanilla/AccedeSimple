# Accede Travel Agency

This is a multi-agent sample using Microsoft.Extensions.AI and Semantic Kernel Agents Framework

## Key Components

### Models/
Defines core data structures like `ApprovalRequest`, which encapsulates details of business approval workflows, including request types and statuses.

### ProcessSteps/
Implements workflow logic, such as `ApprovalStep`, which handles trip approval processes and integrates with event-driven systems.

### MyProxyClient.cs
Facilitates external interactions by processing events like `AdminApprovalNeeded` and `TravelBooked`.

### ServiceExtensions.cs
Configures the travel planning workflow, registering steps and defining event-driven transitions.

### Program.cs
The entry point that initializes dependencies, orchestrates workflows, and demonstrates service capabilities with sample data.
## Workflow

```mermaid
sequenceDiagram
    participant User
    participant Liaison
    participant TravelAgency
    participant Admin

    User->>Liaison: Submit travel request
    Liaison-->>TravelAgency: Plan trip
    TravelAgency->>TravelAgency: Find flights
    TravelAgency->>TravelAgency: Find hotel
    TravelAgency->>TravelAgency: Find car
    TravelAgency-->>Liaison: Send Itineraries for consideration
    Liaison->>User: Display candidate itineraries to user
    User->>Liaison: Choose itinerary
    Liaison-->>Admin: Request travel approval
    Admin-->>Liaison: Confirm travel approval
    Liaison->>User: Send approval confirmation
    Liaison-->>TravelAgency: Book travel
    TravelAgency-->>Liaison: Confirm booking
    Liaison->>User: Send booking confirmation
    User->>Liaison: Submit receipts
    Liaison-->>Admin: Process receipts
    User->>Liaison: Generate expense report
    Liaison-->>Admin: Generate expense report
    Admin-->>Liaison: Send expense report
    Liaison->>User: Display expense report
```