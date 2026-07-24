# TrackId Implementation Plan

## Purpose

This document locks the execution order for migrating `Soundtrail.Services` to the RavenDB index-driven `TrackId` model.

The target state is:

- one persisted `TrackId` field per projection document
- fixed-width lowercase hexadecimal packed identity
- RavenDB index projection helper for all query fields
- database-driven family filtering and sibling ranking
- dedicated song-title parser feeding identity construction

## Phase 1: Lock The Contract

Goal:

- freeze the target-state identity, indexing, and parser rules before broader code changes

Deliverables:

- update `specs/track-id-spec.md`
- record the migration order in this plan

Status:

- in progress during this change set

## Phase 2: Introduce Packed Hex TrackId Foundation

Goal:

- replace the current segmented `TrackId` representation with a fixed-width hex encoding

Deliverables:

- update domain `TrackId` creation and parsing
- define packed base and vector segments
- preserve one persisted `TrackId` field only

Expected code areas:

- `src/Soundtrail.Domain/Catalog/Tracks/TrackId.cs`
- `src/Soundtrail.Domain/Catalog/Tracks/TrackIdentityMath.cs`
- supporting value objects and tests

## Phase 3: Introduce Raven Index Projection Helper

Goal:

- centralize index-time decoding of packed `TrackId`

Deliverables:

- add a shared helper dedicated to Raven-safe derived fields
- expose family selectors
- expose vector/ranking fields
- expose deterministic tie-break fields

Expected code areas:

- domain or infrastructure helper dedicated to projection
- Raven static indexes and projector query paths

## Phase 4: Move Query Flow To Index-Driven Resolution

Goal:

- make exact resolution and sibling fallback fully DB-driven

Deliverables:

- exact `TrackId` fast path
- family filtering by decoded base fields
- DB-side ranking using derived vector fields

Expected code areas:

- projector ports
- API read ports
- enrichment candidate resolution

## Phase 5: Replace Old Split-Key Assumptions

Goal:

- remove remaining logic that assumes stored helper identity fields

Deliverables:

- remove old split-key usage from projectors and tests
- ensure all projections persist only `TrackId`
- ensure all querying depends on decoded index outputs

## Phase 6: Implement Song Title Parser

Goal:

- separate core title identity from trailing release-type qualifiers

Deliverables:

- single-pass parser
- detachable trailing segment grammar
- controlled release-type vocabulary
- conservative fallback behavior

Expected forms:

- `Song (Radio Edit)`
- `Song [Live Mix]`
- `Song - Radio Edit`

Non-goal:

- reinterpreting qualifier-like words inside the core title body

## Phase 7: Feed Parser Output Into Identity Construction

Goal:

- make parser output authoritative for base-title and release-type construction

Deliverables:

- parsed core title feeds `BaseIdentityBits`
- parsed release type feeds `VectorBits`
- identity generation paths share one parser contract

## Phase 8: Verification

Goal:

- prove the new model works end to end

Deliverables:

- packed hex round-trip tests
- family grouping tests
- nearest-sibling ranking tests
- parser tests
- playlist scenario coverage with generic source references and mixed sibling availability

## Migration Notes

- breaking changes are acceptable
- compatibility layers are intentionally not required
- data reset / rebuild is acceptable
- index rebuild is preferred over projection schema expansion
