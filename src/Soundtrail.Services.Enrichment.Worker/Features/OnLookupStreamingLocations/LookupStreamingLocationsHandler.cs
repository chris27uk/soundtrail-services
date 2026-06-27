using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;

public sealed class LookupStreamingLocationsHandler(IGetMusicTrackReference getMusicTrackReference)
    : ILookupStreamingLocationsHandler
{
    private static readonly ProviderName[] SupportedPlaybackProviders =
    [
        ProviderName.AppleMusic,
        ProviderName.Spotify,
        ProviderName.YoutubeMusic
    ];

    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupStreamingLocationsCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var references = await getMusicTrackReference.GetReferenceToMusicTrack(command.LookupKey, cancellationToken);
            var failures = SupportedPlaybackProviders
                .Where(provider => references.All(reference => reference.Provider != provider))
                .Select(provider => new ProviderLookupFailure(provider, command.TargetProvider))
                .ToArray();
            return MusicCatalogLookupAttempted.Completed(command.ToMusicCatalogMetadataFetched(references, failures));
        }
        catch
        {
            return MusicCatalogLookupAttempted.Failed(
                command.CommandId,
                command.MusicCatalogId,
                command.TargetProvider,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                "Lookup failed");
        }
    }
}
