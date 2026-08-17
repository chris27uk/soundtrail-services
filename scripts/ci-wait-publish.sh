#!/usr/bin/env bash
# Wait for scripts/ci-publish-apps-bg.sh.
set -euo pipefail

pid_file=/tmp/publish.pid
log_file=/tmp/publish.log
timeout_seconds="${PUBLISH_WAIT_TIMEOUT:-240}"
deadline=$((SECONDS + timeout_seconds))

dump_log() {
  if [[ -f "$log_file" ]]; then
    echo "---- publish log ----"
    cat "$log_file"
    echo "---- end publish log ----"
  fi
}

if [[ ! -f "$pid_file" && ! -f /tmp/publish.done ]]; then
  echo "::error::Publish was not started."
  exit 1
fi

while true; do
  if [[ -f /tmp/publish.ready ]]; then
    dump_log
    exit 0
  fi

  if [[ -f /tmp/publish.failed ]]; then
    echo "::error::Docker build failed."
    dump_log
    exit 1
  fi

  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if [[ -n "$pid" ]] && ! kill -0 "$pid" 2>/dev/null && [[ ! -f /tmp/publish.done ]]; then
      echo "::error::Publish process exited before signaling done."
      dump_log
      exit 1
    fi
  fi

  if (( SECONDS >= deadline )); then
    echo "::error::Timed out waiting for publish after ${timeout_seconds}s."
    dump_log
    exit 1
  fi

  sleep 0.5
done
