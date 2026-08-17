# MusicBrainz dump HTTP source (Aspire simulator)

Official MetaBrainz JSON dump archives are served from **your host filesystem** via a read-only Docker bind mount into the `musicbrainz-dump-source` Caddy container. Nothing multi-GB lives inside the container image or an anonymous Docker volume.

CatalogImport downloads from this simulator over HTTP into `../musicbrainz-dump-cache/` (also on the host, fully gitignored).

## Layout

```text
musicbrainz-dump-source/
  LATEST                         # pointer file; body = concrete snapshot directory name
  {SnapshotId}/
    artist.tar.xz
    release-group.tar.xz
    release.tar.xz
  2026-08/                       # committed smoke fixture (tiny archives)
```

`LATEST` is a mutable pointer only. Job identity and CatalogImport cache keys use the concrete `{SnapshotId}` (never the word `LATEST`).

Required entities for import: **artist**, **release-group**, **release**. Tracks are materialized by joining the release graph during import (no `track.tar.xz` needed for real dumps; smoke `2026-08/track.tar.xz` is fixture-only).

AppHost does **not** point `MusicBrainzDump__BaseUrl` at production. You download archives manually (or via the helper script) into this folder.

## Download a real snapshot

### Option A — helper script

From the repo:

```bash
chmod +x Soundtrail.Services.AppHost/testdata/fetch-real-snapshot.sh
./Soundtrail.Services.AppHost/testdata/fetch-real-snapshot.sh
# or explicit id:
./Soundtrail.Services.AppHost/testdata/fetch-real-snapshot.sh 20260815-001001
```

The script resolves `LATEST` from MetaBrainz when no argument is given, downloads the three archives with resume support (`curl -C -`), and writes `LATEST`.

### Option B — manual

```bash
SOURCE=Soundtrail.Services.AppHost/testdata/musicbrainz-dump-source
BASE=https://data.metabrainz.org/pub/musicbrainz/data/json-dumps
SNAPSHOT=$(curl -fsS "$BASE/LATEST" | tr -d '[:space:]')
mkdir -p "$SOURCE/$SNAPSHOT"

for entity in artist release-group release; do
  curl -fsSL --retry 5 --continue-at - -o "$SOURCE/$SNAPSHOT/$entity.tar.xz" \
    "$BASE/$SNAPSHOT/$entity.tar.xz"
done

printf '%s\n' "$SNAPSHOT" > "$SOURCE/LATEST"
```

Expect multi-GB downloads; `release.tar.xz` dominates disk and time. Ensure tens of GB free for archives plus cache extract under `musicbrainz-dump-cache/`.

## Gitignore

- Real snapshot folders matching `YYYYMMDD-HHMMSS/` (e.g. `20260815-001001/`) are gitignored (see `.gitignore` here).
- Smoke `2026-08/` remains tracked.
- Extracted/cache data under `../musicbrainz-dump-cache/` is fully gitignored.

## Verify simulator before import

With AppHost running:

```bash
# dump-source URL from Aspire
URL=$(aspire describe musicbrainz-dump-source --apphost ./Soundtrail.Services.AppHost --format Json --nologo 2>/dev/null \
  | python3 -c 'import sys,json; o=json.load(sys.stdin); print(o["urls"][0]["url"])')

curl -fsS "$URL/LATEST"
curl -fsI "$URL/$(curl -fsS "$URL/LATEST" | tr -d '[:space:]')/artist.tar.xz" | head -5
```

## Run the import

1. AppHost healthy: `aspire describe --apphost ./Soundtrail.Services.AppHost`
2. TickerQ dashboard: `http://localhost:5181/tickerq`
3. Trigger either:
   - **ImportMusicBrainzDump** — resolves `LATEST` via Scheduler snapshot catalog, or
   - **ImportMusicBrainzDumpSnapshot** — request body: `{ "dumpVersion": "20260815-001001" }`
4. Watch:
   - `aspire logs soundtrail-catalog-import --apphost ./Soundtrail.Services.AppHost`
   - Raven job doc `musicbrainz-dump:{SnapshotId}` → `Status: Completed`, `LastError: null`
   - Catalog collections (`CatalogArtistRecordDto`, albums, tracks) non-zero

First run can take hours (HTTP copy from simulator → cache, extract, release join, sharded import).

## Odesli (local stubs)

With `LocalDevelopment:UseProviderStubs`, Odesli goes to WireMock. Empty `linksByPlatform` responses are fine for this exercise. The dump still enqueues Low `streaming_location_for_track` work; expect Worker NotFound churn on a full catalog — no action required for this pass.

## Smoke vs real

| | Smoke `2026-08` | Real snapshot e.g. `20260815-001001` |
|--|-----------------|--------------------------------------|
| Size | KB | Multi-GB |
| In git | Yes (tiny fixtures) | No (gitignored) |
| Use | Quick local demo | Full catalog E2E |

Scheduled import resolves latest via `LATEST` (or newest snapshot directory). Manual import uses TickerQ `ImportMusicBrainzDumpSnapshot` with `{ "dumpVersion": "<SnapshotId>" }`.
