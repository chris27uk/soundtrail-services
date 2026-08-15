#!/usr/bin/env bash
# Wait until scripts/ci-load-tooling-bg.sh has finished loading BuildKit.
set -euo pipefail

pid_file=/tmp/tooling.pid
log_file=/tmp/tooling.log
timeout_seconds="${TOOLING_WAIT_TIMEOUT:-180}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- tooling load log ----"
    cat "$log_file"
    echo "---- end tooling load log ----"
  fi
}

# If never started (local/dev), load synchronously.
if [[ ! -f "$pid_file" && ! -f /tmp/tooling.done ]]; then
  echo "No background tooling load; running synchronously."
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  bash "$root/ci-load-tooling-images.sh"
  touch /tmp/tooling.ready /tmp/tooling.done
  exit 0
fi

while true; do
  if [[ -f /tmp/tooling.ready ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/tooling.failed ]]; then
    echo "::error::Tooling image load failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null && [[ ! -f /tmp/tooling.done ]]; then
      echo "::error::Tooling load process exited before signaling done."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for tooling load after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.25
done
