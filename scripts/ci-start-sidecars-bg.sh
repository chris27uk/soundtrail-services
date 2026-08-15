#!/usr/bin/env bash
# Compose up Redis/OSB/Raven in the background (images must already be loaded).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p reports testhost /tmp/sidecar-images
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done

nohup bash "$root/ci-up-sidecars.sh" >/tmp/sidecars.log 2>&1 &
echo $! >/tmp/sidecars.pid
disown || true
echo "Sidecar startup started (pid $(cat /tmp/sidecars.pid))"
