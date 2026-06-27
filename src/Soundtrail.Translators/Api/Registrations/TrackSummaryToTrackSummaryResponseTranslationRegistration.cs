using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class TrackSummaryToTrackSummaryResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<TrackSummary, TrackSummaryResponseDto>(
            translate: track =>
                new TrackSummaryResponseDto(
                    track.TrackId.Value,
                    track.Title,
                    track.AlbumId.Value,
                    track.AlbumName,
                    track.Isrc,
                    track.DurationMs,
                    track.PlayabilityStatus.ToString(),
                    track.AvailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    track.TerminallyUnavailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    track.ProviderReferences.Select(x => registry.Translate<ProviderReferenceResponseDto>(x)).ToArray()));
    }
}
