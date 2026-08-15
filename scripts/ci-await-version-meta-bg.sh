#!/usr/bin/env bash
# Poll for version-meta in the background so Build can overlap Version with testhost build.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rm -f /tmp/version-meta.ready /tmp/version-meta.failed /tmp/version-meta.done

nohup bash -c "
  set +e
  export SKIP_GITHUB_ENV=1
  bash \"$root/ci-await-version-meta.sh\" >/tmp/version-meta.log 2>&1
  status=\$?
  if [[ \$status -eq 0 ]]; then
    touch /tmp/version-meta.ready
  else
    touch /tmp/version-meta.failed
  fi
  touch /tmp/version-meta.done
  exit \$status
" >/dev/null 2>&1 &
echo $! >/tmp/version-meta.pid
disown || true
echo "Version-meta await started (pid $(cat /tmp/version-meta.pid))"
