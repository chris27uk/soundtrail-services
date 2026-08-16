#!/usr/bin/env bash
# Run the published testhost in the background so main can publish apps in parallel.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rm -f /tmp/tests.ready /tmp/tests.failed /tmp/tests.done

nohup bash -c "
  set +e
  bash \"$root/ci-run-tests.sh\" >/tmp/tests.log 2>&1
  status=\$?
  if [[ \$status -eq 0 ]]; then
    touch /tmp/tests.ready
  else
    touch /tmp/tests.failed
  fi
  touch /tmp/tests.done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/tests.pid
disown || true
echo "Tests started (pid $(cat /tmp/tests.pid))"
