#!/usr/bin/env bash
# Prepare CI images for Build + tests:
#   - Sidecars: wait for bg GHA restore (if any), then load/up/health in background
#   - BuildKit: load in the foreground (critical path before setup-buildx)
# Returns once tooling is ready; sidecars overlap buildx/build.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p reports testhost /tmp/sidecar-images
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done /tmp/sidecars.need-reseed

# Sidecar path waits on /tmp/sidecar-cache.done when restore was kicked off.
nohup env WAIT_FOR_SIDECAR_CACHE=1 bash "$root/ci-start-sidecars.sh" >/tmp/sidecars.log 2>&1 &
echo $! >/tmp/sidecars.pid
disown || true

bash "$root/ci-load-tooling-images.sh"
