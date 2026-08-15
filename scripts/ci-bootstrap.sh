#!/usr/bin/env bash
# Prepare CI images for Build + tests:
#   - Sidecars (Redis/OSB/Raven/aspnet): load/up/health in the background
#   - BuildKit tooling: load in the foreground (required before setup-buildx)
# Returns once tooling is ready; sidecars keep warming in parallel with buildx/build.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p reports testhost /tmp/sidecar-images
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done /tmp/sidecars.need-reseed

nohup bash "$root/ci-start-sidecars.sh" >/tmp/sidecars.log 2>&1 &
echo $! > /tmp/sidecars.pid
disown || true

bash "$root/ci-load-tooling-images.sh"
