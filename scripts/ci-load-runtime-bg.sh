#!/usr/bin/env bash
# Load aspnet runtime image in the background (overlaps Buildx/testhost).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p /tmp/sidecar-images
rm -f /tmp/runtime.loaded /tmp/runtime.load-failed /tmp/runtime.load-done

nohup bash -c "
  set +e
  bash \"$root/ci-load-runtime-images.sh\" >/tmp/runtime-load.log 2>&1
  status=\$?
  if [[ \$status -ne 0 ]]; then
    touch /tmp/runtime.load-failed
  fi
  touch /tmp/runtime.load-done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/runtime-load.pid
disown || true
echo "Runtime image load started (pid $(cat /tmp/runtime-load.pid))"
