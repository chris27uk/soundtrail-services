#!/usr/bin/env bash
# Join background version-meta await and ensure GITHUB_ENV is set for subsequent steps.
set -euo pipefail

deadline=$((SECONDS + "${VERSION_META_WAIT_TIMEOUT:-180}"))
while (( SECONDS < deadline )); do
  if [[ -f /tmp/version-meta.ready ]]; then
    if [[ -f version-meta/version.env ]]; then
      set -a
      # shellcheck disable=SC1091
      source version-meta/version.env
      set +a
      {
        echo "BUILD_VERSION=${BUILD_VERSION}"
        echo "GITVERSION_INFORMATIONALVERSION=${GITVERSION_INFORMATIONALVERSION}"
        echo "OTEL_SERVICE_VERSION=${OTEL_SERVICE_VERSION}"
      } >> "$GITHUB_ENV"
      echo "Applied BUILD_VERSION=${BUILD_VERSION}"
    fi
    exit 0
  fi
  if [[ -f /tmp/version-meta.failed ]]; then
    echo "::error::version-meta await failed"
    [[ -f /tmp/version-meta.log ]] && cat /tmp/version-meta.log
    exit 1
  fi
  # Not started (local); run synchronously.
  if [[ ! -f /tmp/version-meta.pid && ! -f /tmp/version-meta.done ]]; then
    root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    exec bash "$root/ci-await-version-meta.sh"
  fi
  sleep 1
done

echo "::error::Timed out waiting for background version-meta await"
[[ -f /tmp/version-meta.log ]] && cat /tmp/version-meta.log
exit 1
