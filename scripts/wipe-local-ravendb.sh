#!/usr/bin/env bash
# Wipe local AppHost RavenDB data so the next Aspire run starts clean.
#
# Usage:
#   ./scripts/wipe-local-ravendb.sh              # hard-delete database "soundtrail" (Raven must be up)
#   ./scripts/wipe-local-ravendb.sh --purge      # also remove the Docker volume (stop AppHost / ravendb first)
#
# Env:
#   RAVEN_URL          default http://localhost:8080 (also tries http://ravendb.localhost)
#   RAVEN_DATABASE     default soundtrail
#   RAVEN_VOLUME_NAME  default soundtrail-ravendb-data

set -euo pipefail

DATABASE="${RAVEN_DATABASE:-soundtrail}"
VOLUME_NAME="${RAVEN_VOLUME_NAME:-soundtrail-ravendb-data}"
PURGE=0

for arg in "$@"; do
  case "$arg" in
    --purge|-p)
      PURGE=1
      ;;
    -h|--help)
      sed -n '2,12p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 1
      ;;
  esac
done

resolve_raven_url() {
  if [[ -n "${RAVEN_URL:-}" ]]; then
    printf '%s\n' "${RAVEN_URL}"
    return
  fi

  for candidate in "http://localhost:8080" "http://ravendb.localhost"; do
    if curl -fsS --max-time 2 "${candidate}/studio/index.html" >/dev/null 2>&1 \
      || curl -fsS --max-time 2 "${candidate}/" >/dev/null 2>&1; then
      printf '%s\n' "${candidate}"
      return
    fi
  done

  return 1
}

hard_delete_database() {
  local base_url="$1"
  local url="${base_url%/}/admin/databases?name=${DATABASE}&hard-delete=true"
  echo "Hard-deleting RavenDB database '${DATABASE}' at ${base_url} ..."
  local code
  code="$(curl -sS -o /tmp/soundtrail-raven-wipe-body.txt -w '%{http_code}' -X DELETE "${url}")"
  if [[ "${code}" != "200" && "${code}" != "204" && "${code}" != "404" ]]; then
    echo "Delete failed (HTTP ${code}):" >&2
    cat /tmp/soundtrail-raven-wipe-body.txt >&2 || true
    exit 1
  fi
  if [[ "${code}" == "404" ]]; then
    echo "Database '${DATABASE}' was already absent."
  else
    echo "Database '${DATABASE}' deleted."
  fi
}

purge_volume() {
  if ! docker volume inspect "${VOLUME_NAME}" >/dev/null 2>&1; then
    echo "Docker volume already absent: ${VOLUME_NAME}"
    return
  fi

  echo "Removing Docker volume: ${VOLUME_NAME}"
  docker volume rm "${VOLUME_NAME}"
  echo "Volume removed."
}

if raven_url="$(resolve_raven_url)"; then
  hard_delete_database "${raven_url}"
else
  if [[ "${PURGE}" -eq 0 ]]; then
    echo "RavenDB is not reachable on localhost:8080 or ravendb.localhost." >&2
    echo "Start the AppHost, or re-run with --purge after stopping Raven to remove the data volume." >&2
    exit 1
  fi
  echo "RavenDB is not reachable; skipping HTTP delete and purging the Docker volume only."
fi

if [[ "${PURGE}" -eq 1 ]]; then
  purge_volume
fi

echo "Done."
