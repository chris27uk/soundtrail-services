# MusicBrainz dump HTTP source (Aspire simulator)

Place MetaBrainz-layout archives here:

```text
LATEST                         # pointer file; body = concrete snapshot directory name
{SnapshotId}/artist.tar.xz
{SnapshotId}/release-group.tar.xz
{SnapshotId}/release.tar.xz
```

`LATEST` is a mutable pointer only. Job identity and CatalogImport cache keys use the concrete `{SnapshotId}` it resolves to (never the word `LATEST`).

Smoke archives for `2026-08` are committed for a quick local demo. For a real multi-GB exercise, download official JSON dumps from MetaBrainz into a snapshot folder and point `LATEST` at that folder name (do not commit multi-GB archives). CatalogImport pulls them over HTTP from this simulator into a local/blob cache.

Scheduled import resolves latest via `LATEST` (or newest snapshot directory). Manual import uses TickerQ function `ImportMusicBrainzDumpSnapshot` with request `{ "dumpVersion": "<SnapshotId>" }`.
