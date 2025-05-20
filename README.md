# Accede Travel Concierge

## Introduction

Accede Travel Concierge is a modular application designed to streamline travel planning and expense management. The project is structured into three main components:

## Project Structure

### .NET Projects
- **`AccedeSimple.AppHost/`**: Serves as the entry point and host for the application, managing configuration and startup logic.
- **`AccedeSimple.Service/`**: Implements the core business logic and service layer, handling travel planning, approvals, and expense processing.
- **`AccedeSimple.ServiceDefaults/`**: Provides default implementations and shared utilities to support the service layer.
- **`AccedeSimple.Domain/`**: Contains domain models and logic for features such as approvals, bookings, expenses, trips, and shared utilities.
- **`AccedeSimple.MCPServer/`**: Implements the MCP server functionality for extended capabilities.

### Other Projects

- **`webui/`**: React web application for the user interface.
  - **`src/components/`**: Contains React components like `AdminPage`, `ChatContainer`, and `VirtualizedChatList`.
  - **`src/services/`**: Includes service files like `AdminService.ts` and `ChatService.ts`.
  - **`src/styles/`**: Contains CSS files for styling various components.
  - **`src/types/`**: TypeScript type definitions for the application.
- **`localguide/`**: FastAPI Web API for retrieving city attractions using an AI agent.
  - **`Dockerfile`**: Configuration for containerizing the API.
  - **`main.py`**: Entry point for the FastAPI application.
  - **`pyproject.toml`**: Python project configuration.

## Prerequisites

To run the application, ensure the following tools and frameworks are installed:

- [.NET 9 SDK or greater](https://dotnet.microsoft.com/download)
- [Python 3.12 or greater](https://www.python.org/downloads/)
- [UV](https://docs.astral.sh/uv/)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Azure OpenAI Resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal) with the `gpt-4.1` model deployed and [permissions](https://learn.microsoft.com/azure/ai-services/openai/how-to/role-based-access-control).
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows) (if applicable)

## Quick Start

1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-repo/AccedeSimple.git
   cd AccedeSimple
   ```

2. **Install dependencies**:
   Ensure you have the required .NET SDK installed, then restore dependencies:

   **.NET**

   ```bash
   dotnet restore
   ```

   **Python**

   ```bash
   cd src/localguide
   uv sync
   ```

3. **Configure user secrets**

   1. Navigate to the *_src/AccedeSimple.AppHost* project.
   1. Set the following user secrets. i.e. `dotnet user-secrets set "AzureOpenAI:ResourceGroup" "YOUR-VALUE"`
      - **AzureOpenAI:ResourceGroup** - The name of your Azure Resource Group where the OpenAI Resource is deployed to
      - **AzureOpenAI:ResourceName** - The name of your Azure OpenAI Resource
      - **AzureOpenAI:Endpoint** - The endpoint for youor Azure OpenAI Resource
      - **Azure:SubscriptionId** - The subscription ID you deployed your resources to 
      - **Azure:ResourceGroup** - The name of your Azure OpenAI Resource is deployed to.
      - **Azure:Location** - The location you deployed your Azure OpenAI Resource to.
      - **Azure:AllowResourceGroupCreation**  - Set to *false* to use existing resource.
      - **AzureAIFoundry:Project** - The name of the [Azure AI Foundry Project](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/create-projects) within the above subscription and resource group (and within an AI Foundry Hub) that can be used for content safety evaluations. See [Setting Up Azure AI Foundry for Safety Evaluations](https://devblogs.microsoft.com/dotnet/evaluating-ai-content-safety/#setting-up-azure-ai-foundry-for-safety-evaluations).

### Running the app

1. **Run the application**:

   Start the application using the .NET CLI:
   ```bash
   dotnet run --project src/AccedeSimple.AppHost
   ```

You're now ready to use the Accede Travel Concierge application!

## Deployment

Follow the standard [deployment guidance for Aspire](https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment)

1. In the root directory, run the following command

   ```bash
   azd init
   ```

1. Deploy the app

   ```bash
   azd up
   ```