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

`TrackId` represents the most specific known identity for a track.

It is built from canonical music metadata in this order:

- `CanonicalArtistName`
- `CanonicalTrackName`
- optional `CanonicalAlbumName`
- optional `CanonicalReleaseDate`
- optional `CanonicalReleaseType`

Short justification:

- artist and track name are the irreducible core identity
- album can further distinguish tracks where the same artist/title pairing is insufficient
- release date and release type are specific detail and must remain part of exact identity when known

### BaseTrackIdentity

`BaseTrackIdentity` represents the non-release grouping for a track.

It is built from:

- `CanonicalArtistName`
- `CanonicalTrackName`
- optional `CanonicalAlbumName`

It explicitly excludes:

- `CanonicalReleaseDate`
- `CanonicalReleaseType`

Short justification:

- rereleases such as `2006` and `2009` should be queryable as part of the same base range
- release type and release date are too specific for playlist low-quality data and should not be required for grouping

## Required And Optional Components

### Required

These are always required to construct either a `TrackId` or a `BaseTrackIdentity`.

- `CanonicalArtistName`
- `CanonicalTrackName`

Short justification:

- a track without canonical artist and track title does not have enough stable music identity to be catalogued safely

### Optional

These increase specificity when present.

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

`CanonicalReleaseDate` and `CanonicalReleaseType` are specific identity detail.

They must remain part of `TrackId` when known.

They must not participate in `BaseTrackIdentity`.

This gives the following intended behaviour:

- two studio rereleases with different release dates can have different `TrackId`s and the same `BaseTrackIdentity`
- studio and radio variants can have different `TrackId`s
- low-quality playlist data can still group rereleases by base identity even when release detail is missing

Short justification:

- exact identity and grouping identity serve different purposes
- the grouping identity should support playlist matching without flattening all release-specific detail

## Mathematical Property

The system should model `TrackId` so that all tracks under the same `BaseTrackIdentity` are queryable as one range.

This spec mandates the property even though final persistence encoding may still evolve:

- `TrackId` must contain or project to `BaseTrackIdentity`
- it must be possible to query all specific `TrackId`s that share a given `BaseTrackIdentity`

Short justification:

- playlist handling needs to select from existing tracks under the same non-release identity
- rerelease selection should not require provider-specific lookup logic

## Formal Construction

Let:

- `A` = canonical artist name
- `T` = canonical track name
- `B` = canonical album name or empty
- `D` = canonical release date or empty
- `R` = canonical release type or empty

Let `Join(x1, x2, ..., xn)` denote a deterministic ordered composition with unambiguous separators and explicit encoding for missing optional values.

Then the identity functions are:

```text
BaseTrackIdentity = G(A, T, B) = Join(A, T, B)
TrackId = F(A, T, B, D, R) = Join(G(A, T, B), D, R)
```

Required mathematical properties:

1. Determinism  
   For the same canonical inputs, `F` and `G` must always return the same outputs.

2. Projection  
   There must exist a total function `ProjectBase` such that:

```text
ProjectBase(F(A, T, B, D, R)) = G(A, T, B)
```

3. Range grouping  
   For any two exact identities:

```text
F(A, T, B, D1, R1)
F(A, T, B, D2, R2)
```

both must project to the same base identity:

```text
G(A, T, B)
```

4. Specificity preservation  
   If either release date or release type differs, the exact identity must be allowed to differ:

```text
if D1 != D2 or R1 != R2 then F(A, T, B, D1, R1) may differ from F(A, T, B, D2, R2)
```

Short justification:

- rereleases need to group together without losing exact release-specific identity
- the projection must be mechanical, not heuristic

## Ordering And Query Semantics

The implementation must support querying all exact tracks under a given base identity.

There are two compliant ways to achieve this:

- an encoding where `BaseTrackIdentity` is a literal prefix or high-order component of `TrackId`
- a schema where `BaseTrackIdentity` is stored separately and indexed alongside `TrackId`

The mathematical requirement is:

- given `BaseTrackIdentity = G(A, T, B)`, the system can enumerate every `TrackId = F(A, T, B, D, R)` for that base identity without provider lookups or fuzzy matching

Short justification:

- playlist completion needs exact-track selection within a known non-release family

## Selection Rule Within A Base Identity

Where multiple exact `TrackId`s exist under the same `BaseTrackIdentity`, the system may select a preferred exact track.

The intended policy direction is:

- select the most recently added exact `TrackId` within the base identity range

This spec does not yet define the final persisted ordering source, but acceptable examples include:

- latest authoritative catalog event timestamp
- latest append order within the catalog event stream

Short justification:

- playlists may only know the base identity and still need a concrete exact `TrackId`

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

## Recommended Numeric Construction

The recommended implementation approach is:

- construct identity with zero external lookups
- canonicalize raw metadata locally
- use `BLAKE2b`
- derive a large `BaseKey` from non-release identity
- derive a separate `SpecificKey` from release-specific identity
- persist both parts explicitly for RavenDB querying

This means `TrackId` is logically one structured identity, but the persisted/query-friendly representation is split into:

- `BaseKey`
- `SpecificKey`

Short justification:

- collision resistance matters more than forcing one compact primitive
- RavenDB queries are simpler and safer when the grouping key is stored explicitly
- construction must work even when names are known but not pre-normalized

An implementation may use the `SauceControl.Blake2Fast` NuGet package for the unkeyed `BLAKE2b` hashing step.

Source:

- [SauceControl.Blake2Fast on NuGet](https://www.nuget.org/packages/SauceControl.Blake2Fast)

## Key Derivation

Given canonical parts:

- `A` = canonical artist name
- `T` = canonical track name
- `B` = canonical album name or empty
- `D` = canonical release date or empty
- `R` = canonical release type or empty

Construct two byte sequences:

```text
BaseBytes = Encode(A, T, B)
SpecificBytes = Encode(D, R)
```

Then derive:

```text
BaseKey = First256Bits(BLAKE2b(BaseBytes))
SpecificKey = First128Bits(BLAKE2b(SpecificBytes))
```

Required properties:

- the same canonical inputs always produce the same keys
- `BaseKey` depends only on artist, track, and album
- `SpecificKey` depends only on release-specific inputs
- no central registry or repository lookup is involved

Short justification:

- this preserves zero-lookup construction
- this keeps collision resistance especially high for grouping identity

## Representation

The recommended RavenDB-friendly persisted shape is:

- `TrackIdBaseKeyHigh`
- `TrackIdBaseKeyLow`
- `TrackIdSpecificKey`

Where:

- `TrackIdBaseKeyHigh` is the high 128 bits of the 256-bit `BaseKey`
- `TrackIdBaseKeyLow` is the low 128 bits of the 256-bit `BaseKey`
- `TrackIdSpecificKey` is the 128-bit `SpecificKey`

In application code, these three values together represent one structured `TrackId`.

Short justification:

- this keeps the exact identity structured and durable
- this avoids relying on a native 384-bit primitive that C# and RavenDB do not provide

## RavenDB Persistence Shape

For RavenDB document storage and indexing, the preferred shape is:

- persist exact identity as named scalar fields
- persist the canonical source fields alongside the derived keys
- query by base-key equality rather than by reparsing text identity

The recommended document fields for a track projection are:

- `TrackIdBaseKeyHigh`
- `TrackIdBaseKeyLow`
- `TrackIdSpecificKey`
- `CanonicalArtistName`
- `CanonicalTrackName`
- `CanonicalAlbumName`
- `CanonicalReleaseDate`
- `CanonicalReleaseType`
- `ObservedAt`

Optional convenience fields may also be stored:

- `TrackIdDisplay`
- `BaseKeyDisplay`

These convenience fields are for debugging and operational visibility only. They are not authoritative and must not be used as the source of identity logic.

Short justification:

- RavenDB queries and indexes work best with explicit scalar fields
- the canonical fields remain available for debugging, projections, and collision investigation
- the derived keys remain the authoritative query identity

## RavenDB Index Shape

The preferred index shape for sibling-track queries is an index keyed by the base-key parts and the observed ordering field.

An illustrative static index shape is:

```csharp
public sealed class Tracks_ByBaseKey : AbstractIndexCreationTask<TrackDocument>
{
    public Tracks_ByBaseKey()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.TrackIdBaseKeyHigh,
                            track.TrackIdBaseKeyLow,
                            track.TrackIdSpecificKey,
                            track.ObservedAt
                        };
    }
}
```

Short justification:

- the base-key fields support exact sibling lookup
- the specific key distinguishes exact tracks within the same base identity
- the ordering field supports preferred-track selection

## RavenDB Query Semantics

The primary sibling query should be expressed as equality on the base-key fields:

```csharp
var siblings = await session.Query<TrackDocument, Tracks_ByBaseKey>()
    .Where(x =>
        x.TrackIdBaseKeyHigh == baseKey.High &&
        x.TrackIdBaseKeyLow == baseKey.Low)
    .OrderByDescending(x => x.ObservedAt)
    .ToListAsync(cancellationToken);
```

The preferred exact track for a base identity is then:

- the first result ordered by the chosen ordering rule

Short justification:

- this avoids fuzzy logic and reparsing
- this keeps the database query aligned to the mathematical grouping model

## RavenDB Range Note

This specification does not require a single-field numeric `BETWEEN` query over a packed 384-bit scalar.

Instead, for RavenDB, the required operational behavior is:

- equality query on `TrackIdBaseKeyHigh`
- equality query on `TrackIdBaseKeyLow`
- optional ordering or filtering within that sibling set

This still satisfies the core range intent:

- all tracks that belong to the same non-release identity are queryable directly and efficiently by their shared base-key parts

Short justification:

- RavenDB handles indexed scalar equality queries naturally
- this preserves collision resistance without forcing an unnatural giant primitive representation

## Example Document Shape

An illustrative RavenDB document shape is:

```csharp
public sealed class TrackDocument
{
    public string Id { get; init; } = string.Empty;

    public UInt128 TrackIdBaseKeyHigh { get; init; }

    public UInt128 TrackIdBaseKeyLow { get; init; }

    public UInt128 TrackIdSpecificKey { get; init; }

    public string CanonicalArtistName { get; init; } = string.Empty;

    public string CanonicalTrackName { get; init; } = string.Empty;

    public string? CanonicalAlbumName { get; init; }

    public DateOnly? CanonicalReleaseDate { get; init; }

    public string? CanonicalReleaseType { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
```

Short justification:

- the document shape keeps derived identity and canonical facts side by side
- this supports replay-safe projections, diagnostics, and future collision checks

## Query Rule

The required grouping query is:

- all tracks under the same base identity are all tracks whose `TrackIdBaseKeyHigh` and `TrackIdBaseKeyLow` match the target base key

This means the database query shape is equality on the stored base-key parts, not fuzzy matching and not provider lookup.

Short justification:

- this satisfies the grouping requirement even when a single packed 384-bit numeric range is impractical
- this is operationally simpler in RavenDB than forcing one giant scalar

## Example Implementation

The following code is illustrative. It is not a final API contract, but it demonstrates the recommended construction.

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

    public static StructuredTrackId CreateTrackId(CanonicalTrackIdentityParts parts)
    {
        var baseKey = CreateBaseKey(parts);
        var specificKey = CreateSpecificKey(parts);
        return new StructuredTrackId(baseKey.High, baseKey.Low, specificKey);
    }

    public static UInt256Parts CreateBaseKey(CanonicalTrackIdentityParts parts)
    {
        var bytes = Encode(
            parts.ArtistName,
            parts.TrackName,
            parts.AlbumName);

        return UInt256Parts.FromBytes(Blake2b(bytes, 32));
    }

    public static UInt128 CreateSpecificKey(CanonicalTrackIdentityParts parts)
    {
        var bytes = Encode(
            parts.ReleaseDate?.ToString("yyyy-MM-dd"),
            parts.ReleaseType);

        var hash = Blake2b(bytes, 16);
        return BinaryPrimitives.ReadUInt128BigEndian(hash);
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

public readonly record struct StructuredTrackId(
    UInt128 BaseKeyHigh,
    UInt128 BaseKeyLow,
    UInt128 SpecificKey)
{
    public (UInt128 High, UInt128 Low) GetBaseKey() => (BaseKeyHigh, BaseKeyLow);
}

public readonly record struct UInt256Parts(UInt128 High, UInt128 Low)
{
    public static UInt256Parts FromBytes(byte[] bytes)
    {
        var high = BinaryPrimitives.ReadUInt128BigEndian(bytes[..16]);
        var low = BinaryPrimitives.ReadUInt128BigEndian(bytes[16..32]);
        return new UInt256Parts(high, low);
    }
}
```

This example shows the core invariants:

- identity values are validated before key derivation
- key derivation uses only local canonicalized inputs
- exact identity is composed from a strong grouping key plus strong specific key
- the grouping key is directly queryable in RavenDB without reparsing provider data

Note:

- the code above is illustrative and assumes the `SauceControl.Blake2Fast` package API shown in its documentation

## Example Projection Rule

Any final implementation must provide a deterministic way to get the grouping key for any exact track identity.

An example API shape is:

```csharp
public static class TrackIdentityProjection
{
    public static (UInt128 High, UInt128 Low) GetBaseKey(StructuredTrackId trackId) =>
        trackId.GetBaseKey();
}
```

Short justification:

- grouping behavior must not depend on replaying provider logic or reparsing external identifiers

## Worked Examples

Example 1:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: `1997-05-21`
- release type: `studio`

Produces:

- `BaseKey = BLAKE2b-256("radiohead|karma police|ok computer")`
- `SpecificKey = BLAKE2b-128("1997-05-21|studio")`
- `TrackId = (BaseKeyHigh, BaseKeyLow, SpecificKey)`

Example 2:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: `2009-01-01`
- release type: `studio`

Produces:

- same `BaseKey` as Example 1
- different `SpecificKey`
- different exact `TrackId`

Example 3:

- artist: `Radiohead`
- track: `Karma Police`
- album: `OK Computer`
- release date: missing
- release type: missing

Produces:

- the same `BaseKey` as the richer examples
- a deterministic `SpecificKey` derived from `"~|~"`
- a deterministic exact `TrackId` even when release detail is missing

Short justification:

- low-quality playlist data can still be represented deterministically
- richer future data can still produce more specific exact identities

## Playlist Lookup Consequence

Playlist lookup success may contain low-quality track metadata.

That data may be insufficient to determine exact release detail, but it should still be sufficient to derive a `BaseTrackIdentity` when artist and track title are valid.

The intended future behaviour is:

- use playlist metadata to derive `BaseTrackIdentity`
- query existing tracks under that base identity
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
- whether `TrackId` is numeric or string-backed
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
- separation of exact identity from broader base identity
- the ability to query all tracks under a given base identity

If a future implementation violates any of these rules, it is non-compliant with this specification.
