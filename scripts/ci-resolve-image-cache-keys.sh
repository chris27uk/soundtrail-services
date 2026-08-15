#!/usr/bin/env bash
# Derive sidecar image cache key/fallback without waiting on the Version job.
# Mirrors Version job logic so Build can restore caches in parallel.
set -euo pipefail

PR_NUMBER="${PR_NUMBER:-}"
fallback="none"

# Best-effort last release tag for restore-keys (shallow checkout may lack history).
git fetch --depth=1 origin "+refs/tags/v*:refs/tags/v*" 2>/dev/null || true
last_tag=$(git describe --tags --match 'v[0-9]*' --abbrev=0 HEAD 2>/dev/null || true)
fallback="${last_tag:-none}"

if [[ "${GITHUB_REF:-}" == refs/tags/v* ]]; then
  cache_key="${GITHUB_REF_NAME}"
  prev_tag=$(git describe --tags --match 'v[0-9]*' --abbrev=0 HEAD^ 2>/dev/null || true)
  fallback="${prev_tag:-$fallback}"
elif [[ "${GITHUB_EVENT_NAME:-}" == pull_request && -n "$PR_NUMBER" ]]; then
  cache_key="pr-${PR_NUMBER}"
else
  cache_key="main"
fi

{
  echo "imageCacheKey=${cache_key}"
  echo "imageCacheFallback=${fallback}"
} | tee -a "${GITHUB_OUTPUT:-/dev/null}"

echo "ImageCacheKey=${cache_key}"
echo "ImageCacheFallback=${fallback}"
