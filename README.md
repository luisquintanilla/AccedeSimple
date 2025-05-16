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

   ```bash
   cd src/localguide
   uv sync
   ```

3. **Configure user secrets**

   1. Navigate to the *_src/AccedeSimple.AppHost* project.
   1. Set the following user secrets. i.e. `dotnet user-secrets set "AzureOpenAI:ResourceGroup" "YOUR-VALUE"`
      - **AzureOpenAI:ResourceGroup** - The name of your Azure Resource Group where the OpenAI Resource is deployed to
      - **AzureOpenAI:ResourceName** - The name of your Azure OpenAI Resource
      - **Azure:SubscriptionId** - The subscription ID you deployed your resources to 
      - **Azure:ResourceGroup** - The name of your Azure OpenAI Resource is deployed to.
      - **Azure:Location** - The location you deployed your Azure OpenAI Resource to.
      - **Azure:AllowResourceGroupCreation**  - Set to *false* to use existing resource.

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

