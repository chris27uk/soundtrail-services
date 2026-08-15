#!/usr/bin/env bash
# Restore sidecars.tar from GHA cache in the background (overlaps buildx/build).
# Uses the already-downloaded actions/cache restore entrypoint with a private
# GITHUB_OUTPUT so we do not race the job's real output file.
set -euo pipefail

outdir=/tmp/sidecar-cache
mkdir -p /tmp/sidecar-images "$outdir"
rm -f /tmp/sidecar-cache.done /tmp/sidecar-cache.hit
: >"$outdir/github_output"

if [[ -z "${SIDECAR_CACHE_KEY:-}" ]]; then
  echo "SIDECAR_CACHE_KEY not set; skipping sidecar cache restore"
  echo "false" >/tmp/sidecar-cache.hit
  touch /tmp/sidecar-cache.done
  exit 0
fi

# actions/cache@v4 restore action entrypoint (bundled; avoid npm install on critical path).
restore_js="$(find /home/runner/work/_actions/actions/cache -path '*/dist/restore-only/index.js' 2>/dev/null | sort | tail -1 || true)"
if [[ -z "$restore_js" ]]; then
  # Older layouts used dist/restore/index.js
  restore_js="$(find /home/runner/work/_actions/actions/cache -path '*/dist/restore/index.js' 2>/dev/null | sort | tail -1 || true)"
fi
if [[ -z "$restore_js" ]]; then
  echo "actions/cache restore entrypoint not found; sidecars will pull if needed"
  echo "false" >/tmp/sidecar-cache.hit
  touch /tmp/sidecar-cache.done
  exit 0
fi

(
  set +e
  export INPUT_PATH="/tmp/sidecar-images/sidecars.tar"
  export INPUT_KEY="$SIDECAR_CACHE_KEY"
  export INPUT_RESTORE-KEYS="${SIDECAR_CACHE_RESTORE_KEYS:-}"
  export INPUT_ENABLECROSSOSARCHIVE=false
  export INPUT_FAIL-ON-CACHE-MISS=false
  export INPUT_LOOKUP-ONLY=false
  export GITHUB_OUTPUT="$outdir/github_output"
  echo "Restoring sidecar cache key=$SIDECAR_CACHE_KEY"
  node "$restore_js"
  status=$?
  hit=false
  if grep -q '^cache-hit=true' "$outdir/github_output" 2>/dev/null; then
    hit=true
  fi
  echo "$hit" >/tmp/sidecar-cache.hit
  if [[ "$status" -ne 0 ]]; then
    echo "Sidecar cache restore exited $status (hit=$hit)"
  else
    echo "Sidecar cache restore finished (exact-hit=$hit)"
  fi
  touch /tmp/sidecar-cache.done
) >/tmp/sidecar-cache.log 2>&1 &
echo $! >/tmp/sidecar-cache.pid
echo "Sidecar cache restore pid=$(cat /tmp/sidecar-cache.pid)"
