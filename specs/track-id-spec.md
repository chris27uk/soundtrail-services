# Track ID Specification

## Purpose

This document defines what a `TrackId` is in `Soundtrail.Services`.

It records the identity decisions made for catalog tracks so that future work, including lookup completion, playlist ingestion, and catalog persistence, can build from a shared model.

This spec is intentionally narrow. It does not define the full catalog model. It defines:

- the data items that make up a `TrackId`
- the relationship between `TrackId` and a broader base identity
- validation limits for canonical music metadata
- the expected behaviour when metadata is invalid or incomplete

## Why This Spec Exists

The project needs a track identity model that supports all of the following:

- exact storage of release-specific track detail
- grouping rereleases of the same underlying track
- local-first playlist handling
- future support for Apple Music, Spotify, YouTube Music, and BBC music
- simple orchestration in `OnLookupCompleted`

The project explicitly does not want to depend on:

- provider track ids
- MusicBrainz release ids
- iTunes product ids
- transport-shaped identity models

The identity model must remain provider-agnostic and domain-led.

## Identity Model

### TrackId

`TrackId` is one exact identity value.

It is not an opaque random identifier.

It must carry enough structure to support both:

- exact identity equality
- mathematical family and range selection

The required logical parts of `TrackId` are:

- `BaseComponent`
- `VectorComponent`

`TrackId` is therefore:

```text
TrackId = Join(BaseComponent, VectorComponent)
```

Where:

- `BaseComponent` identifies the song-family space
- `VectorComponent` identifies the exact position within that space

Short justification:

- most catalog uses need one exact identity
- some lookup scenarios need mathematical range and nearest-selection semantics
- one structured value is preferable to many persisted helper fields on every projection

### BaseComponent

`BaseComponent` represents the broad song-family space.

It must be derived from:

- `CanonicalArtistName`
- `CanonicalTrackName`
- optional `CanonicalAlbumName`

It must exclude:

- `CanonicalReleaseDate`
- `CanonicalReleaseType`

Required property:

- all exact track identities that belong to the same song-family must share the same `BaseComponent`

Short justification:

- playlists and other low-specificity sources often know the song family before they know the exact release variant

### VectorComponent

`VectorComponent` represents the exact variant position within a known `BaseComponent`.

It must be deterministic.

It may include release-specific or version-specific information such as:

- release date
- release type
- remake/remaster criteria
- other future range-selection dimensions

Required properties:

- the same canonical inputs always produce the same vector
- two exact variants under the same base family may differ by vector
- the vector must support mathematical comparison within a base family

Short justification:

- the project needs to choose concrete siblings from low-specificity references
- ordering and distance inside one song-family should be principled rather than ad hoc

## Required And Optional Components

### Required

These are always required to construct a `TrackId` base family.

- `CanonicalArtistName`
- `CanonicalTrackName`

Short justification:

- a track without canonical artist and track title does not have enough stable music identity to be catalogued safely

### Optional

These increase specificity when present and may contribute to the vector.

- `CanonicalAlbumName`
- `CanonicalReleaseDate`
- `CanonicalReleaseType`

Short justification:

- these fields are valuable when present
- lookup sources, especially playlist data, may not always provide them reliably

## Canonical Metadata Value Limits

The catalog must not accept canonical track metadata beyond the system’s supported interoperability envelope.

The project should align this envelope to Apple’s documented maxima because Soundtrail aims to interoperate with Apple Music and should not accept metadata that one of its target ecosystems cannot support.

Current limits:

- `CanonicalTrackName`: maximum `250` characters
- `CanonicalAlbumName`: maximum `250` characters
- `CanonicalArtistName`: maximum `1000` characters

Reference:

- Apple `ContentAttributes` lists `name` max `250`, `albumName` max `250`, and `artistName` max `1000`

Source:

- [Apple ContentAttributes](https://developer.apple.com/documentation/sirikitcloudmedia/contentattributes?changes=_3__6)

Short justification:

- Soundtrail should not store canonical metadata it cannot reasonably interoperate with
- a documented external limit is safer than an invented local limit

## Validation Rule

Canonical identity generation must:

1. validate
2. canonicalize
3. derive identity

It must never:

1. truncate
2. canonicalize
3. derive identity

If a canonical metadata component exceeds the accepted limit, identity generation must fail.

Short justification:

- truncation introduces collision risk
- rejection preserves identity integrity

## Canonicalization Rules

Identity construction must operate on canonical values, not raw provider text.

Canonicalization should use the project’s existing music identity normalization rules as the baseline, specifically the behavior currently represented by `MusicIdentityText`.

The intended canonicalization rules are:

- trim leading and trailing whitespace
- convert to lowercase invariant text
- collapse repeated whitespace to a single space
- preserve alphanumeric content
- remove punctuation and formatting noise that is not identity-bearing
- treat `null`, empty, or whitespace-only values as missing

For identity construction:

- `CanonicalArtistName` must be present and valid
- `CanonicalTrackName` must be present and valid
- `CanonicalAlbumName` is optional
- `CanonicalReleaseType` is optional
- `CanonicalReleaseDate` is optional

Short justification:

- identity should be derived from stable music meaning, not transport formatting
- canonicalization must be shared so that lookup, storage, and replay all derive the same identities

## Release Semantics

`CanonicalReleaseDate` and `CanonicalReleaseType` are exact-variant detail.

They must not change the `BaseComponent`.

They may change the `VectorComponent`.

This gives the following intended behaviour:

- two studio rereleases with different release dates can have different exact `TrackId`s and the same `BaseComponent`
- studio and radio variants can have different exact `TrackId`s
- low-quality playlist data can still group rereleases by base family when release detail is missing

Short justification:

- exact identity and family membership serve different purposes
- family grouping must survive low-quality source data

## Mathematical Property

The system should model `TrackId` so that all tracks under the same `BaseComponent` are queryable as one mathematical family or range.

This specification mandates the property even though encoding details may evolve:

- `TrackId` must contain or deterministically project to `BaseComponent`
- `TrackId` must contain or deterministically project to `VectorComponent`
- it must be possible to query all exact `TrackId`s that share a given `BaseComponent`
- it must be possible to compare vector positions inside that family

Short justification:

- playlist handling needs exact sibling selection under a known family
- rerelease and variant selection should not require provider-specific logic

## Formal Construction

Let:

- `A` = canonical artist name
- `T` = canonical track name
- `B` = canonical album name or empty
- `V` = deterministic vector inputs derived from release and version detail

Let `Join(x1, x2, ..., xn)` denote a deterministic ordered composition with unambiguous separators and explicit encoding for missing optional values.

Then the required identity functions are:

```text
BaseComponent = G(A, T, B)
VectorComponent = H(V)
TrackId = F(A, T, B, V) = Join(G(A, T, B), H(V))
```

Required mathematical properties:

1. Determinism  
   For the same canonical inputs, `F`, `G`, and `H` must always return the same outputs.

2. Base projection  
   There must exist a total function `ProjectBase` such that:

```text
ProjectBase(F(A, T, B, V)) = G(A, T, B)
```

3. Vector projection  
   There must exist a total function `ProjectVector` such that:

```text
ProjectVector(F(A, T, B, V)) = H(V)
```

4. Family grouping  
   For any two exact identities:

```text
F(A, T, B, V1)
F(A, T, B, V2)
```

both must project to the same base family:

```text
G(A, T, B)
```

5. Exact specificity preservation  
   If the vector inputs differ in a way the domain treats as exact-identity-significant, the exact `TrackId` must be allowed to differ.

Short justification:

- exact variants need one exact identity
- family grouping must remain mechanical, not heuristic
- mathematical comparison must happen only inside a known family

## Ordering And Query Semantics

The implementation must support querying all exact tracks under a given `BaseComponent`.

The mathematical requirement is:

- given `BaseComponent = G(A, T, B)`, the system can enumerate every `TrackId = F(A, T, B, V)` for that family without provider lookups or fuzzy matching

The implementation must also support vector comparison inside that family.

Examples of compliant operations:

- family membership: "does `TrackId x` share the same `BaseComponent` as `TrackId y`?"
- family enumeration: "find all exact tracks under `BaseComponent g`"
- nearest exact sibling: "within family `g`, which concrete exact track is nearest to vector `v`?"
- bounded range selection: "within family `g`, which exact tracks fall inside vector bounds `r`?"

Short justification:

- playlist and artist aggregation scenarios need exact-track selection within a known family

## Selection Rule Within A Base Family

Where multiple exact `TrackId`s exist under the same `BaseComponent`, the system may select a preferred exact track.

The preferred long-term direction is:

- select by deterministic vector comparison within the family

Acceptable examples include:

- nearest vector distance to a generic reference
- bounded range membership followed by deterministic tie-breaking
- vector comparison followed by latest authoritative timestamp

The system must not require provider-specific ids to make this choice.

Short justification:

- playlists may only know the family and still need a concrete exact `TrackId`
- selection should become principled rather than ad hoc

## Example Identity Parts

The following value object shape is consistent with this specification:

```csharp
public sealed record CanonicalTrackIdentityParts(
    string ArtistName,
    string TrackName,
    string? AlbumName,
    DateOnly? ReleaseDate,
    string? ReleaseType);
```

Short justification:

- exact and base identities should be derived from the same canonical input set

## Recommended Construction

The recommended implementation approach is:

- construct identity with zero external lookups
- canonicalize raw metadata locally
- derive a deterministic `BaseComponent`
- derive a deterministic `VectorComponent`
- pack both into one exact `TrackId`

The project may use a strong deterministic hash for part of the construction, especially for the base family portion, but the specification does not require a specific hash algorithm.

Important:

- if hashing is used, it is for deterministic identity construction
- if vector distance is required, that distance must come from structured vector dimensions, not from numeric distance between cryptographic hash outputs

Short justification:

- construction must work offline and without database round-trips
- exact identity and mathematical family behavior must coexist in one value

## Packed Representation

`TrackId` should be persisted as one canonical field per projection document.

This specification intentionally prefers:

- one persisted `TrackId`

And intentionally does not prefer:

- many persisted decomposition fields on every projection document

The packed encoding is implementation-defined, but it must support deterministic decoding of:

- `BaseComponent`
- `VectorComponent`

Short justification:

- new range-selection cases should not cause projection schema growth across every document type
- identity complexity belongs in identity code and indexes, not in every projection shape

## Locked Target Encoding

The target implementation for `Soundtrail.Services` is now locked to:

- one persisted `TrackId` field per projection document
- a fixed-width lowercase hexadecimal packed representation
- deterministic RavenDB index-time decoding through a shared projection helper
- database-driven family filtering and database-driven sibling ranking

This target explicitly replaces the earlier segmented text representation.

Short justification:

- the project wants one durable identity value in storage
- RavenDB indexing works well when a single packed value is decoded into query fields
- fixed-width hex is deterministic, compact, and easy to decode without schema growth

## Locked Structural Model

The locked logical model is:

```text
TrackId = Join(BaseIdentityBits, VectorBits)
```

Where:

- `BaseIdentityBits` define the broad song-family inclusion gate
- `VectorBits` define exact-variant specificity and in-family ranking signals

Required rule:

- optional specificity such as album, release type, and release date must not be required for family inclusion when missing data would otherwise exclude a valid sibling

Short justification:

- low-specificity sources often omit album or release details
- optional data must improve ranking without preventing recovery

## Locked Base Identity Rule

The locked target direction is:

- `BaseIdentityBits` are derived from canonical artist name and canonical core track title

The base identity must not depend on optional release-specific qualifiers whose absence should still permit sibling matching.

This means the family inclusion gate should remain broad enough that:

- an exact album-specific track can still be found when playlist data omits album
- a remake or radio version can still be found when generic input omits release detail

Short justification:

- broad inclusion first, principled ranking second

## Locked Vector Rule

The locked target direction is:

- `VectorBits` carry optional specificity and ranking signals

This includes current expected dimensions such as:

- album-derived discriminator
- parsed release type
- release date or release-date-derived ordering value

It may later include additional dimensions such as:

- remake or remaster profile
- live-performance qualifiers
- future release-shape dimensions

Required rule:

- when optional data is present on both sides, matching values should be favored
- when the exact sibling is absent, a nearby sibling should be favored by deterministic distance or similarity rules

Illustrative intent:

- requested `2024 remake`
- available `2022 remake`
- available `1970 original`

The `2022 remake` should rank ahead of the `1970 original`.

Short justification:

- exactness is best when available
- nearest valid sibling is the correct fallback behavior

## Locked RavenDB Query Model

The locked target query model is:

1. exact `TrackId` lookup when an exact packed value is known
2. family filtering by decoded `BaseIdentityBits`
3. database-side sibling ranking using vector or derived ranking fields decoded from `VectorBits`

The API should not be required to rank large sibling sets in memory as the primary design.

Short justification:

- heavy resolution behavior should remain DB-driven
- API work should stay thin and orchestration-focused

## Locked RavenDB Index Projection Helper Rule

RavenDB index definitions must not duplicate packed identity logic ad hoc.

The target implementation must provide a shared projection helper dedicated to index-time decoding of `TrackId`.

The helper is responsible for deriving Raven-safe fields such as:

- family selection fields
- vector dimensions
- ranking vector payloads
- deterministic tie-break fields

These decoded fields belong in RavenDB indexes, not in persisted projection document schemas.

Short justification:

- identity logic should live in one place
- query-specific projections should be centralized and testable

## Locked Fixed-Width Hex Rule

The packed storage encoding must use:

- lowercase hexadecimal
- fixed-width segments
- deterministic decoding without delimiter heuristics

The canonical stored `TrackId` may still include a stable prefix, but the encoded payload itself must remain fixed-width and parseable by position.

Short justification:

- positional decode is cheap and predictable
- Raven indexes can derive numeric fields from stable segments without schema sprawl

## Locked Parser Responsibility

Identity construction must include a dedicated track-title parser, not only generic text normalization.

The parser must separate:

- canonical core title
- detachable trailing qualifier
- parsed release-type signal

The locked grammar direction is:

- release-type extraction is permitted only from a detachable trailing title segment

Examples of detachable trailing segments include:

- trailing parenthesized qualifier
- trailing bracketed qualifier
- trailing dash-separated qualifier

Examples of recognized release-type vocabulary include:

- `mix`
- `edit`
- `remix`
- `version`
- `instrumental`
- `release`
- `live`

Required rule:

- qualifier words appearing inside the core title body must not be reinterpreted as release type unless they occur in a detachable trailing segment

Short justification:

- this reduces false positives
- parser correctness directly controls sibling-set quality and ranking accuracy

## Locked Ranking Principle

The ranking model must remain deterministic.

When both requested and candidate metadata provide optional specificity:

- exact album should be strongly favored
- exact release type should be strongly favored
- nearer release dates should outrank more distant release dates

When optional specificity is absent:

- candidates must remain eligible if they share the same broad family

Short justification:

- optional metadata should improve precision without blocking recovery

## RavenDB Persistence Rule

For RavenDB document storage:

- projection documents should persist `TrackId` once
- projection documents may persist other business data they already need
- projection documents should not be forced to persist every decoded identity component separately just to support future selection cases

Permitted exceptions:

- a projection may store additional convenience fields for debugging or user-facing output
- those convenience fields are not authoritative identity fields

Short justification:

- this minimizes schema sprawl
- future identity evolution should primarily affect index code, not every document contract

## RavenDB Index Rule

RavenDB indexes may decode the packed `TrackId` into derived query fields.

This is the preferred place to materialize:

- base family selectors
- vector coordinates
- exact tie-break components

These decoded fields are index-time concerns, not projection-schema concerns.

Short justification:

- one domain identity field can still support rich query semantics
- Raven indexes are the correct place to expose query-optimized projections

## RavenDB Spatial Querying

If the vector semantics benefit from mathematical range selection, RavenDB indexes may project the vector subset of `TrackId` into Cartesian coordinates and index them spatially.

Compliant examples include:

- bounding-box membership within a known base family
- nearest-version selection within a known base family
- deterministic in-family ordering using vector axes plus tie-break rules

Non-compliant example:

- treating numeric distance between unrelated exact track ids as musically meaningful

Required rule:

- spatial or vector comparison is meaningful only inside a known `BaseComponent`

Short justification:

- distance between arbitrary songs is not meaningful
- distance between variants of one known song-family can be meaningful

## RavenDB Query Semantics

The required Raven query pattern is:

1. derive or decode `BaseComponent` from `TrackId`
2. constrain candidates to that family
3. apply vector range, spatial membership, or deterministic nearest-selection rules within that family

This means:

- family membership is deterministic
- in-family selection may be mathematical
- provider-specific ids are not required

Short justification:

- the system needs exactness first and ordered selection second

## Index Evolution Rule

If a new dimension later becomes part of range selection, the preferred migration path is:

- keep persisted projection documents unchanged where possible
- update Raven index definitions to decode the new dimension from `TrackId`
- allow Raven to rebuild side-by-side indexes

This specification explicitly prefers index evolution over projection-schema explosion.

Required condition:

- any future range-selection dimension should be derivable from persisted `TrackId` or other already-persisted business data

Short justification:

- the project expects new edge cases over time
- index rebuilds are operationally cheaper than adding identity helper fields to every projection forever

## Example Document Shape

An illustrative RavenDB document shape is:

```csharp
public sealed class TrackDocument
{
    public string Id { get; init; } = string.Empty;

    public string TrackId { get; init; } = string.Empty;

    public string CanonicalArtistName { get; init; } = string.Empty;

    public string CanonicalTrackName { get; init; } = string.Empty;

    public string? CanonicalAlbumName { get; init; }

    public DateOnly? CanonicalReleaseDate { get; init; }

    public string? CanonicalReleaseType { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
```

Short justification:

- the document shape stores one canonical identity value
- this supports replay-safe projections, diagnostics, and future collision checks

## Query Rule

The required grouping query is:

- all tracks under the same song family are all tracks whose decoded `BaseComponent` matches the target family

This means the database query shape is:

- equality or partitioning on decoded base family
- optional range, spatial, or nearest-selection logic on the decoded vector

It is not:

- provider lookup
- fuzzy matching across unrelated song families

Short justification:

- this satisfies the grouping requirement while keeping one persisted identity field
- it preserves exactness and mathematical selection in the same design

## Example Implementation

The following code is illustrative. It is not a final API contract, but it demonstrates the recommended shape.

```csharp
using System.Buffers.Binary;
using System.Text;
using Blake2Fast;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Domain.Catalog.Identity;

public static class CanonicalTrackIdentity
{
    private const int MaxArtistLength = 1000;
    private const int MaxTrackLength = 250;
    private const int MaxAlbumLength = 250;
    private const int MaxReleaseTypeLength = 50;

    public static CanonicalTrackIdentityParts Create(
        string artistName,
        string trackName,
        string? albumName,
        DateOnly? releaseDate,
        string? releaseType)
    {
        var canonicalArtist = CanonicalizeRequired(artistName, MaxArtistLength, nameof(artistName));
        var canonicalTrack = CanonicalizeRequired(trackName, MaxTrackLength, nameof(trackName));
        var canonicalAlbum = CanonicalizeOptional(albumName, MaxAlbumLength, nameof(albumName));
        var canonicalReleaseType = CanonicalizeOptional(releaseType, MaxReleaseTypeLength, nameof(releaseType));

        return new CanonicalTrackIdentityParts(
            canonicalArtist,
            canonicalTrack,
            canonicalAlbum,
            releaseDate,
            canonicalReleaseType);
    }

    public static PackedTrackId CreateTrackId(CanonicalTrackIdentityParts parts)
    {
        var baseComponent = CreateBaseComponent(parts);
        var vectorComponent = CreateVectorComponent(parts);
        return PackedTrackId.From(baseComponent, vectorComponent);
    }

    public static byte[] CreateBaseComponent(CanonicalTrackIdentityParts parts)
    {
        var bytes = Encode(
            parts.ArtistName,
            parts.TrackName,
            parts.AlbumName);

        return Blake2b(bytes, 32);
    }

    public static VariantVector CreateVectorComponent(CanonicalTrackIdentityParts parts)
    {
        return new VariantVector(
            releaseDateOrdinal: parts.ReleaseDate?.DayNumber,
            releaseType: parts.ReleaseType);
    }

    private static string CanonicalizeRequired(string value, int maxLength, string paramName)
    {
        var canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            throw new ArgumentException("Identity value is required.", paramName);
        }

        if (canonical.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Identity value exceeds max length {maxLength}.");
        }

        return canonical;
    }

    private static string? CanonicalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            return null;
        }

        if (canonical.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Identity value exceeds max length {maxLength}.");
        }

        return canonical;
    }

    private static byte[] Encode(params string?[] parts)
    {
        var text = string.Join("|", parts.Select(part => part ?? "~"));
        return Encoding.UTF8.GetBytes(text);
    }

    private static byte[] Blake2b(byte[] bytes, int outputLength)
    {
        return Blake2b.ComputeHash(outputLength, bytes).ToArray();
    }
}

public readonly record struct PackedTrackId(string Value)
{
    public static PackedTrackId From(byte[] baseComponent, VariantVector vector) =>
        new(Convert.ToBase64String(Pack(baseComponent, vector)));
}

public readonly record struct VariantVector(int? ReleaseDateOrdinal, string? ReleaseType);
```

This example shows the core invariants:

- identity values are validated before key derivation
- key derivation uses only local canonicalized inputs
- exact identity is composed from one family component plus one vector component
- one persisted identity value can still be decoded by Raven indexes for querying

Note:

- the code above is illustrative and assumes the `SauceControl.Blake2Fast` package API shown in its documentation

## Example Projection Rule

Any final implementation must provide deterministic decoders for at least:

- `ProjectBase(trackId)`
- `ProjectVector(trackId)`

An example API shape is:

```csharp
public static class TrackIdentityProjection
{
    public static byte[] GetBaseComponent(PackedTrackId trackId) => DecodeBase(trackId.Value);

    public static VariantVector GetVectorComponent(PackedTrackId trackId) => DecodeVector(trackId.Value);
}
```

Short justification:

- grouping and vector behavior must not depend on replaying provider logic or reparsing external identifiers

## Worked Examples

Example 1:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: `1997-05-21`
- release type: `studio`

Produces:

- one deterministic `BaseComponent`
- one deterministic `VectorComponent`
- one exact packed `TrackId`

Example 2:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: `2009-01-01`
- release type: `studio`

Produces:

- same `BaseComponent` as Example 1
- different `VectorComponent`
- different exact `TrackId`

Example 3:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: missing
- release type: missing

Produces:

- the same `BaseComponent` as the richer examples
- a deterministic generic `VectorComponent`
- a deterministic exact `TrackId` even when release detail is missing

Short justification:

- low-quality playlist data can still be represented deterministically
- richer future data can still produce more specific exact identities

## Playlist Lookup Consequence

Playlist lookup success may contain low-quality track metadata.

That data may be insufficient to determine exact variant detail, but it should still be sufficient to derive a `BaseComponent` when artist and track title are valid.

The intended future behaviour is:

- use playlist metadata to derive the target `BaseComponent`
- query existing tracks under that family
- compare vector position only within that family
- prefer an existing specific `TrackId` when one is already known
- otherwise use the best valid identity derivable from the available canonical data

Short justification:

- playlists should be writable without provider ids
- playlist handling should not depend on MusicBrainz release modelling or provider product ids

## Invalid And Unplayable Items

An item with metadata that exceeds accepted canonical limits must not produce a `TrackId`.

This should not necessarily fail an entire playlist or batch lookup.

The long-term intended behaviour is:

- valid items remain playable
- invalid or unsupported items are recorded as not playable
- consumer applications can surface them similarly to greyed-out items in iTunes

Short justification:

- a single bad item should not invalidate an entire playlist
- the system should preserve useful partial results while refusing unsafe identity generation

## Non-Goals

This spec does not define:

- the final storage encoding of `TrackId`
- whether `TrackId` is bit-packed binary, base64, or another single-field encoding
- the final vector dimensions and weighting rules
- the final persistence schema for playlists
- the full `OnLookupCompleted` workflow
- provider-specific metadata adapters

These are separate design steps.

## Hard Rules

Any future implementation of `TrackId` must preserve all of the following:

- provider-agnostic identity
- no dependence on provider track ids
- no dependence on MusicBrainz release ids
- no dependence on iTunes product ids
- no truncation during identity generation
- explicit failure when canonical metadata exceeds accepted limits
- one exact identity value per track
- the ability to project family membership from that identity
- the ability to project vector comparison semantics from that identity
- the ability to query all tracks under a given family

If a future implementation violates any of these rules, it is non-compliant with this specification.
