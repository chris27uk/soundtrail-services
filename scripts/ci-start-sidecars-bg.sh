#!/usr/bin/env bash
# Start Redis/OSB/Raven (+ aspnet) load/up in the background.
# Call only AFTER tooling docker load has finished (avoids Docker daemon lock contention).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p reports testhost /tmp/sidecar-images
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done /tmp/sidecars.need-reseed

nohup bash "$root/ci-start-sidecars.sh" >/tmp/sidecars.log 2>&1 &
echo $! >/tmp/sidecars.pid
disown || true
echo "Sidecar startup started (pid $(cat /tmp/sidecars.pid))"
