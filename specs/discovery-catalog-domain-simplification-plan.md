# Discovery And Catalog Domain Simplification Plan

## Purpose

This document proposes a concrete simplification plan for the discovery and catalog model in `Soundtrail.Services`.

It is intended to align the current implementation with the business direction already present in the existing specifications:

- `specs/music-catalog-discovery-api-v2.md`
- `specs/discovery-prioritisation-and-worker-admission-layering.md`
- `specs/event-sourcing-spec-ravendb-production.md`

This is not a low-level refactor task list only. It is a domain correction plan. The goal is to move the codebase toward a model where:

- catalog streams contain catalog facts
- discovery streams contain discovery request and lookup-journey facts
- execution owns provider admission and third-party calls
- integration events are emitted from domain facts, not stored as domain facts

This plan deliberately preserves the project’s current naming where that naming better reflects the business language than older specs. In particular:

- `StreamingLocations` remains preferred project terminology over `ProviderReferences` when the concept is specifically about playable provider locations
- existing project conventions should be treated as the baseline unless there is a clear domain reason to change them

The aim is to correct event ownership and responsibility boundaries without flattening the language that already fits the business well.

## Terminology

This plan uses the following terminology intentionally and explicitly.

### Streaming Locations

`StreamingLocations` is the preferred business term for playable provider locations for a music entity.

This is the term the redesign should use in domain modeling, specifications, and new code where the concept is about playable locations on streaming providers.

This plan does not treat older `ProviderReference` wording as the target vocabulary for that concept.

### Provider References

`ProviderReference` should only be used where the concept is genuinely a generic provider reference rather than a playable streaming location.

If the business concept is specifically about whether a user can play a track through a supported provider, `StreamingLocations` is the correct term.

### Availability

Provider availability and terminal unavailability are catalog facts about an entity's known playable state.

They are separate from:

- discovery lifecycle
- orchestration decisions
- transport messages

## Why This Plan Exists

Recent investigation of `SearchCatalogRequestedHandler`, `SearchOrSeekHistory`, `MusicTrack`, and the related projector/orchestrator code shows that the current implementation has drifted away from the intended design in the specs.

The main problems are:

1. The API bug means every request can generate work, whether or not work is actually justified.
2. Discovery streams are mixing legitimate discovery facts with downstream work instructions and integration-shaped events.
3. Catalog streams are leaking workflow semantics such as `StreamingLocationsRequired`.
4. Artist and album lookup concepts are modeled as if they are first-class discovery workflows, but the current business direction says artist/album metadata comes from music lookup side effects rather than dedicated MusicBrainz orchestration.
5. Assessment and planning are track-oriented in practice, while the event model implies broader artist/album workflow support.
6. Integration-style events are leaking into event streams that should contain domain history.

The result is a model where event sourcing is doing double duty as:

- domain history
- work queue
- orchestration state
- integration boundary

That makes the code hard to reason about and weakens the architectural guarantees already stated in the specs.

## Hard Constraints

Nothing in this plan should be interpreted as permission to weaken the current architectural conventions.

These constraints are hard.

They are not guidance, preferences, or aspirations.

They are implementation requirements.

Any implementation step that violates them is wrong, even if it appears to make local progress or preserves temporary backward compatibility.

The redesign must preserve all of the following:

- append-only replayable event streams
- the ability to replay streams to rebuild projections and recover state
- aggregates as the place where domain rules and event emission live
- the translation layer between domain events and persisted/message DTOs
- strict infrastructure/domain separation
- the ability for handlers, projectors, and subscribers to react to the same domain facts in multiple ways

The redesign must also preserve a single clear consistency boundary per incoming message.

It is not acceptable for one handler, one feature, or one "attempted/applied" chain to imperatively update multiple authoritative streams or models as part of handling a single incoming message unless that write boundary is truly atomic within the target design.

For the avoidance of doubt, this is a hard rule, not a style preference:

- no handler-to-handler orchestration that hides multiple authoritative writes behind ordinary handler calls
- no split-write flow where one incoming message updates catalog and discovery separately as peer authoritative boundaries
- no acceptance of "retry will probably fix it" as justification for a broken consistency boundary
- no landing of an implementation step that depends on all intermediate writes being perfectly idempotent across every failure mode in order to remain coherent

If a design would allow one durable write to succeed and a second durable write to fail or retry later, leaving the system in a subtle partially-applied state, that design is non-compliant with this plan.

Where more than one downstream reaction is needed, the correct shape is:

- one authoritative fact recorded at the correct boundary
- downstream projections, subscriptions, or follow-on handlers reacting from that fact

The authoritative write must not be obscured behind indirection that makes a multi-boundary consistency problem look like a normal in-process call chain.

This plan is intended to reduce race-prone accidental coupling, not to forbid multiple handlers or multiple projections from responding to the same fact. If a single fact legitimately drives multiple downstream reactions, that remains acceptable. The constraint is that those reactions must remain consistent with event-sourced, replay-safe design rather than collapsing back into hidden orchestration inside aggregates.

Where there is uncertainty about whether something belongs in the domain model or the orchestration model, the default decision should be:

- build the domain concept first
- introduce orchestration only when the domain fact alone is insufficient

In other words, confusion should be resolved in favor of stronger domain modeling, not by prematurely pushing meaning into orchestration.

## Implementation Gating Rule

This plan must be executed with hard implementation gates.

If an implementation step cannot satisfy the full target flow end to end, stop and report that instead of landing a partial architectural rewrite.

For this plan, "full target flow end to end" means the relevant flow is coherent through all of the following:

- request or triggering fact entry
- domain fact recording in the correct stream
- assessment and/or planning in the correct model
- dispatch to the correct worker or downstream command path
- worker outcome handling
- application of resulting catalog or discovery facts
- projection/read-model continuity
- replay-safe behavior consistent with the target model

It is not acceptable to land a slice that only renames concepts, partially moves ownership, or introduces new aggregates/handlers/flows without the rest of the flow being complete.

It is not acceptable to preserve the old orchestration shape under new names simply because that makes the refactor easier to stage.

If a full target flow cannot be completed safely in the current step, the correct action is to stop, report the gap clearly, and choose a different implementation slice rather than land an incoherent intermediate architecture.

The same rule applies to consistency boundaries.

If a proposed implementation step requires a temporary split-write, temporary handler-to-handler orchestration, or temporary dual-authority update in order to "get closer" to the target model, that step must not be landed.

The correct action is to stop and report that the slice cannot yet satisfy the hard consistency constraints end to end.

## Business Direction From Existing Specs

The existing specs imply the following target direction.

### 1. The API is local-first

The API should:

- return local RavenDB-backed results immediately
- append discovery intent when local data is incomplete
- never block on external calls

This means the API should record discovery intent, not invent arbitrary downstream work in-process.

### 2. Discovery is asynchronous and lifecycle-based

The specs define discovery as a lifecycle:

- `DiscoveryRequested`
- `DiscoveryPlanned`
- `DiscoveryDeferred`
- `DiscoveryRejected`
- `DiscoveryFailed`
- `DiscoveryStarted`
- `DiscoveryCompleted`

This points to discovery as a request-resolution/orchestration domain, not a bag of track lookup commands.

### 3. Catalog is fact-based

The catalog side is supposed to contain music facts such as:

- `ArtistDiscovered`
- `AlbumDiscovered`
- `TrackDiscovered`
- `StreamingLocationsDiscovered`
- `StreamingLocationsLookupFailed`
- `ArtworkDiscovered`
- `MetadataCorrected`

Older specs may say `ProviderReferenceDiscovered` or related terms. This plan does not adopt that wording as the target language for playable provider locations. The intended direction is to standardize on `StreamingLocations` for that concept.

This points to catalog streams as sources of truth for music entities, not planners for future work.

### 4. Orchestrator and worker concerns must stay separate

The layering spec explicitly separates:

- prioritisation and lifecycle orchestration
- worker-edge admission and provider execution

This means work planning belongs to orchestration, while budget gating and external calls belong to execution.

### 5. Projections must be rebuildable from clear event meanings

The event sourcing spec requires aggregates to stay pure and projections to be derived from domain events. This becomes much harder when event streams hold mixed domain facts and orchestration instructions.

## Current State Assessment

### Discovery Stream Problems

The current discovery aggregate `SearchOrSeekHistory` is overloaded.

It currently mixes:

- search request lifecycle
- seek request lifecycle
- known track requests
- metadata lookup requests
- album/artist lookup requests
- catalog track streaming location requests

This means a stream keyed by `DiscoveryQueryKey` is carrying facts about:

- what was requested
- whether the request should be looked up
- what catalog entity should be processed later
- what downstream subsystem should do next

That is too many responsibilities for a single stream.

### Catalog Stream Problems

The current `MusicTrack` aggregate is mostly fact-based, but it also emits `StreamingLocationsRequired`.

`StreamingLocationsRequired` is not a catalog fact in the same sense as `TrackDiscovered` or `ProviderReferenceDiscovered`. It is a work instruction or orchestration signal.

That means the track stream is not just describing what is true about the track. It is also describing what the system wants to do next.

### Workflow Support Is Broader Than Real Capability

The current event model implies first-class support for:

- track lookup orchestration
- artist lookup orchestration
- album lookup orchestration

But the current business direction says:

- artist and album metadata lookup are not separate first-class MusicBrainz workflows
- artist and album data arrive as side effects of looking up music

That means the current model overstates domain complexity and creates fake workflow surfaces that policy and assessment do not actually treat consistently.

### Integration Leakage

The code currently leaks integration-shaped semantics into internal event streams. The specs clearly want:

- domain events persisted in streams
- integration events emitted by CDC / outboxing / boundary translation

This boundary needs to be restored.

## Target Domain Model

The target model should distinguish three concerns clearly.

### A. Discovery Domain

Keyed by:

- `DiscoveryQueryKey`

Owns:

- the lifecycle of a user discovery request or known-item request
- request acceptance
- resolved identity as it becomes known through the request journey
- planning, fan-out, and deferral facts about the request journey
- resolution outcomes
- the state exposed through discovery status projections

Should not own:

- transport-level message names
- execution-edge admission data
- provider-specific worker protocol details
- catalog facts

### B. Catalog Domain

Keyed by a catalog entity identity.

Longer-term target:

- artist-keyed aggregate if the business wants artist-centric ownership of albums and tracks

Shorter-term acceptable state:

- existing track-keyed catalog aggregate, but fact-only

Owns:

- artist facts
- album facts
- track facts
- streaming locations / provider location facts
- artwork
- metadata corrections

Should not own:

- orchestration or planning signals
- worker admission concerns
- integration DTO semantics

### C. Execution / Integration Boundary

Execution and messaging remain separate from the domain model.

Owns:

- dispatching worker commands
- provider admission
- third-party calls
- transport concerns
- integration event emission

Should not masquerade as:

- catalog facts
- discovery facts

## Proposed Stream Ownership

### 1. Discovery Stream

The discovery stream should contain request and lookup-journey facts.

Recommended event set:

- `DiscoveryRequested`
- `DiscoveryPlanned`
- `DiscoveryDeferred`
- `DiscoveryRejected`
- `DiscoveryStarted`
- `DiscoveryCompleted`
- `DiscoveryFailed`
- `DiscoveryResolvedToKnownCatalogItem`
- `DiscoveryResolvedAsNotFound`
- `DiscoveryResolvedAsAmbiguous`
- `CatalogSearchCandidateRecorded`
- explicit domain facts recording what the request has fanned out to next

These are facts about the request journey.

They are not permission to store:

- transport message shapes
- worker admission outcomes
- command DTO semantics
- low-level retry mechanics that exist only to drive infrastructure
- provider-specific execution protocol chatter

### 2. Catalog Stream

The catalog stream should contain only catalog facts.

Recommended event set:

- `ArtistDiscovered`
- `AlbumDiscovered`
- `TrackDiscovered`
- `StreamingLocationsDiscovered`
- `StreamingLocationsLookupFailed`
- `ArtworkDiscovered`
- `MetadataCorrected`

If the stream remains track-keyed temporarily, these facts may still be emitted from the track aggregate, but they must stay facts rather than orchestration instructions.

### 3. Execution Boundary

The system will still need handlers and ports that translate discovery facts into worker commands and translate worker outcomes back into domain facts.

Those handlers are part of the execution and integration boundary, not a separate domain event stream by default.

If a future work model gains a strong stable business identity and real domain state, it may be promoted into its own domain model later. This plan does not assume that up front.

## Event Reclassification

The following current events should be reclassified.

### Keep In Discovery

- `DiscoveryRequested`
- `DiscoveryPlanned`
- `DiscoveryDeferred`
- `DiscoveryRejected`
- `DiscoveryStarted`
- `DiscoveryCompleted`
- `DiscoveryFailed`
- `CatalogSearchCandidateRecorded`
- explicit resolved-identity facts
- explicit fan-out facts that describe what the request has been expanded to

### Remove From Discovery Streams

- `TrackMetadataLookupRequested`
- `KnownTrackRequested`
- `ArtistCatalogLookupRequested`
- `AlbumCatalogLookupRequested`
- `MusicTrackSearchStarted`

Reason:

These are not domain facts about the request journey. They are either work requests, alternate request types leaking through the wrong abstraction, or integration/orchestration-shaped signals.

### Remove From Catalog Streams

- `StreamingLocationsRequired`

Reason:

This is a planning instruction, not a stable catalog fact.

### Keep Out Of Domain Streams Entirely

- integration DTO semantics
- transport-level message names
- execution-edge admission outcomes unless intentionally translated into domain facts
- provider-specific worker protocol chatter
- orchestration records whose only meaning is "send this command next"

## Business Rules To Preserve

Any redesign must preserve these business truths from the specs.

### Local-First Search

- API returns known results immediately.
- API appends discovery intent when local information is incomplete.
- API does not block for provider calls.

### Asynchronous Discovery

- discovery lifecycle remains visible in RavenDB-backed projections
- discovery owns request-level planning and deferral facts
- worker owns execution and provider admission

### Provider Semantics

- MusicBrainz is the canonical metadata source
- Odesli / Songlink is the provider playback reference source
- provider failures are tracked per provider

### Hierarchy Rules

- track IDs are globally unique
- artist/album hierarchy must remain consistent in browse APIs
- album names alone are not unique

### Event-Sourcing Guarantees

- event store remains source of truth
- projections are rebuildable
- aggregates remain persistence-ignorant
- translation boundaries remain explicit
- replay remains a first-class supported operation throughout the redesign
- request journey facts must remain expressible as domain facts rather than hidden in handlers

### Architectural Conventions

- aggregates remain the primary domain boundary
- domain code does not absorb infrastructure concerns for convenience
- repository and translation layers remain responsible for DTO/event-store mapping
- handlers may continue to perform multiple downstream reactions to the same fact where appropriate
- no part of this redesign should rely on bypassing existing infrastructure/domain conventions to appear simpler
- discovery aggregates may store what has been planned or fanned out to when those are domain facts about the request journey, but they must not store integration events or low-level execution protocol detail

## Implementation Strategy

The safest path is an incremental multi-phase refactor.

## Phase 0: Define Vocabulary And Boundaries

### Objective

Create a shared target language before changing behavior.

### Work

- Document the three bounded concerns:
  - discovery request and lookup journey
  - catalog facts
  - execution/integration boundary
- Document the preferred business language for this codebase, including explicit retention of `StreamingLocations` terminology where it better fits the business than older spec wording.
- Agree on whether discovery should cover both search and known-item requests, or whether seek requests should become a separate concept.
- Decide whether the long-term catalog aggregate should be artist-keyed or whether that is a later evolution after workflow cleanup.
- Define what counts as a discovery fact, a catalog fact, and an integration/execution concern.
- Define the tie-break rule that ambiguous concepts are modeled in the domain first, not orchestration first.

### Deliverables

- glossary added to spec set
- updated architecture diagram
- event classification table

### Exit Criteria

- team can explain where a new event belongs without ambiguity

## Phase 1: Stop Generating Work By Accident

### Objective

Fix the behavioral bug where every request generates work whether it should or not.

### Work

- Trace all request entry points from API and message listeners.
- Identify where discovery intent automatically turns into work items without a domain decision.
- Introduce a single explicit decision point where the system determines:
  - local data is already sufficient
  - discovery lifecycle should be recorded only
  - external work should be planned
- Ensure search requests that already have sufficient local results do not automatically create unnecessary work.

### Design Direction

Move from:

- "request received therefore create work"

To:

- "request received therefore record discovery intent"
- "planner later decides whether work is justified"

### Deliverables

- corrected request-to-discovery flow
- regression tests for the API bug

### Exit Criteria

- a request may record discovery intent without necessarily generating provider work

## Phase 2: Simplify Discovery Into A Pure Lifecycle Stream

### Objective

Make `DiscoveryQueryKey` streams about request lifecycle only.

### Work

- Refactor `SearchOrSeekHistory` so it no longer appends track/catalog work instructions.
- Remove responsibility for emitting `StreamingLocationsRequired`.
- Remove responsibility for emitting first-class artist/album lookup requests.
- Re-evaluate whether `KnownTrackRequested` belongs:
  - if known-item requests are just another way to initiate discovery, translate them into the same lifecycle model
  - if not, split known-item requests into their own stream/aggregate
- Replace overloaded boolean/status logic with clearer lifecycle transitions if needed.
- Consider introducing explicit resolution result events if `Reason` is currently absorbing too much meaning.

### Deliverables

- simplified discovery aggregate
- updated discovery event set
- discovery status projection updated to derive only from lifecycle events

### Exit Criteria

- a discovery stream can be read as a coherent story of one request’s lifecycle

## Phase 3: Move Work Instructions Out Of Domain Streams

### Objective

Create an explicit planning/orchestration model for external work.

### Work

- Introduce a dedicated planner output model such as:
  - `TrackMetadataLookupNeeded`
  - `ProviderReferenceLookupNeeded`
  - `LookupDeferred`
  - `LookupScheduled`
- Decide whether these are stored as:
  - orchestration events in a separate stream
  - durable planning records
  - or translated directly into commands after domain decisions
- Ensure planning is based on business state, not transport side effects.
- Ensure duplicate work suppression lives here rather than leaking into catalog/discovery facts.

### Notes

This phase is where the current `TrackMetadataLookupRequested`, `CatalogDiscoveryWork*`, and `StreamingLocationsRequired` semantics should converge into a smaller and more intentional planning model.

The redesign here must not remove replayability, translation boundaries, or aggregate-based ownership. The planning model should still be explicit, deterministic, and safe to rebuild or re-drive from persisted facts.

### Deliverables

- dedicated work planning abstraction
- stable command generation rules
- clearer orchestrator boundaries

### Exit Criteria

- no domain event stream needs to be interpreted as a work queue

## Phase 4: Make Catalog Streams Fact-Only

### Objective

Remove orchestration semantics from catalog aggregates.

### Work

- Remove `StreamingLocationsRequired` from catalog streams.
- Review all catalog event names and ensure they state facts, not intentions.
- Standardize on `StreamingLocations` terminology where the concept is specifically about playable provider locations.
- Keep provider lookup success/failure as catalog facts only when they describe observed state about a catalog entity.
- Preserve support for future artwork and metadata correction without reintroducing workflow instructions.

### Deliverables

- catalog aggregate emits fact-only events
- projectors consume fact-only catalog streams

### Exit Criteria

- reading a catalog stream tells you what became known about music entities, not what jobs the system wanted to run

## Phase 5: Align Artist/Album Semantics With Real Capability

### Objective

Remove fake first-class workflow support for artist/album lookup if the business does not actually want it.

### Work

- Audit all uses of `ArtistCatalogLookupRequested` and `AlbumCatalogLookupRequested`.
- Remove those event types if they are only placeholders for a workflow that does not exist.
- Replace them with side-effect-free catalog enrichment rules:
  - artist metadata may be discovered while looking up track metadata
  - album metadata may be discovered while looking up track metadata
- Update planner policies so they operate on the work types the system actually supports.

### Deliverables

- reduced event surface area
- assessment logic matches actual workflow support

### Exit Criteria

- there is no first-class orchestration path for artist/album lookup unless the business truly wants one

## Phase 6: Restore Integration Boundaries

### Objective

Stop domain streams from leaking integration event semantics.

### Work

- Review all domain events that are currently translated too directly into boundary messages.
- Ensure domain events remain internal business facts.
- Move transport-specific concerns to CDC / outboxing / translation layers.
- Ensure internal naming does not depend on message DTO shape.
- Review all "requested/required" events and classify them as:
  - domain fact
  - orchestration fact
  - integration message

### Deliverables

- clearer CDC translation layer
- reduced coupling between event store and message bus contracts

### Exit Criteria

- domain event names and payloads make sense even if messaging technology changes

## Phase 7: Rework Projections And Tests Around The New Boundaries

### Objective

Make the read side reflect the simplified event model.

### Work

- Update discovery lifecycle projections to rely only on discovery lifecycle events.
- Update catalog browse/search projections to rely only on catalog facts.
- Remove projector logic that depends on work-planning events appearing in the wrong stream.
- Update outside-in tests so they verify:
  - local-first results
  - discovery status behavior
  - planning behavior
  - execution behavior
  - projection rebuildability

### Deliverables

- updated projection code
- revised outside-in and unit tests

### Exit Criteria

- projections can be rebuilt from event streams without hidden coupling to transport or planning hacks

## Candidate Target Shapes

## Option A: Minimal Change Architecture

Use this if the team wants the lowest-risk path.

### Characteristics

- keep existing track-keyed catalog aggregate
- simplify discovery stream aggressively
- move work planning into orchestration records/streams
- keep artist/album as facts discovered through track metadata flows

### Benefits

- lowest migration cost
- least disruptive to current projectors
- fixes the domain leakage without requiring artist-keyed aggregate redesign now

### Drawbacks

- catalog ownership remains track-centric
- longer-term artist-first browse concerns may remain awkward

## Option B: Business-True Intermediate Architecture

Use this if the team wants stronger domain alignment without a full catalog rewrite in one pass.

### Characteristics

- discovery becomes lifecycle-only
- orchestration becomes explicit
- catalog stays track-keyed initially but introduces stronger artist/album fact modeling
- later migration path to artist-keyed aggregate is documented but deferred

### Benefits

- good balance between correctness and delivery risk
- supports future artist-centric evolution

### Drawbacks

- still requires two-step migration

## Option C: Full Domain Realignment

Use this if the team wants to optimize for the end-state now.

### Characteristics

- discovery lifecycle-only streams
- explicit orchestration model
- catalog aggregate re-centered around artist ownership
- albums and tracks become nested facts under artist streams

### Benefits

- strongest conceptual integrity
- best long-term fit if browse/navigation is artist-led

### Drawbacks

- highest migration and replay cost
- more projector and import changes
- requires very careful event migration planning

## Recommended Path

Recommended path: Option B.

Reasoning:

- It addresses the most harmful current problem first: mixed event meanings.
- It aligns with the specs without forcing an immediate artist-keyed catalog migration.
- It creates a safer platform for later catalog aggregate redesign if the business still wants artist-centered ownership.

## Required Design Decisions Before Implementation

The team should explicitly answer these questions before coding begins.

1. Should known-item requests share the same discovery lifecycle stream as free-text search, or become a separate seek lifecycle stream?
2. Is the long-term catalog aggregate truly intended to be artist-keyed, or is that only a conceptual aid for event ownership?
3. Should work planning itself be event-sourced, or stored as durable planning records plus dispatched commands?
4. Do we need explicit resolution outcome events such as `Resolved`, `Ambiguous`, and `NotFound`, or can the lifecycle remain sufficient with richer projection logic?
5. Is any first-class artist/album external lookup still intended in future, or should those concepts be removed from orchestration now?

## Migration Considerations

### Event Compatibility

- Existing persisted streams may contain events that no longer belong in their target domain.
- The team must decide whether to:
  - keep historical events and adapt projectors
  - translate old events during replay
  - or version the stream interpretation

### Projection Rebuild

- Replay safety is mandatory.
- Projection rebuild rules must be documented before removing or replacing event meanings.

### Message Compatibility

- Existing listeners and message DTO contracts may assume current event names and routing.
- CDC translation must be updated carefully to avoid breaking message consumers unintentionally.
- Translation layers should absorb naming or routing evolution rather than pushing transport concerns back into the domain model.

### Incremental Delivery

- The system should continue serving local-first search throughout the migration.
- Discovery status responses must remain stable or be deliberately versioned.

## Testing Plan

### Aggregate Tests

- discovery lifecycle transition tests
- duplicate request suppression tests
- rejection/defer/complete semantics tests
- catalog fact emission tests
- replay/re-hydration tests proving the simplified model still rebuilds correctly

### Orchestration Tests

- work planned only when justified
- duplicate work suppressed
- no accidental work generation from API request handling
- artist/album side-effect handling remains correct

### Execution Tests

- worker admission remains idempotent
- provider budget unavailability maps to deferred outcomes
- duplicate command deliveries do not duplicate provider calls

### Projection Tests

- discovery status projection rebuilds from discovery lifecycle events only
- catalog browse/search projections rebuild from catalog facts only
- multiple handlers/subscribers can still react to the same fact without violating replay safety

### Outside-In Tests

- known local result returns immediately without unnecessary work
- incomplete result records discovery intent and returns status
- planned work occurs only when planner decides it should
- provider lookup failures update catalog availability correctly

## Suggested Work Breakdown

1. Write and agree the target event classification.
2. Fix the API bug that creates work too eagerly.
3. Simplify discovery streams to lifecycle-only semantics.
4. Introduce explicit orchestration/work-planning ownership.
5. Remove `StreamingLocationsRequired` from catalog streams.
6. remove artist/album fake workflow paths unless product direction changes.
7. update projectors and tests.
8. evaluate whether artist-keyed catalog ownership is still desired after the model is simplified.

## Definition Of Done

This plan is complete when the implementation satisfies all of the following:

- discovery streams describe request lifecycle only
- catalog streams describe catalog facts only
- work planning is explicit and separate
- API requests do not automatically become provider work
- artist/album workflow support matches actual business intent
- integration event concerns sit at the boundary, not inside domain streams
- RavenDB projections remain fully rebuildable from append-only domain history
- existing aggregate, translation-layer, and infrastructure/domain conventions remain intact
- any ambiguity resolved during redesign was settled by strengthening domain modeling first

## Final Recommendation

Do not start by renaming types.

Start by correcting event ownership.

The naming issues are real, but the deeper problem is that the code is currently expressing business facts, work intent, and integration flow in the same streams. If the team fixes ownership first, the right names will become much easier to see and much harder to get wrong.
