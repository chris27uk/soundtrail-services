#!/usr/bin/env bash
# Wait for scripts/ci-run-tests-bg.sh and replay its log onto this step.
set -euo pipefail

pid_file=/tmp/tests.pid
log_file=/tmp/tests.log
timeout_seconds="${TEST_WAIT_TIMEOUT:-240}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- test log ----"
    cat "$log_file"
    echo "---- end test log ----"
  fi
}

if [[ ! -f "$pid_file" && ! -f /tmp/tests.done ]]; then
  echo "No background tests; running synchronously."
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  bash "$root/ci-run-tests.sh"
  exit 0
fi

while true; do
  if [[ -f /tmp/tests.ready ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/tests.failed ]]; then
    echo "::error::Tests failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null && [[ ! -f /tmp/tests.done ]]; then
      echo "::error::Test process exited before signaling done."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for tests after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.5
done
