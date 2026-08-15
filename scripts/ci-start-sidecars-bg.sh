#!/usr/bin/env bash
# Load sidecar images then compose up (background). Aspnet is runtime.tar.
# Overlaps Buildx/testhost so tests rarely wait on health.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p reports testhost /tmp/sidecar-images
rm -f /tmp/sidecars.ready /tmp/sidecars.failed /tmp/sidecars.done
rm -f /tmp/sidecars.loaded /tmp/sidecars.load-failed

nohup bash -c "
  set +e
  {
    bash \"$root/ci-load-sidecar-images.sh\"
    status=\$?
    if [[ \$status -ne 0 ]]; then
      touch /tmp/sidecars.failed
      touch /tmp/sidecars.done
      exit \$status
    fi
    bash \"$root/ci-up-sidecars.sh\"
    exit \$?
  } >/tmp/sidecars.log 2>&1
" >/dev/null 2>&1 &
echo $! >/tmp/sidecars.pid
disown || true
echo "Sidecar load+up started (pid $(cat /tmp/sidecars.pid))"
