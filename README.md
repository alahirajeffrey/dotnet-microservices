# .NET Microservices Project

This repository contains a .NET 10 microservices solution for authentication, products, orders, payments, event logging, and API routing. The stack is built around ASP.NET Core services, PostgreSQL, Redis, RabbitMQ, MongoDB, and a YARP-based API gateway.

## Services in the solution

- AuthService
  - User authentication, registration, login, and JWT generation

- ProductService
  - Product CRUD, pagination, Redis cache-aside strategy, and stock updates

- OrderService
  - Order creation, retrieval, pagination, and status updates

- PaymentService
  - Paystack payment initialization, webhook verification, and order/payment state updates

- LogService
  - RabbitMQ consumer that stores log events in MongoDB

- Gateway
  - YARP reverse proxy that routes incoming requests to the appropriate backend service

## Infrastructure

The Docker Compose setup starts the following backing services:

- PostgreSQL 16
- Redis 7
- RabbitMQ 3 with management UI
- MongoDB 7
- OpenTelemetry Collector for receiving and forwarding traces
- Grafana Tempo for trace storage and querying
- Grafana for exploring traces

The main local ports are:

- PostgreSQL: localhost:5432
- Redis: localhost:6379
- RabbitMQ AMQP: localhost:5672
- RabbitMQ UI: localhost:15672
- MongoDB: mongodb://localhost:27017
- Gateway: http://localhost:5000
- AuthService: http://localhost:5284
- ProductService: http://localhost:5103
- OrderService: http://localhost:5150
- PaymentService: http://localhost:5117
- LogService: http://localhost:5200
- Grafana: http://localhost:${GRAFANA_PORT}
- Tempo query API: http://localhost:${TEMPO_PORT}
- OpenTelemetry Collector OTLP/gRPC: localhost:${OTEL_COLLECTOR_GRPC_PORT}
- OpenTelemetry Collector OTLP/HTTP: localhost:${OTEL_COLLECTOR_HTTP_PORT}

## View traces in Grafana

The application services export traces to the OpenTelemetry Collector. The Collector accepts OTLP over gRPC and HTTP, then forwards traces to Tempo. Grafana is provisioned with Tempo as its default datasource and connects to it at `http://tempo:3200` over the Compose network.

The traces include ASP.NET Core requests, outgoing HTTP requests, PostgreSQL operations from EF Core, and LogService MongoDB operations. Tempo stores trace data in the `tempo-data` Docker volume and is configured to retain blocks for one hour.

To view traces:

1. Start the stack from the repository root with `docker compose up -d --build`.
2. Send requests to the gateway or services to generate activity. Traces appear only after requests have run.
3. Open `http://localhost:${GRAFANA_PORT}` and sign in with the `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD` values from your `.env` file.
4. Open **Explore**, select the **Tempo** datasource, and use the **Search** view to select a service and run the trace search.
5. Select a result to inspect its spans, timing, and service-to-service calls. Database spans appear under the request trace that caused the database operation.

Inside Compose, application exporters use `http://otel-collector:4317` by default. `OTEL_EXPORTER_OTLP_ENDPOINT` can override this when using a different collector. The host-side Grafana and Collector ports are controlled by `GRAFANA_PORT`, `OTEL_COLLECTOR_GRPC_PORT`, and `OTEL_COLLECTOR_HTTP_PORT` in `.env`.

## Start the full stack

From the project root, run:

```bash
docker compose up -d --build
```

This starts the databases, messaging infrastructure, gateway, and all application services.

To check the running containers:

```bash
docker compose ps
```

To stop the stack:

```bash
docker compose down
```

To remove volumes too:

```bash
docker compose down -v
```

## Start the stack without PostgreSQL and MongoDB

A helper script is included for starting the lightweight runtime without the database-heavy services:

```bash
chmod +x start-services-without-db.sh
./start-services-without-db.sh
```

This script starts the services that do not require PostgreSQL and MongoDB, including:

- Redis
- RabbitMQ
- Gateway
- AuthService
- ProductService
- OrderService
- PaymentService
- LogService

## Gateway usage

All external requests can be routed through the gateway on port 5000. The gateway is configured with YARP routes that forward traffic to the appropriate microservices.

Example:

```bash
http://localhost:5000/auth/...
http://localhost:5000/products/...
http://localhost:5000/orders/...
http://localhost:5000/payments/...
```

## Useful Docker commands

Build the images:

```bash
docker compose build
```

View logs for a specific service:

```bash
docker compose logs -f productservice
```

Rebuild and restart a single service:

```bash
docker compose up -d --build productservice
```

## Build and test

Build the solution:

```bash
dotnet build microservices.slnx
```

Run the tests:

```bash
dotnet test microservices.slnx
```

## Notes

- Product caching uses Redis with a one-hour expiry for selected product reads.
- Log events are published to RabbitMQ and consumed by LogService for persistence in MongoDB.
- Payment webhooks are verified using the Paystack signature before processing.
- The API gateway centralizes external routing and reduces direct backend exposure.

## Todo

- add git action that runs sast and dast when a push is made
- write terraform to deploy backend to aws (use localstack to test)
- write git action to deploy code
- Use image numbers for docker images
