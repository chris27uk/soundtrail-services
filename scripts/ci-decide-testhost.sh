#!/usr/bin/env bash
# After testhost GHA restore and optional PR artifact: reuse published bits or rebuild.
# Testhost uses the runner SDK. Buildx verifies Dockerfile.ci (and publishes apps on main).
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

# Testhost still uses runner-native dotnet publish. Buildx compiles Dockerfile.ci
# on every CI run so PRs catch COPY-list drift before main publish.
need_buildx=true
if [[ "${GITHUB_EVENT_NAME:-}" == "push" && "${GITHUB_REF:-}" == "refs/heads/main" ]]; then
  echo "Buildx required to publish apps on main."
else
  echo "Buildx required to verify Dockerfile.ci compile."
fi

{
  echo "hit=${hit}"
  echo "need_buildx=${need_buildx}"
} | tee -a "${GITHUB_OUTPUT:-/dev/null}"
