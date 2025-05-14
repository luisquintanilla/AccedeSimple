# Accede Travel Concierge

## Introduction

Accede Travel Concierge is a modular application designed to streamline travel planning and expense management. The project is structured into three main components:

## .NET Projects

- **`AccedeSimple.AppHost/`**: Serves as the entry point and host for the application, managing configuration and startup logic.
- **`AccedeSimple.Service/`**: Implements the core business logic and service layer, handling travel planning, approvals, and expense processing.
- **`AccedeSimple.ServiceDefaults/`**: Provides default implementations and shared utilities to support the service layer.

### Other projects

- **webui** - React web application
- **localguide** - The FastAPI Web API provides endpoints for retrieving city attractions using an AI agent. This feature enhances the travel planning experience by offering intelligent recommendations tailored to user preferences.

## Prerequisites

To run the application, ensure the following tools and frameworks are installed:

- [.NET 9 SDK or greater](https://dotnet.microsoft.com/download)
- [Python 3.12 or greater](https://www.python.org/downloads/)
- [UV](https://docs.astral.sh/uv/)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows) (if applicable)

## Quick Start

Follow these steps to set up and run the application:

### Environemnt variables

Set the following environment variables

- **AOAI_ENDPOINT** - Your Azure OpenAI Endpoint
- **OPENAI_API_KEY** - Your OpenAI Endpoint

### Running the app

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-repo/AccedeSimple.git
   cd AccedeSimple
   ```

2. **Install dependencies**:
   Ensure you have the required .NET SDK installed, then restore dependencies:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   Start the application using the .NET CLI:
   ```bash
   dotnet run --project src/AccedeSimple.AppHost
   ```

You're now ready to use the Accede Travel Concierge application!
