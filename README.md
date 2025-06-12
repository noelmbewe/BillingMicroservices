# BillingMicroservices

C# Microservices Billing System with Lago Integration
This project is a microservices-based billing system built with .NET 9.0, integrated with Lago for billing and RabbitMQ for event-driven communication. It processes event usage data, sends it to Lago for billing calculations, and publishes events for other services to consume.
Table of Contents

Project Overview
Architecture
Prerequisites
Setup Instructions
Configuration
Running the Application
Testing the API
Lago Integration
RabbitMQ Integration
Project Structure
Dependencies
Contributing
License

Project Overview
The Billing Microservices system handles event-based billing for API usage or other measurable events. It integrates with Lago for billing calculations and uses RabbitMQ for asynchronous event publishing. The system is built using a clean architecture approach, separating concerns into Domain, Application, Infrastructure, and API layers.
Architecture

Domain Layer: Contains entities (EventUsage) and value objects (BillingPlan).
Application Layer: Handles business logic, DTOs, and interfaces for services (IBillingService, ILagoService, IMessagePublisher).
Infrastructure Layer: Implements external integrations (Lago, RabbitMQ) and logging.
API Layer: Exposes RESTful endpoints for event usage processing and health checks.
Shared Layer: Contains common utilities and event definitions.

Prerequisites

.NET 9.0 SDK
Docker (for RabbitMQ)
Lago instance (local or cloud-based)
Command-line tools: bash, PowerShell, or equivalent
curl (for testing API endpoints)

Verify .NET installation:
dotnet --version

Install Entity Framework Core tools:
dotnet tool install --global dotnet-ef

Setup Instructions

Clone the Repository (if applicable):
git clone <repository-url>
cd BillingMicroservices



Set Up RabbitMQ:
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management

Access the RabbitMQ Management UI at http://localhost:15672 (default credentials: guest/guest).

Configure Lago:Update src/Services/BillingService/BillingService.API/appsettings.json with your Lago instance details:
{
  "Lago": {
    "BaseUrl": "http://localhost:3000",
    "ApiKey": "your-lago-api-key",
    "BillableMetricCode": "api_calls"
  }
}



Configuration
The primary configuration file is appsettings.json in the BillingService.API project. Key sections include:

Lago: Base URL, API key, and billable metric code.
RabbitMQ: Hostname, port, username, and password.
BillingPlan: Defines the billing plan details (e.g., plan name, currency, charges).
Serilog: Configures logging to console and file.

Example appsettings.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "Lago": {
    "BaseUrl": "http://localhost:3000",
    "ApiKey": "your-lago-api-key",
    "BillableMetricCode": "the_metric code"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  },
  "BillingPlan": {
    "PlanName": "MASM_CLAIMS",
    "PlanCode": "MASM_CLAIMS_plan265",
    "Interval": "Monthly",
    "Currency": "MWK",
    "Description": "Billing Masm claims",
    "Charges": [
      {
        "Model": "Volume",
        "Interval": "Monthly",
        "FirstUnit": 0,
        "LastUnit": 9999,
        "PerUnit": 250.00,
        "FlatFee": 0.00
      },
      {
        "Model": "Volume",
        "Interval": "Monthly",
        "FirstUnit": 10000,
        "LastUnit": -1,
        "PerUnit": 150.00,
        "FlatFee": 0.00
      }
    ]
  }
}

Running the Application

Build the Solution:
dotnet build


Run the API:
cd src\Services\BillingService\BillingService.API
dotnet run

The API will be available at:

https://localhost:7001 or http://localhost:5001
Swagger UI: https://localhost:7001/swagger



Testing the API
Test the event usage endpoint using curl:
https://localhost:7001/api/billing/event-usage
 {
  "event": {
    "transactionId": "7826346bkjhahakh",
    "externalSubscriptionId": "Gift_medi_masm265",
    "code": "masm_claims265"
    
  }
}

Check the health endpoint:
curl https://localhost:7001/api/billing/health

Lago Integration
The system integrates with Lago to process billing events. The LagoService in the Infrastructure layer sends events to Lago's API using the configured base URL and API key. Ensure your Lago instance is running and properly configured with the billable metric code.
RabbitMQ Integration
RabbitMQ is used for publishing events (e.g., billing.event.processed) to other services. The RabbitMqPublisher class handles message publishing to the billing-events topic exchange. Ensure RabbitMQ is running and accessible at localhost:5672.
Project Structure
BillingMicroservices/
├── src/
│   ├── Services/
│   │   ├── BillingService/
│   │   │   ├── BillingService.API/
│   │   │   │   ├── Controllers/
│   │   │   │   ├── Program.cs
│   │   │   │   ├── appsettings.json
│   │   │   │   └── BillingService.API.csproj
│   │   │   ├── BillingService.Application/
│   │   │   │   ├── DTOs/
│   │   │   │   ├── Interfaces/
│   │   │   │   ├── Services/
│   │   │   │   └── BillingService.Application.csproj
│   │   │   ├── BillingService.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   ├── ValueObjects/
│   │   │   │   └── BillingService.Domain.csproj
│   │   │   └── BillingService.Infrastructure/
│   │   │       ├── External/
│   │   │       ├── Messaging/
│   │   │       ├── Logging/
│   │   │       └── BillingService.Infrastructure.csproj
│   └── Shared/
│       └── Common/
│           ├── Events/
│           ├── Extensions/
│           └── Common.csproj
├── docker-compose.yml
└── BillingMicroservices.sln

Dependencies
BillingService.API

Serilog.AspNetCore
Serilog.Sinks.Console
Serilog.Sinks.File
RabbitMQ.Client
Microsoft.AspNetCore.OpenApi
Swashbuckle.AspNetCore

BillingService.Application

FluentValidation
MediatR
Microsoft.Extensions.Logging.Abstractions

BillingService.Infrastructure

Microsoft.EntityFrameworkCore
Microsoft.Extensions.Configuration.Abstractions
Microsoft.Extensions.Http
Microsoft.Extensions.Logging.Abstractions
Newtonsoft.Json
RabbitMQ.Client
Serilog

C
