#!/usr/bin/env bash
# Kick every independent background job for this run in one step.
# Each *-bg.sh script forks via nohup and returns immediately, so calling
# them back to back here starts jobs running truly in parallel while the
# foreground steps below (testhost decide/publish) proceed. Version-meta
# has no dependency on the sidecar tars; sidecars/runtime load the tars
# the preceding cache-restore step just populated.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-await-version-meta-bg.sh"
bash "$root/ci-start-sidecars-bg.sh"
bash "$root/ci-load-runtime-bg.sh"
