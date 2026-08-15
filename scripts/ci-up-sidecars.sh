#!/usr/bin/env bash
# Bring Redis/OSB/Raven up (images must already be loaded).
# Signals /tmp/sidecars.ready on success.
set -euo pipefail

compose=(docker compose -f docker-compose.ci.yml)
mkdir -p reports
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done

cleanup_on_error() {
  touch /tmp/sidecars.failed
  touch /tmp/sidecars.done
}
trap cleanup_on_error ERR

echo "Starting sidecars"
# Raven healthcheck is /usr/lib/ravendb/scripts/healthcheck.sh.
"${compose[@]}" up -d --wait --wait-timeout 120 redis openservicebus ravendb

# OpenServiceBus has no image healthcheck; confirm management HTTP before testhost starts.
curl -fsS --retry 20 --retry-delay 1 --retry-all-errors http://127.0.0.1:5300/health >/dev/null
echo "Dependencies ready."
"${compose[@]}" ps
touch /tmp/sidecars.ready
touch /tmp/sidecars.done
