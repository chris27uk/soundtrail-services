#!/usr/bin/env bash
# Wait until aspnet runtime image is loaded.
set -euo pipefail

pid_file=/tmp/runtime-load.pid
log_file=/tmp/runtime-load.log
timeout_seconds="${RUNTIME_LOAD_WAIT_TIMEOUT:-180}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- runtime load log ----"
    cat "$log_file"
    echo "---- end runtime load log ----"
  fi
}

if [[ ! -f "$pid_file" && ! -f /tmp/runtime.load-done && ! -f /tmp/runtime.loaded ]]; then
  echo "No background runtime load; running synchronously."
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  bash "$root/ci-load-runtime-images.sh"
  exit 0
fi

while true; do
  if [[ -f /tmp/runtime.loaded ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/runtime.load-failed ]]; then
    echo "::error::Runtime image load failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null; then
      if [[ -f /tmp/runtime.loaded ]]; then
        dump_log
        exit 0
      fi
      echo "::error::Runtime load process exited before image was ready."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for runtime image load after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.5
done
