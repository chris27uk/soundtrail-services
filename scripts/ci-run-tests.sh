#!/usr/bin/env bash
# Wait for sidecars (started by ci-bootstrap.sh), then run the published testhost.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-wait-sidecars.sh"
docker compose -f docker-compose.ci.yml run --rm --no-deps testhost
