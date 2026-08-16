#!/usr/bin/env bash
# Publish apps in the background so tests can run in the foreground.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rm -f /tmp/publish.ready /tmp/publish.failed /tmp/publish.done

nohup bash -c "
  set +e
  bash \"$root/ci-publish-apps.sh\" >/tmp/publish.log 2>&1
  status=\$?
  if [[ \$status -eq 0 ]]; then
    touch /tmp/publish.ready
  else
    touch /tmp/publish.failed
  fi
  touch /tmp/publish.done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/publish.pid
disown || true
echo "Publish started (pid $(cat /tmp/publish.pid))"
