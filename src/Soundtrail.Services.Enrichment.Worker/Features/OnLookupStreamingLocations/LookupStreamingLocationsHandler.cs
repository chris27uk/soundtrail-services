using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;

public sealed class LookupStreamingLocationsHandler(IGetMusicTrackReference getMusicTrackReference, ICommandBus bus)
    : IHandler<LookupStreamingLocationsCommand>
{
    private static readonly ProviderName[] SupportedPlaybackProviders = ProviderName.All;

    public async Task Handle(LookupStreamingLocationsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var references = await getMusicTrackReference.GetReferenceToMusicTrack(command.LookupKey, cancellationToken);
            var failures = SupportedPlaybackProviders
                .Where(provider => references.All(reference => reference.Provider != provider))
                .Select(provider => new ProviderLookupFailure(provider, command.TargetProvider))
                .ToArray();
            await bus.SendAsync(Completed(command, references, failures), cancellationToken);
        }
        catch
        {
            await bus.SendAsync(Failed(command), cancellationToken);
        }
    }

    private static MusicCatalogLookupAttempted Completed(LookupStreamingLocationsCommand command, IReadOnlyList<ExternalReference> references, ProviderLookupFailure[] failures)
    {
        return MusicCatalogLookupAttempted.Completed(command.ToMusicCatalogMetadataFetched(references, failures));
    }

    private static MusicCatalogLookupAttempted Failed(LookupStreamingLocationsCommand command)
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
