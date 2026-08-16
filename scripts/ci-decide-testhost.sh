#!/usr/bin/env bash
# After testhost GHA restore and optional PR artifact: reuse published bits or rebuild.
# need_buildx is true on miss, or on main (publish-apps still uses Buildx).
set -euo pipefail

source_hash="${SOURCE_TREE_HASH:-}"
hit=false

testhost_usable() {
  [[ -f testhost/Soundtrail.Services.Tests.dll ]] || return 1
  if [[ -f testhost/source-hash ]]; then
    [[ -n "$source_hash" && "$(tr -d '[:space:]' < testhost/source-hash)" == "$source_hash" ]]
    return
  fi
  # Legacy GHA cache has no stamp; the restore key is the source hash.
  [[ "${TESTHOST_CACHE_HIT:-false}" == "true" ]]
}

if testhost_usable; then
  hit=true
  echo "Reusing testhost output."
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
