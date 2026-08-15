#!/usr/bin/env bash
# Load sidecar images in the background (no compose up).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p /tmp/sidecar-images reports
rm -f /tmp/sidecars.loaded /tmp/sidecars.load-failed /tmp/sidecars.load-done

nohup bash -c "
  set +e
  bash \"$root/ci-load-sidecar-images.sh\" >/tmp/sidecars-load.log 2>&1
  status=\$?
  if [[ \$status -ne 0 ]]; then
    touch /tmp/sidecars.load-failed
  fi
  touch /tmp/sidecars.load-done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/sidecars-load.pid
disown || true
echo "Sidecar image load started (pid $(cat /tmp/sidecars-load.pid))"
