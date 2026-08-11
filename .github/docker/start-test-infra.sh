#!/usr/bin/env bash
# Start Redis + Service Bus emulator for CI tests.
# Readiness: Redis docker health + emulator HTTP /health (emulator is distroless — no in-container healthcheck).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT}/.github/docker/docker-compose.test-infra.yml"
CONFIG_PATH="${ROOT}/Soundtrail.Services.AppHost/servicebus-emulator/Config.json"
NETWORK_NAME="soundtrail-test"

if [[ ! -f "$CONFIG_PATH" ]]; then
  echo "::error::Service Bus emulator Config.json not found at $CONFIG_PATH"
  exit 1
fi

export SOUNDTRAIL_SERVICEBUS_CONFIG="$CONFIG_PATH"

echo "Starting test infra (Redis + Service Bus emulator)..."
docker compose -f "$COMPOSE_FILE" up -d --pull missing

echo "Waiting for Redis..."
for _ in $(seq 1 60); do
  if docker compose -f "$COMPOSE_FILE" exec -T redis redis-cli ping 2>/dev/null | grep -q PONG; then
    echo "Redis is ready."
    break
  fi
  sleep 1
done

if ! docker compose -f "$COMPOSE_FILE" exec -T redis redis-cli ping 2>/dev/null | grep -q PONG; then
  echo "::error::Redis did not become ready"
  docker compose -f "$COMPOSE_FILE" logs --no-color redis || true
  exit 1
fi

echo "Waiting for Service Bus emulator /health on :5300..."
ready=0
for _ in $(seq 1 60); do
  # Emulator reports healthy only after SQL setup + entity sync.
  if curl -fsS "http://127.0.0.1:5300/health" 2>/dev/null | grep -qi '"status"[[:space:]]*:[[:space:]]*"healthy"'; then
    echo "Service Bus emulator is healthy."
    ready=1
    break
  fi
  sleep 2
done

if [[ "$ready" -ne 1 ]]; then
  echo "::error::Service Bus emulator did not become healthy on http://127.0.0.1:5300/health"
  docker compose -f "$COMPOSE_FILE" ps || true
  docker compose -f "$COMPOSE_FILE" logs --no-color || true
  exit 1
fi

echo "Test infra is up."
docker compose -f "$COMPOSE_FILE" ps
echo "NETWORK_NAME=${NETWORK_NAME}"
