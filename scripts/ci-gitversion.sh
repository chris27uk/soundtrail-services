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

# Mainline walks merge parents. A shallow checkout of pull/N/merge often lacks
# those parents after "merge main into PR", which GitVersion reports as
# "Cannot find the base commit of merged branch."
base_ref="${GITHUB_BASE_REF:-main}"
fetch_version_history() {
  git fetch --no-tags --deepen=100 2>/dev/null || true
  git fetch --no-tags --depth=100 origin "${base_ref}:refs/remotes/origin/${base_ref}" 2>/dev/null || true
  if [[ -n "${GITHUB_HEAD_REF:-}" ]]; then
    git fetch --no-tags origin "${GITHUB_HEAD_REF}:refs/remotes/origin/${GITHUB_HEAD_REF}" 2>/dev/null || true
  fi
  git fetch --depth=1 origin "+refs/tags/v*:refs/tags/v*" 2>/dev/null || true
}

unshallow_version_history() {
  echo "Unshallowing for GitVersion Mainline merge-base walk."
  if [[ "$(git rev-parse --is-shallow-repository)" == "true" ]]; then
    git fetch --unshallow --no-tags origin 2>/dev/null \
      || git fetch --no-tags --deepen=5000 origin 2>/dev/null \
      || true
  fi
  git fetch --no-tags origin "${base_ref}:refs/remotes/origin/${base_ref}" 2>/dev/null || true
  if [[ -n "${GITHUB_HEAD_REF:-}" ]]; then
    git fetch --no-tags origin "${GITHUB_HEAD_REF}:refs/remotes/origin/${GITHUB_HEAD_REF}" 2>/dev/null || true
  fi
}

fetch_version_history

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

run_gitversion() {
  set +e
  "$gv_bin" "${gv_args[@]}" > /tmp/gitversion.json 2> /tmp/gitversion.err
  gv_exit=$?
  set -e
}

run_gitversion

if [[ $gv_exit -ne 0 ]] && grep -q "Cannot find the base commit of merged branch" /tmp/gitversion.err /tmp/gitversion.json 2>/dev/null; then
  unshallow_version_history
  run_gitversion
fi

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
