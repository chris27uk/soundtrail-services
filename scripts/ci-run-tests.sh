#!/usr/bin/env bash
# Wait for sidecars + aspnet runtime, then run the published testhost.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-wait-sidecars.sh"
bash "$root/ci-wait-runtime.sh"
docker compose -f docker-compose.ci.yml run --rm --no-deps testhost
