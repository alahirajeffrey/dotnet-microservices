#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [[ -f .env ]]; then
	set -a
	source .env
	set +a
fi

LOG_DIR="$PWD/.local-logs"
services=(frontend gateway logservice paymentservice orderservice productservice authservice)
all_pids=()

pid_matches_service() {
	local service="$1"
	local pid="$2"
	local command_line
	local expected

	case "$service" in
		frontend) expected="npm run dev" ;;
		gateway) expected="Gateway/Gateway.csproj" ;;
		authservice) expected="AuthService/AuthService.csproj" ;;
		productservice) expected="ProductService/ProductService.csproj" ;;
		orderservice) expected="OrderService/OrderService.csproj" ;;
		paymentservice) expected="PaymentService/PaymentService.csproj" ;;
		logservice) expected="LogService/LogService.csproj" ;;
	esac

	command_line=$(ps -p "$pid" -o args= 2>/dev/null || true)
	[[ "$command_line" == *"$expected"* ]]
}

process_running() {
	local state
	state=$(ps -o stat= -p "$1" 2>/dev/null || true)
	[[ -n "$state" && "$state" != Z* ]]
}

collect_process_tree() {
	local pid="$1"
	local child

	[[ -d "/proc/$pid" ]] || return 0
	all_pids+=("$pid")

	while read -r child; do
		[[ -n "$child" ]] && collect_process_tree "$child"
	done < <(ps -o pid= --ppid "$pid" 2>/dev/null || true)
}

echo "Stopping locally launched application services..."
for service in "${services[@]}"; do
	pid_file="$LOG_DIR/$service.pid"
	if [[ -f "$pid_file" ]]; then
		read -r pid < "$pid_file" || true
		if [[ "${pid:-}" =~ ^[0-9]+$ ]] && process_running "$pid" && pid_matches_service "$service" "$pid"; then
			collect_process_tree "$pid"
		fi
		rm -f "$pid_file"
	fi
done

if ((${#all_pids[@]})); then
	for pid in "${all_pids[@]}"; do
		kill -TERM "$pid" 2>/dev/null || true
	done

	for ((attempt = 0; attempt < 10; attempt++)); do
		remaining=0
		for pid in "${all_pids[@]}"; do
			if process_running "$pid"; then
				remaining=1
			fi
		done
		((remaining == 0)) && break
		sleep 1
	done

	for pid in "${all_pids[@]}"; do
		if process_running "$pid"; then
			kill -KILL "$pid" 2>/dev/null || true
		fi
	done
fi

echo "Stopping local Docker Compose infrastructure..."
docker compose stop --timeout 10 postgres redis rabbitmq mongodb otel-collector tempo grafana localstack

echo "Local application services and infrastructure have been stopped."
