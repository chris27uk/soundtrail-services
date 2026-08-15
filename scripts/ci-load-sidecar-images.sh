#!/usr/bin/env bash
# Load/pull Redis/OSB/Raven images only (no compose up). Aspnet is runtime.tar.
# Signals /tmp/sidecars.loaded on success.
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

tar_path="${SIDECAR_IMAGE_TAR:-/tmp/sidecar-images/sidecars.tar}"
mkdir -p "$(dirname "$tar_path")" reports
rm -f /tmp/sidecars.loaded /tmp/sidecars.load-failed

cleanup_on_error() {
  touch /tmp/sidecars.load-failed
}
trap cleanup_on_error ERR

image_present() {
  docker image inspect "$1" >/dev/null 2>&1
}

ensure_image() {
  local ref=$1
  if image_present "$ref"; then
    echo "Already present: $ref"
    return 0
  fi
  echo "Pulling $ref"
  docker pull "$ref"
}

# Digest pull may leave only an ID / digest ref. Compose + docker save need RepoTags.
pin_tag_from_digest() {
  local digest_ref=$1
  local tag=$2
  if image_present "$tag"; then
    return 0
  fi
  local id
  id=$(docker image inspect -f '{{.Id}}' "$digest_ref")
  docker tag "$id" "$tag"
  echo "Pinned $tag <- $digest_ref"
}

ensure_pinned_sidecars() {
  local i pids=() p status=0
  for i in "${!CI_SIDECAR_IMAGES[@]}"; do
    (
      ensure_image "${CI_SIDECAR_IMAGES[$i]}"
      pin_tag_from_digest "${CI_SIDECAR_IMAGES[$i]}" "${CI_SIDECAR_TAGS[$i]}"
    ) &
    pids+=($!)
  done
  for p in "${pids[@]}"; do
    wait "$p" || status=1
  done
  return "$status"
}

tags_present() {
  local tag
  for tag in "${CI_SIDECAR_TAGS[@]}"; do
    if ! image_present "$tag"; then
      return 1
    fi
  done
  return 0
}

tar_ok=0
if [[ -f "$tar_path" ]]; then
  tar_size=$(stat -c%s "$tar_path" 2>/dev/null || stat -f%z "$tar_path")
  if (( tar_size >= 10000000 )); then
    echo "Loading cached sidecar images from $tar_path ($tar_size bytes)"
    if docker load -i "$tar_path"; then
      tar_ok=1
    else
      echo "Cached sidecar tar was unusable; pulling instead."
      rm -f "$tar_path"
    fi
  else
    echo "Cached sidecar tar is too small ($tar_size bytes); pulling instead."
    rm -f "$tar_path"
  fi
fi

if [[ "$tar_ok" -ne 1 ]] || ! tags_present; then
  if [[ "$tar_ok" -eq 1 ]]; then
    echo "Cached sidecar tar missing tag refs; ensuring digest pins (will reseed on miss/new key)."
    touch /tmp/sidecars.need-reseed
  fi
  ensure_pinned_sidecars
else
  for tag in "${CI_SIDECAR_TAGS[@]}"; do
    echo "Already present: $tag"
  done
fi

touch /tmp/sidecars.loaded
echo "Sidecar images loaded."
