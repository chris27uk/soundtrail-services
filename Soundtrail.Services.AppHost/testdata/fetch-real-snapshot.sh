#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="${SCRIPT_DIR}/musicbrainz-dump-source"
BASE_URL="${MB_DUMP_BASE_URL:-https://data.metabrainz.org/pub/musicbrainz/data/json-dumps}"

if [[ $# -ge 1 ]]; then
  SNAPSHOT="$1"
else
  echo "Resolving LATEST from MetaBrainz (${BASE_URL}/LATEST)..."
  SNAPSHOT="$(curl -fsS "${BASE_URL}/LATEST" | tr -d '[:space:]')"
fi

if [[ -z "${SNAPSHOT}" ]]; then
  echo "Could not resolve snapshot id." >&2
  exit 1
fi

TARGET="${SOURCE_DIR}/${SNAPSHOT}"
mkdir -p "${TARGET}"

entities=(artist release-group release)

for entity in "${entities[@]}"; do
  url="${BASE_URL}/${SNAPSHOT}/${entity}.tar.xz"
  dest="${TARGET}/${entity}.tar.xz"
  echo "Downloading ${entity}.tar.xz -> ${dest}"
  curl -fsSL --retry 5 --continue-at - -o "${dest}" "${url}"
done

printf '%s\n' "${SNAPSHOT}" > "${SOURCE_DIR}/LATEST"
echo "Done. LATEST -> ${SNAPSHOT}"
echo "Archives: ${TARGET}/"
