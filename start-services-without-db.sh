#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

docker compose up -d --build \
  rabbitmq \
  redis \
  gateway \
  authservice \
  productservice \
  orderservice \
  paymentservice \
  logservice

echo "Started microservice stack without postgres and mongodb."
