#!/usr/bin/env bash
# Wait for the Version job's version-meta artifact, then export env for later steps.
set -euo pipefail

dest="${VERSION_META_DIR:-version-meta}"
rm -rf "$dest"
mkdir -p "$dest"

deadline=$((SECONDS + "${VERSION_META_WAIT_TIMEOUT:-180}"))
artifact_id=""

echo "Waiting for version-meta artifact from Version job"
while (( SECONDS < deadline )); do
  artifact_id=$(gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${GITHUB_RUN_ID}/artifacts" \
    --jq '.artifacts[] | select(.name=="version-meta" and .expired==false) | .id' 2>/dev/null | head -n1 || true)
  if [[ -n "$artifact_id" ]]; then
    break
  fi
  sleep 2
done

if [[ -z "$artifact_id" ]]; then
  echo "::error::Timed out waiting for version-meta artifact"
  exit 1
fi

gh api "repos/${GITHUB_REPOSITORY}/actions/artifacts/${artifact_id}/zip" > /tmp/version-meta.zip
unzip -qo /tmp/version-meta.zip -d "$dest"
rm -f /tmp/version-meta.zip

if [[ ! -f "$dest/version.env" ]]; then
  echo "::error::version-meta artifact missing version.env"
  exit 1
fi

# shellcheck disable=SC1091
set -a
# shellcheck source=/dev/null
source "$dest/version.env"
set +a

# Background await must not write GITHUB_ENV (collected per-step); wait script applies it.
if [[ "${SKIP_GITHUB_ENV:-}" != "1" && -n "${GITHUB_ENV:-}" ]]; then
  {
    echo "BUILD_VERSION=${BUILD_VERSION}"
    echo "GITVERSION_INFORMATIONALVERSION=${GITVERSION_INFORMATIONALVERSION}"
    echo "OTEL_SERVICE_VERSION=${OTEL_SERVICE_VERSION}"
  } >> "$GITHUB_ENV"
fi

echo "Applied BUILD_VERSION=${BUILD_VERSION}"
