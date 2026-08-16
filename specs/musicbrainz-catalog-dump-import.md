# MusicBrainz Catalog Dump Import

## Purpose

Define how Soundtrail loads MusicBrainz catalogue data from official JSON dumps into the event-sourced catalog, at dump scale (tens to hundreds of millions of rows), without tying up enrichment Service Bus workers or relying on live Raven CDC as the dump hot path.

This specification is the durable product and architecture contract for the **CatalogImport** service. It strategically replaces the previously assumed MusicBrainz dump console / Import Tool.

Related specs:

- `specs/music-catalog-discovery-api-v2.md` — local-first discovery API; dump import bypasses planner/worker
- `specs/event-sourcing-spec-ravendb-production.md` — event store and projection rebuild rules
- `specs/track-id-spec.md` — canonical identity for tracks

## Scope

### In scope

- Monthly (and manually triggered) MusicBrainz JSON dump ingest
- Client-side ETL (transform before load): download → decompress/shard → batch load
- Dedicated CatalogImport host (producer + consumers)
- Append catalog domain events and bulk materialise read models
- `SourceSystemId` provenance on catalog items
- Freshness rules between dump import and live Worker lookups
- OpenTelemetry progress and final job status
- Sociable-first testing and a local HTTP dump-source demo

### Out of scope (v1)

- Replacing live MusicBrainz Worker lookups entirely
- MusicBrainz dump console / Import Tool (obsolete; replaced by CatalogImport)
- Per-MBID Service Bus fan-out
- ELT (load raw JSON into Raven then transform)
- Using Raven as a competing work queue
- Running dump ETL inside Enrichment.Worker
- Pausing live Raven CDC subscriptions during dump
- Mediums (CD/DVD/digital carrier) as first-class catalog concepts
- Incremental MusicBrainz replication packets (design checkpoints so they can plug in later)
- ASB `Completed` / `Failed` lifecycle consumers (job document + OTel are sufficient)

## Architecture Principles

- Event store is the source of truth for catalog facts; projections are rebuildable from events alone.
- Dump import bypasses planner and Enrichment.Worker online lookup path.
- Import writes **domain events** (and bulk-projects read models); it does not treat Raven read models as the only write target.
- Azure Service Bus is used for **short control messages** only (start producer; dispatch shards). Long work runs out-of-band after message completion.
- Raven job documents hold **orchestration state and checkpoints**, not a message queue.
- Technology-independent ports expose domain objects; Contracts hold transport DTOs; adapters own blob/ASB/Raven/file I/O.
- Coding standards in `CODING-STANDARDS.md` apply (feature folders, sociable-first tests, no production in-memory fakes).

## Import Path

```text
TickerQ (Scheduler)
    -> ImportMusicBrainzDumpCommand (IScheduledMessage)
    -> StartMusicBrainzDumpImport (ASB, catalog-import queue)
    -> CatalogImport producer (download / shard)
    -> ImportMusicBrainzDumpShard × N (ASB)
    -> CatalogImport consumers (ETL batches)
    -> Event store (catalog-stream + ArtistCatalog)
    -> Bulk read-model projection (importer)
    -> RavenDB read models
```

Live Projector CDC continues for **online** traffic. Dump-appended events are tagged `ProjectionHint = bulk-import` and **excluded** from live subscription filters. Rebuild/repair loads streams without that exclusion.

## Trigger

- Owned by Enrichment.Scheduler via **TickerQ**.
- **Monthly cron** for routine dumps.
- **Manual TickerQ trigger** for occasional re-runs (e.g. after an import bug). Same handler as cron.
- Scheduler handler only: ensure job document + publish `StartMusicBrainzDumpImport`. No download/parse in the tick.

## Services And Roles

| Component | Role |
|---|---|
| Enrichment.Scheduler | Thin trigger (TickerQ → Start message) |
| Enrichment.CatalogImport | Producer and shard consumers |
| Enrichment.Worker | Unchanged online MB lookups; admission gated by freshness rules |
| Enrichment.Orchestrator | Skip MusicBrainz Worker when dump-fresh and complete; accept Low Odesli need from dump and High elevate from demand |
| Projector | Live CDC for non–bulk-import events; rebuild tools ignore bulk-import filter |

## Producer / Consumer Model

### Producer (exactly one owner per dump job)

- Lease ensures a single producer.
- Ensure dump archive on blob (**skip download** if already present).
- One decompress/split pass → N uncompressed JSONL shard blobs.
- Partition by stable hash of artist key; **copy** multi-credit rows into **each** credited artist’s shard.
- Publish `ImportMusicBrainzDumpShard` for **one phase at a time**; wait until all shards in that phase complete before publishing the next phase.
- Internal parallelism for shard writes is allowed; the producer lease is not.

### Consumers (any free CatalogImport instance)

- Compete for shard messages.
- Claim shard lease → complete ASB message in seconds → ETL that shard out-of-band.
- One Raven `BulkInsert` instance per consumer task (not shared across threads). Multiple concurrent BulkInsert instances across workers are supported.
- Preserve **per-stream sequence/version numbers** when writing event store documents.

### Shard sizing

Shards must be large enough that BulkInsert batches are worthwhile, and small enough that many consumers stay busy (tune empirically; dozens–hundreds of shards per phase, not millions).

## Phases And Entity Mapping

Processing order (phase gate):

1. Artists
2. Release groups / releases (as needed for Album + dates)
3. Recordings / tracks (as needed for Track metadata)

MusicBrainz → Soundtrail:

| MusicBrainz | Soundtrail |
|---|---|
| Artist | Artist |
| Release group (abstract album) | Album |
| Release | Supplies **release date** (applied to Track when track/recording has no date) |
| Recording / track data via release graph | Track |
| Medium (CD/DVD/digital) | Ignored in v1 |

Do not rely on the standalone-recording dump alone for catalogue tracks; use the release / release-group graph as needed. CatalogImport **materializes** denormalized track JSONL from official `release` dumps (media → tracks → recording + nested release-group + date) when a prebuilt track source is absent; it does not HTTP-download a Soundtrail-only `track` archive or the standalone `recording` dump.

## Messaging

Dedicated ASB queue (e.g. `catalog-import`). Do not reuse `lookup-musicbrainz`.

| Message | Transport | Purpose |
|---|---|---|
| `ImportMusicBrainzDumpCommand` | TickerQ / `IScheduledMessage` (not ASB) | Schedule or manual start |
| `StartMusicBrainzDumpImport` | ASB | Wake single producer |
| `ImportMusicBrainzDumpShard` | ASB | One shard for any free consumer |

Optional in-process `Channel` + pump on each CatalogImport host: local ack-then-continue so ASB handlers return in seconds. Channels do **not** replace ASB for cross-instance fan-out.

No per-row or per-MBID Service Bus messages. No v1 ASB Completed/Failed consumer.

## Checkpointing

ASB is unordered and at-least-once. It is **not** the progress authority.

Source of truth: Raven **job document**:

- Job status: `Pending` → `Downloading` → `Extracting` → `Importing` → `Completed` / `Failed` / `Cancelled`
- Dump version, timestamps, cancellation flag, producer lease
- Per-shard: `{ phase, shardId, lineOffset, status, lease }`

Rules:

- Shard order within a phase is irrelevant.
- Phase N+1 shards are published only when all phase-N shards are `Completed`.
- Duplicate shard delivery: `TryClaimShard` no-ops if leased or completed.
- Crash after ASB ack: lease expiry → republish or reclaim; resume from `lineOffset`.
- Load is at-least-once; writes must be idempotent under re-play.

## ETL Output Contract

### Domain events (required)

Append the same catalog facts the online path produces, e.g. `ArtistDiscovered`, `AlbumDiscovered`, `TrackDiscovered`, onto:

- catalog-stream (as applicable)
- `ArtistCatalog` aggregate streams

Each stored event carries `ProjectionHint`:

- `live` (default) — online path
- `bulk-import` — dump path; excluded from live CDC filters

### Read models

Bulk-project via the shared `ArtistCatalogProjectionMaterializer` / `ArtistCatalogProjectionDocuments` (same browse documents as live `ArtistCatalogChangedProjectorHandler`: artist, artist-albums, artist-tracks, album, album-tracks, track), plus search-candidate docs for dirty keys. Dump does **not** run playlist repair.

Any new artist-catalog browse/search projection must be added to this shared materializer (not only to a CDC handler).

### `SourceSystemId`

Value type `(System, Id)` with stable form `System:Id` (split on first `:`).

Stored as a **set** on Artist / Album / Track. Dump populates `musicbrainz:{mbid}` initially.

### Identity

`TrackId` / artist / album ids from **canonical name fields and parsing** (`specs/track-id-spec.md`), not from MBID.

## Freshness And Live Enrichment

- If a Worker lookup completes with data **newer than the dump file timestamp**, Worker data wins (may overwrite import).
- If import data is **older than** what is already stored for that entity, **skip** the import write (`ObservedAt` / dump observation comparison).
- **MusicBrainz Worker must not compete with dump** for catalog facts: do **not** schedule live MusicBrainz enrichment when the entity was dump-imported within the fresh window and catalog data is complete. Dump never enqueues MusicBrainz discover work.
- **Odesli (streaming locations):** dump enqueues `StreamingLocationForTrack` at `LookupPriorityBand.Low` for tracks written without locations. Demand (`GetTrack`, list discovery fan-out) elevates to `High` when locations are still missing. Worker Odesli budgets remain an absolute hard cap; Orchestrator reserved high-priority planner slots protect High from Low backlog.

## Re-import

If dump/entity data is newer than the last successful import for that entity, **re-import even when `SourceSystemId` already exists** (MusicBrainz is a community dataset that improves over time).

Re-trigger via TickerQ manual run. If the archive is already on blob, producer **must not** download again; re-run split/import from the existing file as appropriate for the job state.

## Bad Rows

Skip the row; record failed data for diagnosis; emit a metric. Do not fail the entire shard or job for a single bad row.

## Observability (OpenTelemetry)

- Progress percent: producer (file/phase while sharding), consumer (per-shard line offset / total), rolled up to job-level `%` on the job document and as gauges.
- Final status: job document `Completed` / `Failed` plus OTel activity status and job-status metric/event.
- Spans for download, decompress/shard, shard import (sample batch flushes if volume requires).

## Storage

- Dev: local disk and/or Azurite blob for **cache** (archives and shards) after HTTPS download.
- Prod: blob storage for compressed archive and uncompressed shard JSONL.
- Origin is always HTTPS (`MusicBrainzDump:BaseUrl`). Scheduled runs resolve the latest concrete snapshot id at the origin; manual runs require an explicit snapshot id. Missing versioned archives are downloaded into the local/blob cache under that concrete id.

## Testing

- Keep Domain / Contracts / adapter boundaries.
- **Sociable-first** (`Unit/Sociable/Features/Scheduled/ImportMusicBrainzDump/`), mirroring ImportKworbChart style.
- **Solitary** for `SourceSystemId`, shard hash stability, row→domain mapping, checkpoint math, resumable HTTPS download.
- **Fake+Real** integration contracts for dump reader / job store ports.
- Domain↔DTO translation round-trips for Start/Shard messages.
- Do not solitary-test ASB listeners or hosted services.

Minimum sociable scenarios: happy path one phase; resume from shard checkpoint; duplicate Start/Shard claim no-op; bad row skip + recorded failure; phase gate; freshness skip when store is newer.

### Local demo

Prerequisites: run AppHost with RavenDB, the Service Bus emulator, and (optionally) Azurite for blob dump storage.

AppHost starts a dedicated **HTTP dump-source** container (Caddy) that bind-mounts `Soundtrail.Services.AppHost/testdata/musicbrainz-dump-source/` and serves MetaBrainz-layout paths (`/{SnapshotId}/{entity}.tar.xz`) plus a `LATEST` pointer file whose body is the concrete smoke snapshot directory (e.g. `2026-08`). Scheduler and CatalogImport share `BaseUrl` → dump-source; CatalogImport caches under `testdata/musicbrainz-dump-cache/{SnapshotId}/`. Downloads resume via HTTP Range when interrupted.

- **Scheduled:** TickerQ function `ImportMusicBrainzDump` resolves latest → concrete snapshot id → ensure job → Start.
- **Manual:** TickerQ function `ImportMusicBrainzDumpSnapshot` with request `{ "dumpVersion": "<SnapshotId>" }` (required; must exist at origin; never pass `LATEST` as the version).

Startup validation fails fast if `LATEST`/snapshot dirs or required smoke archives are missing.

Trigger scheduled: open the Scheduler TickerQ dashboard and run `ImportMusicBrainzDump`. Trigger a specific snapshot: run `ImportMusicBrainzDumpSnapshot` with `dumpVersion`.

Observe: CatalogImport logs and OTel progress/status, Raven job document status/`ProgressPercent`, and catalog read models (artists / albums / tracks) written by the import.

## Non-Goals Recap

- DB-as-queue
- Per-MBID ASB fan-out
- Pause CDC for dump
- ELT into Raven
- Dump host = Enrichment.Worker
- Dump console / Import Tool
