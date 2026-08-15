#!/usr/bin/env bash
# Restore sidecars.tar from GHA cache in the background (overlaps buildx/build).
# Uses @actions/cache (same API as actions/cache/restore) so hyphenated INPUT_*
# env quirks and restore-only entrypoint issues are avoided.
set -euo pipefail

outdir=/tmp/sidecar-cache
mkdir -p /tmp/sidecar-images "$outdir"
rm -f /tmp/sidecar-cache.done /tmp/sidecar-cache.hit
: >"$outdir/github_output"

if [[ -z "${SIDECAR_CACHE_KEY:-}" ]]; then
  echo "SIDECAR_CACHE_KEY not set; skipping sidecar cache restore"
  echo "false" >/tmp/sidecar-cache.hit
  touch /tmp/sidecar-cache.done
  exit 0
fi

(
  set +e
  echo "Restoring sidecar cache key=$SIDECAR_CACHE_KEY"
  if [[ -z "${ACTIONS_CACHE_URL:-}${ACTIONS_RESULTS_URL:-}" || -z "${ACTIONS_RUNTIME_TOKEN:-}" ]]; then
    echo "ACTIONS_CACHE_URL/RESULTS_URL or ACTIONS_RUNTIME_TOKEN missing; cannot restore"
    echo "false" >/tmp/sidecar-cache.hit
    touch /tmp/sidecar-cache.done
    exit 0
  fi

  pkg=/tmp/ci-actions-cache
  mkdir -p "$pkg"
  # Install overlaps tooling docker load / buildx (~seconds).
  if [[ ! -d "$pkg/node_modules/@actions/cache" ]]; then
    echo "Installing @actions/cache for background restore"
    npm install --prefix "$pkg" @actions/cache@4.0.3 --no-fund --no-audit --silent
  fi

  node <<'NODE'
const fs = require('fs');
const cache = require('/tmp/ci-actions-cache/node_modules/@actions/cache');

const path = '/tmp/sidecar-images/sidecars.tar';
const key = process.env.SIDECAR_CACHE_KEY;
const restoreKeys = (process.env.SIDECAR_CACHE_RESTORE_KEYS || '')
  .split(/\r?\n/)
  .map((s) => s.trim())
  .filter(Boolean);

(async () => {
  let matched = undefined;
  try {
    matched = await cache.restoreCache([path], key, restoreKeys);
  } catch (err) {
    console.error('restoreCache failed:', err && err.message ? err.message : err);
    if (err && err.stack) console.error(err.stack);
  }
  const exact = matched === key;
  fs.writeFileSync('/tmp/sidecar-cache.hit', exact ? 'true' : 'false');
  fs.writeFileSync('/tmp/sidecar-cache.matched', matched || '');
  console.log(`matched-key=${matched || '(none)'} exact-hit=${exact}`);
  if (matched && fs.existsSync(path)) {
    const st = fs.statSync(path);
    console.log(`restored ${path} (${st.size} bytes)`);
  }
})().finally(() => {
  fs.writeFileSync('/tmp/sidecar-cache.done', '1');
});
NODE
  status=$?
  if [[ "$status" -ne 0 ]]; then
    echo "Sidecar cache restore node exited $status"
    echo "false" >/tmp/sidecar-cache.hit
    touch /tmp/sidecar-cache.done
  fi
) >/tmp/sidecar-cache.log 2>&1 &
echo $! >/tmp/sidecar-cache.pid
echo "Sidecar cache restore pid=$(cat /tmp/sidecar-cache.pid)"
