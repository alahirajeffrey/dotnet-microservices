# .NET Microservices Project

This repository contains a small ASP.NET Core microservices sample built with .NET 10. The solution is organized into three independent services that can be developed, launched, and tested separately while sharing the same infrastructure setup.

## Project overview

The solution currently contains the following services:

- AuthService
  - Handles authentication and authorization of users

- ProductService
  - Handle product-related domain logic

- PaymentService
  - Handle payment/business processing concerns

## Dependencies

### Infrastructure dependencies

The Docker Compose file provisions the following backing services:

- PostgreSQL 16
- Redis 7
- RabbitMQ 3 with management UI

These are exposed on the following local ports:

- PostgreSQL: http://localhost:5432
- Redis: http://localhost:6379
- RabbitMQ AMQP: http://localhost:5672
- RabbitMQ management UI: http://localhost:15672

## How to start the project

### 1. Restore dependencies

```bash
dotnet restore
```

### 2. Start infrastructure services

```bash
docker compose up -d
```

This starts PostgreSQL, Redis, and RabbitMQ in the background.

### 3. Run the services

Each service can be started independently from the project root:

```bash
dotnet run --project AuthService/AuthService.csproj
```

```bash
dotnet run --project ProductService/ProductService.csproj
```

```bash
dotnet run --project PaymentService/PaymentService.csproj
```

### Local service URLs

The launch profiles are configured as follows:

- AuthService: http://localhost:5284
- ProductService: http://localhost:5103
- PaymentService: http://localhost:5117

The HTTPS endpoints are also configured in each service's `launchSettings.json` file.

## OpenAPI and sample endpoints

OpenAPI documents are available in development mode via the standard ASP.NET Core OpenAPI route.

## How to test the project

### Build the solution

```bash
dotnet build microservices.slnx
```

### Run automated tests

```bash
dotnet test microservices.slnx
```
