#!/usr/bin/env bash
# Wait until scripts/ci-start-sidecars.sh has published /tmp/sidecars.ready.
set -euo pipefail

pid_file=/tmp/sidecars.pid
log_file=/tmp/sidecars.log
timeout_seconds="${SIDECAR_WAIT_TIMEOUT:-120}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- sidecar startup log ----"
    cat "$log_file"
    echo "---- end sidecar startup log ----"
  fi
}

while true; do
  if [[ -f /tmp/sidecars.ready ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/sidecars.failed ]]; then
    echo "::error::Sidecar startup failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null; then
      echo "::error::Sidecar startup process exited before becoming ready."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for sidecars after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.5
done
