using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Domain.Catalog.Commands;

public sealed record MusicCatalogChangedCommand(ArtistId ArtistId, IReadOnlyList<VersionedCatalogEvent> Events)
{
    public MusicCatalogChangedCommand(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<VersionedMusicTrackEvent> events)
        : this(
            ResolveArtistId(musicCatalogId, events),
            events.Select(x => new VersionedCatalogEvent(x.Version, EnrichLegacyTrackEvent(musicCatalogId, x.Event))).ToArray())
    {
    }

    private static ArtistId ResolveArtistId(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<VersionedMusicTrackEvent> events)
    {
        var explicitArtistId = events
            .Select(x => x.Event)
            .OfType<ArtistDiscovered>()
            .Select(x => x.ArtistId)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (!string.IsNullOrWhiteSpace(explicitArtistId))
        {
            return ArtistId.From(explicitArtistId);
        }

        var correctedArtistId = events
            .Select(x => x.Event)
            .OfType<MetadataCorrected>()
            .Select(x => x.ArtistId)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (!string.IsNullOrWhiteSpace(correctedArtistId))
        {
            return ArtistId.From(correctedArtistId);
        }

        var discoveredArtistName = events
            .Select(x => x.Event)
            .OfType<TrackDiscovered>()
            .Select(x => x.Artist)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var resolved = ArtistCatalogIdentity.ResolveArtistIdOrNull(null, discoveredArtistName);
        if (resolved is not null)
        {
            return resolved.Value;
        }

        return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(musicCatalogId.Value)}");
    }

    private static IDomainEvent EnrichLegacyTrackEvent(
        MusicCatalogId musicCatalogId,
        IMusicTrackEvent @event) =>
        @event switch
        {
            TrackDiscovered discovered when discovered.MusicCatalogId is null =>
                discovered with { MusicCatalogId = musicCatalogId },
            ProviderReferenceDiscovered discovered when discovered.MusicCatalogId is null =>
                discovered with { MusicCatalogId = musicCatalogId },
            ProviderReferenceLookupFailed failed when failed.MusicCatalogId is null =>
                failed with { MusicCatalogId = musicCatalogId },
            MetadataCorrected corrected when corrected.MusicCatalogId is null =>
                corrected with { MusicCatalogId = musicCatalogId },
            _ => @event
        };
}
