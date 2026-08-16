#!/usr/bin/env bash
# On a main push, find a successful PR CI run that uploaded testhost-out.
# GHA cache is branch-scoped; PR → main must go through artifacts.
set -euo pipefail

out="${GITHUB_OUTPUT:-/dev/null}"
echo "run_id=" >> "$out"

if [[ "${GITHUB_EVENT_NAME:-}" != "push" || "${GITHUB_REF:-}" != "refs/heads/main" ]]; then
  echo "Not a main push; skip PR testhost lookup."
  exit 0
fi

repo="${GITHUB_REPOSITORY:?}"
sha="${GITHUB_SHA:?}"

declare -a prs=()
add_pr() {
  local n="$1"
  [[ -n "$n" ]] || return 0
  local existing
  for existing in "${prs[@]+"${prs[@]}"}"; do
    [[ "$existing" == "$n" ]] && return 0
  done
  prs+=("$n")
}

while IFS= read -r n; do
  add_pr "$n"
done < <(gh api "repos/${repo}/commits/${sha}/pulls" --jq '.[].number' 2>/dev/null || true)

msg=$(gh api "repos/${repo}/commits/${sha}" --jq .commit.message 2>/dev/null || true)
if [[ "$msg" =~ Merge\ pull\ request\ #([0-9]+) ]]; then
  add_pr "${BASH_REMATCH[1]}"
elif [[ "$msg" =~ \(#([0-9]+)\) ]]; then
  add_pr "${BASH_REMATCH[1]}"
fi

if [[ ${#prs[@]} -eq 0 ]]; then
  echo "No PR associated with ${sha}."
  exit 0
fi

for pr in "${prs[@]}"; do
  head_ref=$(gh api "repos/${repo}/pulls/${pr}" --jq .head.ref)
  echo "Looking for testhost-out on PR #${pr} (${head_ref})"
  while IFS= read -r run_id; do
    [[ -n "$run_id" ]] || continue
    found=$(gh api "repos/${repo}/actions/runs/${run_id}/artifacts" \
      --jq '[.artifacts[] | select(.name=="testhost-out" and .expired==false) | .id] | first // empty')
    if [[ -n "$found" ]]; then
      echo "Found testhost-out on run ${run_id} (PR #${pr})"
      echo "run_id=${run_id}" >> "$out"
      exit 0
    fi
  done < <(
    gh run list --repo "$repo" --workflow CI --branch "$head_ref" \
      --event pull_request --status success --limit 15 \
      --json databaseId --jq '.[].databaseId'
  )
done

echo "No testhost-out artifact on associated PR runs."
