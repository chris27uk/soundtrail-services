#!/usr/bin/env bash
# After testhost GHA restore: reuse published bits or rebuild.
# need_buildx is true on miss, or on main (publish-apps still uses Buildx).
set -euo pipefail

cache_hit="${TESTHOST_CACHE_HIT:-false}"
hit=false
if [[ "$cache_hit" == "true" && -f testhost/Soundtrail.Services.Tests.dll ]]; then
  hit=true
  echo "Reusing cached testhost output."
else
  echo "Testhost cache miss; will build."
  rm -rf testhost
fi

need_buildx=true
if [[ "$hit" == "true" && "${GITHUB_EVENT_NAME:-}" == "push" && "${GITHUB_REF:-}" == "refs/heads/main" ]]; then
  echo "Testhost reused; Buildx still required to publish apps."
elif [[ "$hit" == "true" ]]; then
  need_buildx=false
  echo "Testhost reused; skipping Buildx."
fi

{
  echo "hit=${hit}"
  echo "need_buildx=${need_buildx}"
} | tee -a "${GITHUB_OUTPUT:-/dev/null}"
