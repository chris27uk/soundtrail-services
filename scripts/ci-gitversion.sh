#!/usr/bin/env bash
# Run GitVersion (standalone binary) and emit SemVer outputs.
# Expects: repo checkout with GitVersion.yml; optional /tmp/gitversion-prev floor.
# Env: PR_NUMBER, GITVERSION_URL (optional), WRITE_GITHUB_ENV=1 to also set job env.
set -euo pipefail

GITVERSION_URL="${GITVERSION_URL:-https://github.com/GitTools/GitVersion/releases/download/6.7.0/gitversion-linux-x64-6.7.0.tar.gz}"
PR_NUMBER="${PR_NUMBER:-}"

mkdir -p /tmp/gitversion-bin /tmp/gitversion-prev
gv_bin=/tmp/gitversion-bin/gitversion
if [[ ! -x "$gv_bin" ]]; then
  echo "Downloading GitVersion binary"
  curl -fsSL "$GITVERSION_URL" | tar -xzf - -C /tmp/gitversion-bin
  chmod +x "$gv_bin"
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    echo "need_bin_cache=true" >> "$GITHUB_OUTPUT"
  fi
fi

# Build uses fetch-depth: 1; Mainline needs history on HEAD plus the base branch.
# --deepen grows the current shallow tip; base/tags cover merge-base + next-version floor.
git fetch --no-tags --deepen=100 2>/dev/null || true
base_ref="${GITHUB_BASE_REF:-main}"
git fetch --no-tags --depth=100 origin "${base_ref}:refs/remotes/origin/${base_ref}" 2>/dev/null || true
git fetch --depth=1 origin "+refs/tags/v*:refs/tags/v*" 2>/dev/null || true

floor=""
if [[ -f /tmp/gitversion-prev/majorMinorPatch ]]; then
  floor=$(tr -d '[:space:]' < /tmp/gitversion-prev/majorMinorPatch)
  echo "SemVer floor from previous CI: ${floor}"
fi

last_tag=$(git describe --tags --match 'v[0-9]*' --abbrev=0 HEAD 2>/dev/null || true)
if [[ -z "$floor" && -n "$last_tag" ]]; then
  floor="${last_tag#v}"
  echo "SemVer floor from tag: ${floor}"
fi

gv_args=(/allowshallow /verbosity Quiet /output json)
if [[ -n "$floor" ]]; then
  gv_args+=(/overrideconfig "next-version=${floor}")
fi

set +e
"$gv_bin" "${gv_args[@]}" > /tmp/gitversion.json 2> /tmp/gitversion.err
gv_exit=$?
set -e

if [[ -s /tmp/gitversion.err ]]; then
  echo "GitVersion stderr:" >&2
  cat /tmp/gitversion.err >&2
fi

if [[ $gv_exit -ne 0 ]]; then
  echo "::error::GitVersion exited with code $gv_exit"
  echo "GitVersion stdout:" >&2
  cat /tmp/gitversion.json >&2 || true
  exit "$gv_exit"
fi

semVer=$(jq -r '.SemVer' /tmp/gitversion.json)
assemblySemVer=$(jq -r '.AssemblySemVer' /tmp/gitversion.json)
informationalVersion=$(jq -r '.InformationalVersion' /tmp/gitversion.json)
majorMinorPatch=$(jq -r '.MajorMinorPatch' /tmp/gitversion.json)

echo "$majorMinorPatch" > /tmp/gitversion-prev/majorMinorPatch
echo "$semVer" > /tmp/gitversion-prev/semVer

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

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "semVer=${semVer}"
    echo "assemblySemVer=${assemblySemVer}"
    echo "informationalVersion=${informationalVersion}"
    echo "majorMinorPatch=${majorMinorPatch}"
    echo "imageCacheKey=${cache_key}"
    echo "imageCacheFallback=${fallback}"
  } >> "$GITHUB_OUTPUT"
fi

if [[ "${WRITE_GITHUB_ENV:-}" == "1" && -n "${GITHUB_ENV:-}" ]]; then
  {
    echo "BUILD_VERSION=${semVer}"
    echo "GITVERSION_INFORMATIONALVERSION=${informationalVersion}"
    echo "OTEL_SERVICE_VERSION=${informationalVersion}"
  } >> "$GITHUB_ENV"
fi

echo "SemVer: ${semVer}"
echo "InformationalVersion: ${informationalVersion}"
echo "MajorMinorPatch: ${majorMinorPatch}"
echo "ImageCacheKey: ${cache_key}"
echo "ImageCacheFallback: ${fallback}"
