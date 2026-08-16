#!/usr/bin/env bash
# Wait for scripts/ci-run-tests-bg.sh and stream its log live.
set -euo pipefail

pid_file=/tmp/tests.pid
log_file=/tmp/tests.log
timeout_seconds="${TEST_WAIT_TIMEOUT:-240}"
deadline=$((SECONDS + timeout_seconds))
tail_pid=""

stop_tail() {
  if [[ -n "$tail_pid" ]]; then
    kill "$tail_pid" 2>/dev/null || true
    wait "$tail_pid" 2>/dev/null || true
    tail_pid=""
  fi
}
trap stop_tail EXIT

if [[ ! -f "$pid_file" && ! -f /tmp/tests.done ]]; then
  echo "No background tests; running synchronously."
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  bash "$root/ci-run-tests.sh"
  exit 0
fi

touch "$log_file"
echo "---- test log ----"
tail -n +1 -F "$log_file" &
tail_pid=$!

while true; do
  if [[ -f /tmp/tests.ready ]]; then
    sleep 0.2
    stop_tail
    echo "---- end test log ----"
    exit 0
  fi

  if [[ -f /tmp/tests.failed ]]; then
    sleep 0.2
    stop_tail
    echo "---- end test log ----"
    echo "::error::Tests failed."
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null && [[ ! -f /tmp/tests.done ]]; then
      sleep 0.2
      stop_tail
      echo "---- end test log ----"
      echo "::error::Test process exited before signaling done."
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    stop_tail
    echo "---- end test log ----"
    echo "::error::Timed out waiting for tests after ${timeout_seconds}s."
    exit 1
  fi

  sleep 0.5
done
