#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [ -f .env ]; then
  set -a
  source .env
  set +a
fi

POSTGRES_PORT="${POSTGRES_PORT:-5432}"
REDIS_PORT="${REDIS_PORT:-6379}"
RABBITMQ_PORT="${RABBITMQ_PORT:-5672}"
MONGODB_PORT="${MONGODB_PORT:-27017}"

AUTH_SERVICE_PORT="${AUTH_SERVICE_PORT:-5284}"
PRODUCT_SERVICE_PORT="${PRODUCT_SERVICE_PORT:-5103}"
ORDER_SERVICE_PORT="${ORDER_SERVICE_PORT:-5150}"
PAYMENT_SERVICE_PORT="${PAYMENT_SERVICE_PORT:-5117}"
LOG_SERVICE_PORT="${LOG_SERVICE_PORT:-5200}"
GATEWAY_PORT="${GATEWAY_PORT:-5000}"

LOG_DIR="$PWD/.local-logs"
mkdir -p "$LOG_DIR"

echo "Starting infrastructure services with Docker Compose..."
docker compose up -d postgres redis rabbitmq mongodb

echo "Starting AuthService locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT};Database=${AUTH_DB_NAME:-authdb};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  RabbitMQ__HostName="localhost" \
  RabbitMQ__UserName="${RABBITMQ_DEFAULT_USER}" \
  RabbitMQ__Password="${RABBITMQ_DEFAULT_PASS}" \
  Jwt__Key="${JWT_KEY:-O9tFWSYaGPz9jf0yA3FRH5b4BU4j+6m1O8koAqY/hNIguKUfOaw1z+EJvydAXeR7E+QHy2Mfz0UVkWK0lFB+ew==}" \
  Jwt__Issuer="${JWT_ISSUER:-AuthService}" \
  Jwt__Audience="${JWT_AUDIENCE:-Dotnet-Microservices}" \
  dotnet run --project AuthService/AuthService.csproj --urls "http://localhost:${AUTH_SERVICE_PORT}" > "$LOG_DIR/authservice.log" 2>&1 &

sleep 2

echo "Starting ProductService locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT};Database=${PRODUCT_DB_NAME:-productdb};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  Redis__ConnectionString="localhost:${REDIS_PORT}" \
  RabbitMQ__HostName="localhost" \
  RabbitMQ__UserName="${RABBITMQ_DEFAULT_USER}" \
  RabbitMQ__Password="${RABBITMQ_DEFAULT_PASS}" \
  InternalApi__Secret="${INTERNAL_API_SECRET:-internal-secret}" \
  Jwt__Key="${JWT_KEY:-O9tFWSYaGPz9jf0yA3FRH5b4BU4j+6m1O8koAqY/hNIguKUfOaw1z+EJvydAXeR7E+QHy2Mfz0UVkWK0lFB+ew==}" \
  Jwt__Issuer="${JWT_ISSUER:-AuthService}" \
  Jwt__Audience="${JWT_AUDIENCE:-Dotnet-Microservices}" \
  dotnet run --project ProductService/ProductService.csproj --urls "http://localhost:${PRODUCT_SERVICE_PORT}" > "$LOG_DIR/productservice.log" 2>&1 &

sleep 2

echo "Starting OrderService locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT};Database=${ORDER_DB_NAME:-orderdb};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  ProductService__BaseUrl="http://localhost:${PRODUCT_SERVICE_PORT}" \
  RabbitMQ__HostName="localhost" \
  RabbitMQ__UserName="${RABBITMQ_DEFAULT_USER}" \
  RabbitMQ__Password="${RABBITMQ_DEFAULT_PASS}" \
  InternalApi__Secret="${INTERNAL_API_SECRET:-internal-secret}" \
  Jwt__Key="${JWT_KEY:-O9tFWSYaGPz9jf0yA3FRH5b4BU4j+6m1O8koAqY/hNIguKUfOaw1z+EJvydAXeR7E+QHy2Mfz0UVkWK0lFB+ew==}" \
  Jwt__Issuer="${JWT_ISSUER:-AuthService}" \
  Jwt__Audience="${JWT_AUDIENCE:-Dotnet-Microservices}" \
  dotnet run --project OrderService/OrderService.csproj --urls "http://localhost:${ORDER_SERVICE_PORT}" > "$LOG_DIR/orderservice.log" 2>&1 &

sleep 2

echo "Starting PaymentService locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT};Database=${PAYMENT_DB_NAME:-paymentdb};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  OrderService__BaseUrl="http://localhost:${ORDER_SERVICE_PORT}" \
  RabbitMQ__HostName="localhost" \
  RabbitMQ__UserName="${RABBITMQ_DEFAULT_USER}" \
  RabbitMQ__Password="${RABBITMQ_DEFAULT_PASS}" \
  Paystack__SecretKey="${PAYSTACK_SECRET_KEY:-your_paystack_secret_key}" \
  Paystack__CallbackUrl="http://localhost:${PAYMENT_SERVICE_PORT}/api/webhook/paystack" \
  InternalApi__Secret="${INTERNAL_API_SECRET:-internal-secret}" \
  Jwt__Key="${JWT_KEY:-O9tFWSYaGPz9jf0yA3FRH5b4BU4j+6m1O8koAqY/hNIguKUfOaw1z+EJvydAXeR7E+QHy2Mfz0UVkWK0lFB+ew==}" \
  Jwt__Issuer="${JWT_ISSUER:-AuthService}" \
  Jwt__Audience="${JWT_AUDIENCE:-Dotnet-Microservices}" \
  dotnet run --project PaymentService/PaymentService.csproj --urls "http://localhost:${PAYMENT_SERVICE_PORT}" > "$LOG_DIR/paymentservice.log" 2>&1 &

sleep 2

echo "Starting LogService locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  MongoDb__Host="localhost" \
  MongoDb__Port="${MONGODB_PORT}" \
  MongoDb__Username="${MONGODB_USERNAME}" \
  MongoDb__Password="${MONGODB_ROOT_PASSWORD}" \
  MongoDb__DatabaseName="${MONGODB_DATABASE:-logdb}" \
  RabbitMQ__HostName="localhost" \
  RabbitMQ__UserName="${RABBITMQ_DEFAULT_USER}" \
  RabbitMQ__Password="${RABBITMQ_DEFAULT_PASS}" \
  dotnet run --project LogService/LogService.csproj --urls "http://localhost:${LOG_SERVICE_PORT}" > "$LOG_DIR/logservice.log" 2>&1 &

sleep 2

echo "Starting Gateway locally..."
nohup env \
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}" \
  ReverseProxy__Clusters__authCluster__Destinations__auth__Address="http://localhost:${AUTH_SERVICE_PORT}/" \
  ReverseProxy__Clusters__productCluster__Destinations__product__Address="http://localhost:${PRODUCT_SERVICE_PORT}/" \
  ReverseProxy__Clusters__orderCluster__Destinations__order__Address="http://localhost:${ORDER_SERVICE_PORT}/" \
  ReverseProxy__Clusters__paymentCluster__Destinations__payment__Address="http://localhost:${PAYMENT_SERVICE_PORT}/" \
  ReverseProxy__Clusters__logCluster__Destinations__log__Address="http://localhost:${LOG_SERVICE_PORT}/" \
  dotnet run --project Gateway/Gateway.csproj --urls "http://localhost:${GATEWAY_PORT}" > "$LOG_DIR/gateway.log" 2>&1 &

echo "Infrastructure and app services are starting in the background."
echo "Logs are stored in: $LOG_DIR"
echo "Service URLs:"
echo "  Gateway: http://localhost:${GATEWAY_PORT}"
echo "  AuthService: http://localhost:${AUTH_SERVICE_PORT}"
echo "  ProductService: http://localhost:${PRODUCT_SERVICE_PORT}"
echo "  OrderService: http://localhost:${ORDER_SERVICE_PORT}"
echo "  PaymentService: http://localhost:${PAYMENT_SERVICE_PORT}"
echo "  LogService: http://localhost:${LOG_SERVICE_PORT}"
