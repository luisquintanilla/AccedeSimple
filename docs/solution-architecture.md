# Solution Architecture

This document provides a visual representation of the solution architecture for the Accede Travel Concierge application. The diagram below illustrates the main components.

```mermaid
graph TD
    Service["Backend<br>(Agentic Workflows)"]
    MCPServer["MCPServer"]
    WebUI["Web UI"]
    LocalGuide["Local Guide Agent"]

    %% Relationships
    WebUI -->|Uses APIs| Service
    LocalGuide -->|Provides Data| Service
    Service -->|Interacts With| MCPServer
    MCPServer -->|Processes Requests| Service
    Service -->|Manages| LocalGuide
```