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

## Environment configuration

Create a `.env` file in the project root with the required settings before starting Docker Compose.

Example:

```bash
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
JWT_KEY=your_jwt_key_here
JWT_ISSUER=AuthService
JWT_AUDIENCE=Dotnet-Microservices
INTERNAL_API_SECRET=internal-secret
PAYSTACK_SECRET_KEY=your_paystack_secret_key
```

These values are used by the Docker service definitions in [compose.yml](compose.yml).

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
