# Discovery Prioritisation And Worker Admission Layering

## Purpose

This document defines the implementation layering used to satisfy the wider music catalog discovery specification.

It exists to make two separate concerns explicit:

- discovery prioritisation and lifecycle orchestration
- final third-party admission control

The system must not treat these as the same problem.

## Problem Split

### 1. Prioritisation

Prioritisation answers questions such as:

- should this discovery attempt be considered at all
- should it be rejected, deferred, or attempted
- which item should be attempted before another item
- should we avoid planning duplicate or excessive in-flight work

This is long-lived, evented, and business-oriented.

### 2. Admission

Admission answers questions such as:

- can this worker call MusicBrainz right now
- can this worker call Odesli right now
- is there source budget available right now
- is another worker already processing this exact command

This is execution-edge, short-lived, and infrastructure-oriented.

## Layered Responsibility Model

### Orchestrator Responsibilities

The orchestrator owns:

- prioritisation
- deduplication of discovery intent
- coarse in-flight shaping
- business deferral and rejection
- discovery lifecycle event persistence
- dispatching worker commands when work should be attempted

The orchestrator does not own final hard provider budget admission.

### Worker Responsibilities

The worker owns:

- execution lease acquisition for an exact command
- final provider budget reservation
- external provider call execution
- budget commit or release
- returning deferred outcomes when provider admission fails

The worker is the nearest safe place to consume scarce provider budget.

## Why The Split Exists

If provider budget is reserved too early:

- queue delay wastes budget
- retries can hold scarce admission too long
- the system spends budget on work that may never execute

If provider budget is checked only at the worker edge:

- budget is consumed nearest to the real third-party call
- the system makes better use of scarce provider capacity
- discovery planning remains focused on business competition between items

## Event Meanings Under This Layering

### DiscoveryRequested

A discovery attempt for a search criteria was recorded.

### DiscoveryRejected

The orchestrator determined the work should not be attempted.

Examples:

- ambiguous resolution
- not found
- blocked risk/trust policy

### DiscoveryDeferred

The work remains valid but should not continue now.

Examples:

- lower priority than competing work
- coarse in-flight shaping limit reached
- insufficient local information
- worker-edge provider budget unavailable

### DiscoveryPlanned

The orchestrator selected the work to be attempted and dispatched it toward execution.

`DiscoveryPlanned` does not mean provider budget is already reserved.

### DiscoveryStarted

The worker successfully acquired execution admission and began the third-party lookup attempt.

### DiscoveryCompleted

The discovery lifecycle finished successfully for the criteria.

### DiscoveryFailed

The discovery attempt failed without defer semantics.

## Invariants

The design must preserve these invariants:

- the orchestrator must never overspend third-party provider budget
- the worker must never execute the same command concurrently twice
- the worker must use stable command identity for replay-safe admission
- provider budget reservation must be idempotent by command identity
- provider budget unavailability must become a deferred outcome, not a silent drop
- discovery lifecycle state must remain projection-rebuildable from persisted events

## Budget Protection Model

### Orchestrator Layer

The orchestrator protects the system from bad planning, not from final provider overspend.

It may still apply:

- coarse in-flight limits
- priority ordering
- duplicate intent suppression
- defer/reject decisions

It must not be the final hard provider budget gate.

### Worker Layer

The worker is the final hard provider budget gate.

The worker must:

1. acquire execution exclusivity for the exact command
2. reserve provider budget for the exact command
3. call the external provider only if reservation succeeded
4. commit or release the reservation according to execution outcome

This admission control must be replay-safe.

## Recommended Worker Admission Shape

Worker-edge admission should be keyed by stable command identity, not random request identity.

Suggested concepts:

- execution lease keyed by `CommandId`
- provider budget reservation keyed by `CommandId`
- reserve / commit / release semantics

Suggested result shapes:

- accepted
- deferred because budget unavailable
- duplicate because command is already executing

## Execution Lease

An execution lease protects against duplicate live execution of the same command.

The lease:

- is short-lived
- is keyed by stable `CommandId`
- may expire automatically
- must be reacquirable after crash or timeout

This lease is different from provider budget reservation.

## Reservation Lifecycle

The provider reservation lifecycle should be:

- `reserved`
- `committed`
- `released`

Rules:

- `reserve` must be idempotent for the same command
- `commit` must be idempotent
- `release` must only refund an uncommitted reservation
- replay must not consume provider budget twice for the same command

## Replay Semantics

Wolverine handlers may be replayed or redelivered.

Therefore:

- worker admission must be idempotent by `CommandId`
- duplicate worker delivery must not cause duplicate provider budget consumption
- duplicate worker delivery must not cause concurrent duplicate execution
- orchestrator lifecycle transitions must remain append-only and replay-safe

## Crash Semantics

The system must be able to recover if a worker terminates after:

- acquiring an execution lease
- reserving budget
- beginning but not completing a provider call

This requires explicit reservation and lease semantics.

The recovery policy must define:

- when a lease is considered stale
- whether a reserved but uncommitted budget claim is released
- when a provider attempt is considered to have consumed budget

## Relationship To Storage Choices

This layering does not require a single storage technology for all concerns.

Expected long-lived ownership:

- discovery lifecycle events: RavenDB event store
- catalog events and projections: RavenDB event store and projections
- worker-edge provider admission: separate execution-edge store or service as needed

A Redis-backed admission layer is a natural fit when the concern is:

- short-lived
- token-bucket-like
- lease-oriented
- execution-adjacent

A relational workflow store may still be appropriate later if admission evolves into a broader durable workflow subsystem.

## Meaning Of Success

This layering is successful when:

- prioritisation remains in the orchestrator
- provider admission happens at the worker edge
- third-party providers cannot be overspent under replay or concurrency
- the discovery event model remains clear and rebuildable
- competing discovery items can still be prioritized against one another centrally
