#!/usr/bin/env bash
# Load BuildKit tooling image in the background (critical path).
# Overlaps sidecar GHA cache restore; must finish before setup-buildx.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p /tmp/sidecar-images
rm -f /tmp/tooling.ready /tmp/tooling.failed /tmp/tooling.done

nohup bash -c "
  set +e
  bash \"$root/ci-load-tooling-images.sh\" >/tmp/tooling.log 2>&1
  status=\$?
  if [[ \$status -eq 0 ]]; then
    touch /tmp/tooling.ready
  else
    touch /tmp/tooling.failed
  fi
  touch /tmp/tooling.done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/tooling.pid
disown || true
echo "Tooling docker load started (pid $(cat /tmp/tooling.pid))"
