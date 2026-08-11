#!/usr/bin/env bash
# Start Redis + Service Bus emulator on a shared Docker network for CI tests.
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

echo "Starting test infra (Redis + Service Bus emulator) on network '${NETWORK_NAME}'..."
docker compose -f "$COMPOSE_FILE" up -d --pull missing

echo "Waiting for Redis healthy..."
for _ in $(seq 1 60); do
  status="$(docker inspect --format='{{.State.Health.Status}}' "$(docker compose -f "$COMPOSE_FILE" ps -q redis)" 2>/dev/null || true)"
  if [[ "$status" == "healthy" ]]; then
    break
  fi
  sleep 1
done

echo "Waiting for Service Bus emulator healthy..."
for _ in $(seq 1 90); do
  status="$(docker inspect --format='{{.State.Health.Status}}' "$(docker compose -f "$COMPOSE_FILE" ps -q servicebus-emulator)" 2>/dev/null || true)"
  if [[ "$status" == "healthy" ]]; then
    echo "Service Bus emulator is healthy."
    break
  fi
  sleep 2
done

status="$(docker inspect --format='{{.State.Health.Status}}' "$(docker compose -f "$COMPOSE_FILE" ps -q servicebus-emulator)" 2>/dev/null || true)"
if [[ "$status" != "healthy" ]]; then
  echo "::error::Service Bus emulator did not become healthy (status=${status:-unknown})"
  docker compose -f "$COMPOSE_FILE" logs --no-color || true
  exit 1
fi

# Extra settle time — entities finish initializing after the AMQP port opens.
sleep 5

echo "Test infra is up."
docker compose -f "$COMPOSE_FILE" ps
echo "NETWORK_NAME=${NETWORK_NAME}"
