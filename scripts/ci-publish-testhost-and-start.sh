#!/usr/bin/env bash
# Publish testhost (if the cache/artifact missed), stamp its source hash,
# join the background version-meta wait, then start tests in the background.
# One step: publish is the only real cost here, everything else is either
# instant (stamp) or was already overlapped by an earlier background job
# (version-meta) or itself becomes a background job (tests).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
out="${GITHUB_OUTPUT:-/dev/null}"

published=false
if [[ "${TESTHOST_HIT:-false}" != "true" ]]; then
  echo "Testhost cache miss; publishing with build.ps1 -CiTesthost."
  pwsh -File ./build.ps1 -Restore -CiTesthost -OutputDir .
  published=true
else
  echo "Reusing testhost output; skipping publish."
fi
echo "published=${published}" >> "$out"

printf '%s\n' "${SOURCE_TREE_HASH:?SOURCE_TREE_HASH not set}" > testhost/source-hash

bash "$root/ci-await-version-meta-wait.sh"
bash "$root/ci-run-tests-bg.sh"
