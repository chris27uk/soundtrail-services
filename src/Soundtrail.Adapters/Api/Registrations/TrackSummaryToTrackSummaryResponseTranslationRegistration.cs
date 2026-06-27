using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

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
                    track.AvailableProviders.Select(x => x.StableValue).ToArray(),
                    track.TerminallyUnavailableProviders.Select(x => x.StableValue).ToArray(),
                    track.ProviderReferences.Select(x => registry.Translate<ProviderReferenceResponseDto>(x)).ToArray()));
    }
}
