# Music Catalog Discovery API v1

## Purpose

Build a C# music catalog API for discovering metadata for a UI and enabling track playback through supported streaming providers.

The API is local-first. It returns known catalog data quickly from RavenDB and records discovery requests when local data is incomplete.

Supported providers:

- Apple Music
- Spotify
- YouTube Music

Canonical music metadata source:

- MusicBrainz

Provider playback reference source:

- Odesli / Songlink

## Scope

### In scope

- local-first search
- metadata lookup for artists, albums, and tracks
- listing tracks by album
- listing tracks by artist across albums
- discovery request planning
- source API budget enforcement
- event-sourced discovery facts
- RavenDB projections
- Azure Service Bus integration
- support for MusicBrainz dump import through events
- provider availability and playability status
- future-safe metadata repair/enrichment events

### Out of scope

- public API security
- customer API rate limiting
- app attestation
- MusicBrainz dump console implementation
- direct Sonos queue manipulation
- Sonos URI generation
- artwork implementation for MVP

Artwork is not part of MVP, but the model must support adding artwork shortly after MVP without replacing existing documents.

## Architecture Principles

- API is local-first.
- API never calls MusicBrainz or Odesli directly.
- RavenDB projections are the read model.
- Event store is the source of truth.
- Discovery is asynchronous.
- Components communicate through Azure Service Bus.
- Feature folders and business-focused naming are mandatory.
- Outside-in sociable TDD is the required testing approach.

## Routes

```http
GET /search
GET /artists/{artistId}
GET /artists/{artistId}/tracks
GET /artists/{artistId}/albums/{albumId}
GET /artists/{artistId}/albums/{albumId}/tracks/{trackId}
```

No additional routes are part of v1.

## Search

### Request

```http
GET /search?q={query}&types={types}&playback={providers}&limit={limit}&offset={offset}
```

Parameters:

- q (required)
- types = artist, album, track
- playback = spotify, appleMusic, youtubeMusic
- limit default 25 max 100
- offset default 0

### Playback Filtering

The consumer may specify allowed playback mechanisms.

A result is returned if:

- no playback filter is supplied, or
- at least one requested provider is available.

### Response Shape

```json
{
  "query": "karma police",
  "results": [],
  "discovery": {
    "willBeLookedUp": true,
    "reason": "Local results incomplete",
    "retryAfterSeconds": 30
  }
}
```

### Discovery Behaviour

If local metadata is incomplete:

1. Append DiscoveryRequested event.
2. Return local results immediately.
3. Return discovery status.
4. Never block waiting for external APIs.

## Metadata APIs

### Get Artist

```http
GET /artists/{artistId}
```

Returns artist metadata and known albums.

### List Tracks By Artist

```http
GET /artists/{artistId}/tracks
```

Returns tracks across all albums.

### Get Album

```http
GET /artists/{artistId}/albums/{albumId}
```

Returns album metadata and tracks.

### Get Track

```http
GET /artists/{artistId}/albums/{albumId}/tracks/{trackId}
```

Track IDs are globally unique.

The supplied artist and album must match the stored hierarchy.

Return 404 if hierarchy is invalid.

## Identity Rules

Track IDs are globally unique.

Tracks with the same name on different albums are not assumed to be identical.

Tracks store references to both artist and album.

## Playability

```csharp
public enum PlayabilityStatus
{
    Unknown,
    Playable,
    NotYetDiscovered,
    TerminallyUnavailable
}
```

Provider failures are tracked per provider.

Example:

```json
{
  "availableProviders": ["spotify"],
  "terminallyUnavailableProviders": ["youtubeMusic"]
}
```

## Provider References

This API does not generate Sonos URIs.

The companion local Sonos project is responsible for URI generation.

Provider references only:

```csharp
public sealed record ProviderReference(
    StreamingProvider Provider,
    string ProviderEntityType,
    string ProviderId,
    Uri Url,
    DateTimeOffset DiscoveredAt
);
```

## Artwork

Not part of MVP.

Store only highest-quality artwork URL.

```csharp
public sealed record ArtworkReference(
    Uri Url,
    string Source,
    DateTimeOffset DiscoveredAt
);
```

Artwork is discovered through events.

## Data Storage

### Event Store

Source of truth.

### RavenDB

Projection store and search store.

### Import Path

MusicBrainz dump import bypasses planner and worker.

```text
Import Tool
    -> Event Store
    -> Projector
    -> RavenDB
```

Import writes events, never RavenDB documents directly.

## Core Events

```csharp
ArtistDiscovered
AlbumDiscovered
TrackDiscovered
ProviderReferenceDiscovered
ProviderReferenceLookupFailed
ArtworkDiscovered
MetadataCorrected
DiscoveryRequested
DiscoveryPlanned
DiscoveryDeferred
DiscoveryRejected
DiscoveryFailed
```

## Discovery Status

Planner owns retry estimates.

API never calculates them.

### Discovery Status Projection

```csharp
public sealed class DiscoveryStatusDocument
{
    public string Id { get; init; }
    public string QueryKey { get; init; }
    public DiscoveryLifecycleStatus Status { get; init; }
    public DiscoveryPriority Priority { get; init; }
    public bool WillBeLookedUp { get; init; }
    public int? EstimatedRetryAfterSeconds { get; init; }
    public DateTimeOffset? EarliestExpectedCompletionAt { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

### Discovery Lifecycle

```csharp
public enum DiscoveryLifecycleStatus
{
    Requested,
    Planned,
    Deferred,
    InProgress,
    Completed,
    Failed,
    Rejected
}
```

### Query Keys

Examples:

```text
search:track:karma police
search:artist:radiohead
artist:artist_123
album:album_456
track:track_789
```

## Deployables

### MusicCatalog.Api

Responsibilities:

- expose HTTP routes
- query RavenDB
- append discovery requests

### Discovery.Planner

Responsibilities:

- prioritisation
- budget enforcement
- scheduling
- retry estimation

### Discovery.Worker

Responsibilities:

- MusicBrainz lookups
- Odesli lookups
- append discovery facts

### Catalog.Projector

Responsibilities:

- update RavenDB projections
- maintain search indexes

### IntegrationEvents.Cdc

Responsibilities:

- convert domain events into integration events
- Azure Service Bus publication
- outboxing if required using Wolverine

### DiscoveryOrchestration

Responsibilities:

- append discovery lifecycle domain events to discovery event streams
- own discovery planning decisions and retry timing
- translate execution outcomes into discovery lifecycle transitions
- publish planned discovery work through CDC or boundary messaging

### DiscoveryExecution

Responsibilities:

- execute external metadata and playback reference lookups
- enforce source API budgets for concrete executions
- return execution outcomes only
- never mutate discovery read models or discovery aggregates directly

## Source API Budgets

Consumer API rate limiting is out of scope.

Source API budget enforcement is mandatory.

Planner and workers must ensure MusicBrainz and Odesli budgets are not exceeded.

MusicBrainz default behaviour must not exceed 1 request per second unless a private mirror is introduced.

## Search Indexing

Required indexes:

```text
Search/Artists
Search/Albums
Search/Tracks
Artists/ByMusicBrainzId
Albums/ByArtistAndName
Albums/ByMusicBrainzReleaseId
Tracks/ByArtistAlbumAndName
Tracks/ByMusicBrainzRecordingId
Tracks/ByIsrc
```

Provider availability must be indexed to support efficient playback-filtered search.

## Deduplication

Primary matching signals:

1. MusicBrainz identifiers
2. Normalised artist name
3. Normalised album name
4. Release date
5. Track similarity

Album names alone are not unique.

Compilation albums and various-artist albums must be supported.

## Feature Folder Structure

```text
Features/
  Search/
  Artists/
  Albums/
  Tracks/
```

Business-focused naming only.

Examples:

```text
SearchCatalog
GetArtist
ListTracksByArtist
GetAlbum
GetTrack
RequestDiscovery
```

Avoid infrastructure-focused names in feature code.

## Testing

Outside-in sociable TDD.

Required test categories:

- API feature tests
- discovery orchestration tests
- discovery execution tests
- Projection tests
- aggregate tests

Key scenarios:

- local search
- playback filtering
- discovery request creation
- planner budget enforcement
- provider lookup failures
- hierarchy validation
- import event replay
- artwork projection updates

## Acceptance Criteria

- Search supports artist, album, and track results.
- Playback filtering is supported.
- Tracks can be listed across all albums for an artist.
- Discovery is asynchronous.
- API never calls external metadata providers.
- Discovery orchestration owns retry estimates.
- Event store is source of truth.
- RavenDB contains projections only.
- MusicBrainz dump import emits events.
- Provider failures are tracked per provider.
- Artwork can be added without redesigning existing entities.
- Discovery lifecycle state is derived from discovery event streams only.
- Catalog browse state is derived from catalog event streams only.

## Implementation Constraints for Codex

This section contains implementation decisions intended to reduce ambiguity for automated implementation.

### Platform

Use:

- .NET 8 or later
- ASP.NET Core Minimal APIs
- RavenDB native client
- Azure Service Bus for inter-component messaging
- Wolverine only where useful for outboxing or message handling
- xUnit or NUnit for tests
- Shouldly or FluentAssertions for assertions

Do not introduce:

- Redis
- SQL Server
- Entity Framework
- MediatR unless already present in the repository
- in-process caches as a correctness dependency

### Solution Shape

Suggested projects:

```text
src/
  MusicCatalog.Api/
  MusicCatalog.Domain/
  MusicCatalog.Contracts/
  MusicCatalog.Catalog.Projector/
  MusicCatalog.Discovery.Orchestration/
  MusicCatalog.Discovery.Execution/
  MusicCatalog.IntegrationEvents.Cdc/

tests/
  MusicCatalog.Api.Tests/
  MusicCatalog.Discovery.Orchestration.Tests/
  MusicCatalog.Discovery.Execution.Tests/
  MusicCatalog.Projection.Tests/
```

### Namespace Conventions

Use namespaces matching the project and feature folder.

Examples:

```csharp
namespace MusicCatalog.Api.Features.Search;
namespace MusicCatalog.Api.Features.Artists;
namespace MusicCatalog.Domain.Discovery;
namespace MusicCatalog.Projections.Artists;
namespace MusicCatalog.Discovery.Orchestration.Features.RequestDiscovery;
namespace MusicCatalog.Discovery.Execution.Features.LookupMetadata;
```

### Transaction and Persistence Guidance

Avoid handlers that combine:

- aggregate decision-making
- projection updates
- snapshot writes
- discovery lifecycle mutation
- integration message publication

Those concerns should be separated.

Repositories should own domain-event persistence and mapping to storage DTOs.

Projectors should own read-model updates.

CDC should own integration publication.

If a handler appears to perform many writes across multiple concerns, that is usually a sign that aggregate, projector, or CDC boundaries are being bypassed.

### Feature Folder Rules

Feature code must be organised by business capability.

Good:

```text
Features/Search/SearchCatalog.cs
Features/Search/SearchCatalogEndpoint.cs
Features/Artists/GetArtist.cs
Features/Artists/ListTracksByArtist.cs
```

Avoid infrastructure-first feature names:

```text
RavenDbSearchHandler.cs
ServiceBusDiscoveryPublisher.cs
MusicBrainzHttpClientHandler.cs
```

Infrastructure code belongs in infrastructure projects behind business-focused interfaces.

### Minimal API Endpoint Style

Use one endpoint class per route.

Example:

```csharp
public static class SearchCatalogEndpoint
{
    public static IEndpointRouteBuilder MapSearchCatalog(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/search", Search);
        return routes;
    }

    private static async Task<IResult> Search(
        [AsParameters] SearchCatalogRequest request,
        SearchCatalog searchCatalog,
        CancellationToken cancellationToken)
    {
        var response = await searchCatalog.Handle(request, cancellationToken);
        return Results.Ok(response);
    }
}
```

### Business Use Case Style

Use business-focused use case classes.

```csharp
public sealed class SearchCatalog
{
    public Task<SearchCatalogResponse> Handle(
        SearchCatalogRequest request,
        CancellationToken cancellationToken)
    {
        // query RavenDB projection
        // append DiscoveryRequested if needed
        // include discovery status from projection if available
    }
}
```

### RavenDB Document ID Conventions

Use deterministic document prefixes:

```text
artists/{artistId}
albums/{albumId}
tracks/{trackId}
discovery-status/{queryKeyHash}
source-budgets/{source}
```

Do not expose RavenDB document IDs directly if they contain internal prefixes.

Public API IDs should remain stable logical IDs.

### Event Naming Conventions

Events must be past-tense facts.

Good:

```text
ArtistDiscovered
TrackDiscovered
ProviderReferenceLookupFailed
DiscoveryPlanned
MetadataCorrected
```

Avoid command-style event names:

```text
DiscoverArtist
LookupProviderReference
CorrectMetadata
```

### Aggregate Boundaries

Use aggregates to protect business invariants, not to mirror read models.

Suggested aggregates:

```text
ArtistCatalogAggregate
DiscoveryRequestAggregate
SourceBudgetAggregate
```

Discovery lifecycle and catalog facts are separate concerns and must not share a stream.

`DiscoveryRequestAggregate` is keyed by normalized discovery/search criteria and owns only discovery lifecycle state.

`ArtistCatalogAggregate` is the target catalog write model boundary. It owns artist, album, and track facts within a single artist catalog and protects hierarchy and correction invariants across that boundary.

Do not choose aggregate boundaries to mirror RavenDB read models.

The previous guidance to avoid one large aggregate should be interpreted as a warning about hot paths, contention, and oversized consistency boundaries, not as a ban on artist-root aggregates. One aggregate per artist catalog is acceptable when the business invariants are artist-centered.

Compilation albums, collaborations, and future artist-membership relationships must be handled explicitly. In MVP, collaboration albums may be represented as separate artists. Later revisions may model artist membership or artist-to-artist relationships without changing the separation between discovery streams and catalog streams.

The event stream should allow replay into RavenDB projections without calling external APIs.

Catalog projections for artist, album, and track reads must be fully rebuildable from persisted catalog domain events alone.

Discovery status projections must be fully rebuildable from persisted discovery domain events alone.

### Stream Identity Guidance

Discovery stream identity:

```text
one stream per normalized discovery/search criteria
```

Catalog stream identity target:

```text
one stream per artist catalog
```

Track- and album-shaped read models remain projections. They are not independent write-model aggregates by default.

Where a track is discovered before a stable artist identity is available, the system may use a provisional artist-catalog identity strategy, provided that:

- the strategy is deterministic
- later correction or reconciliation is possible through domain events
- replay remains possible without external lookups

### Event Append Rules

API may append:

```text
DiscoveryRequested
```

Discovery orchestration may append:

```text
DiscoveryPlanned
DiscoveryDeferred
DiscoveryRejected
DiscoveryStarted
DiscoveryCompleted
DiscoveryFailed
```

Discovery execution may append no discovery lifecycle events directly. It returns execution outcomes to orchestration.

Catalog write flows may append:

```text
ArtistDiscovered
AlbumDiscovered
TrackDiscovered
ProviderReferenceDiscovered
ProviderReferenceLookupFailed
ArtworkDiscovered
MetadataCorrected
```

Manual/admin repair flows may append:

```text
MetadataCorrected
```

Import tool may append discovery events directly and must bypass planner and worker.

Import tool may also append catalog domain events directly when rebuilding or importing catalog history.

### Azure Service Bus Naming

Suggested queue/topic names:

```text
music-catalog.discovery-requests
music-catalog.discovery-jobs
music-catalog.discovery-results
music-catalog.integration-events
```

Message names should match business intent:

```text
PlannedDiscoveryJob
DiscoveryCompletedNotification
ProjectionRequired
```

### Outbox and CDC

The aggregate root must not know about integration events.

CDC is responsible for translating persisted domain events into integration events.

If Wolverine is used, keep it at the boundary:

```text
Event Store -> CDC -> Wolverine Outbox -> Azure Service Bus
```

Do not publish Azure Service Bus messages directly from aggregates.

Application handlers should prefer:

```text
load aggregate -> invoke domain method -> save aggregate
```

Handlers and listeners must not construct long lists of domain events inline when that logic belongs in an aggregate.

Discovery status documents, search status documents, and catalog browse documents are projection-only. They are never the source of truth.

### Source API Budget Safety

Source API budget enforcement is a correctness requirement.

The budget must be safe across multiple planner and worker instances.

Ordinary RavenDB document writes with default optimistic concurrency are not sufficient for hard global budget enforcement because concurrent updates can lose increments or overspend a token budget.

Use one of these approaches:

#### Recommended v1: RavenDB Compare-Exchange Budget Tokens

Use RavenDB compare-exchange entries for source-budget coordination.

Compare-exchange gives cluster-wide atomic key-value updates and is suitable for distributed coordination.

Budget key examples:

```text
source-budget/musicbrainz/2026-06-14T12:00:00Z
source-budget/odesli/2026-06-14T12:00:00Z
```

Each source budget window stores:

```json
{
  "source": "MusicBrainz",
  "windowStartedAt": "2026-06-14T12:00:00Z",
  "windowEndsAt": "2026-06-14T12:01:00Z",
  "maxRequests": 60,
  "reservedRequests": 12,
  "safetyMarginPercent": 10
}
```

Reservation algorithm:

```text
1. Load compare-exchange value for current source/window.
2. If missing, create it with reservedRequests = 0.
3. Calculate safeMax = maxRequests - safetyMargin.
4. If reservedRequests >= safeMax, reject reservation.
5. Attempt compare-exchange update with reservedRequests + requestedAmount.
6. If compare-exchange fails, retry with jitter.
7. Worker may call source API only after reservation succeeds.
```

The planner may use the same reservation path before scheduling work, or the worker may reserve immediately before execution.

For MusicBrainz, use both:

```text
- fixed-window budget
- minimum spacing of one request per second
```

This avoids minute-level compliance while accidentally bursting several requests in the same second.

#### Alternative: Single Budget Owner Queue

Route all source API calls for a given source through a single logical budget owner.

Pros:

- simple to reason about
- hard to overspend
- no distributed token contention

Cons:

- lower throughput
- one bottleneck per source
- more queue choreography

This is acceptable for MusicBrainz because the public limit is low.

#### Alternative: Redis Distributed Bucket

Redis is not required for v1.

Pros:

- common distributed rate-limit pattern
- high throughput
- good expiry primitives

Cons:

- extra infrastructure
- another operational dependency
- unnecessary if RavenDB compare-exchange is already available

Use Redis only if later throughput or operational requirements justify it.

### Budget Consistency Decision

For v1, use RavenDB compare-exchange or a single budget-owner queue.

Do not implement source API budgeting as ordinary RavenDB documents only.

Do not rely on eventual projection consistency to enforce budgets.

RavenDB projections may expose budget state for observability, but they must not be the enforcement mechanism.

### Idempotency Requirements

All message handlers must be idempotent.

Use deterministic keys for:

```text
DiscoveryRequested
PlannedDiscoveryJob
ProviderReferenceDiscovered
ProviderReferenceLookupFailed
ArtistDiscovered
AlbumDiscovered
TrackDiscovered
```

Examples:

```text
provider-reference/{entityType}/{entityId}/{provider}
lookup-failure/{entityType}/{entityId}/{provider}
musicbrainz-artist/{mbid}
musicbrainz-release/{mbid}
musicbrainz-recording/{mbid}
```

### Discovery Query Key Normalisation

Query keys must be deterministic.

Rules:

- trim whitespace
- lowercase invariant culture
- collapse repeated spaces
- remove punctuation that does not change search intent
- include search type and playback filters
- hash if too long for document IDs

Example:

```text
search:track:karma police:playback=spotify,applemusic
```

### Provider Availability Indexing

Playback availability must be indexed in RavenDB.

Search should not load every candidate document to determine provider availability.

Index fields:

```csharp
public string[] PlayableProviders { get; init; } = [];
public string[] TerminallyUnavailableProviders { get; init; } = [];
public string[] MissingProviders { get; init; } = [];
```

### Error Handling

External lookup failures must be classified as:

```text
transient
terminal
budget-deferred
invalid-request
not-found
```

Terminal provider failures are per provider.

A failed YouTube Music lookup must not prevent Spotify or Apple Music from being playable.

### Observability

Log at minimum:

- discovery requested
- discovery planned
- discovery deferred
- source budget reservation accepted/rejected
- external lookup started/completed/failed
- projection applied
- duplicate event ignored

Metrics should include:

- source API calls per source/window
- budget reservation failures
- discovery queue age
- worker failure counts
- terminal provider lookup failures
- search latency
- RavenDB query duration

### Testing Constraints

Prefer sociable tests over heavy mocking.

API tests should exercise:

- real routing
- real JSON serialisation
- RavenDB test database or test container
- fake event store
- fake discovery status projection

Planner tests should use:

- real priority logic
- fake clock
- fake budget store or RavenDB compare-exchange abstraction
- fake queue

Worker tests should use:

- fake MusicBrainz adapter
- fake Odesli adapter
- fake budget reservation service
- fake event writer

Projection tests should replay events into RavenDB and assert documents and indexes.

### Definition of Done

A Codex implementation should be considered incomplete unless it includes:

- endpoint implementations
- request/response DTOs
- RavenDB documents
- RavenDB indexes
- event definitions
- discovery status projection
- source budget abstraction
- compare-exchange or single-owner budget implementation
- Azure Service Bus message contracts
- feature tests for each route
- planner budget tests
- provider failure tests
- projection replay tests
