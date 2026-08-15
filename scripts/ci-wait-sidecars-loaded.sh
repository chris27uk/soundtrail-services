#!/usr/bin/env bash
# Wait until ci-load-sidecar-images.sh has finished (success or failure).
set -euo pipefail

pid_file=/tmp/sidecars-load.pid
log_file=/tmp/sidecars-load.log
timeout_seconds="${SIDECAR_LOAD_WAIT_TIMEOUT:-180}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- sidecar load log ----"
    cat "$log_file"
    echo "---- end sidecar load log ----"
  fi
}

# If never started (local/dev), load synchronously.
if [[ ! -f "$pid_file" && ! -f /tmp/sidecars.load-done && ! -f /tmp/sidecars.loaded ]]; then
  echo "No background sidecar load; running synchronously."
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  bash "$root/ci-load-sidecar-images.sh"
  exit 0
fi

while true; do
  if [[ -f /tmp/sidecars.loaded ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/sidecars.load-failed ]]; then
    echo "::error::Sidecar image load failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null; then
      if [[ -f /tmp/sidecars.loaded ]]; then
        dump_log
        exit 0
      fi
      echo "::error::Sidecar load process exited before images were ready."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for sidecar image load after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.5
done
