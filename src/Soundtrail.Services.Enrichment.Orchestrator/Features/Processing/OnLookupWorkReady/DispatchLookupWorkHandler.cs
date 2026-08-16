using System.Diagnostics;
using Soundtrail.Adapters.MusicBrainzDumpFreshness;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;

public sealed class LookupWorkReadyHandler(
    IMusicBrainzDumpFreshnessEvaluator dumpFreshnessEvaluator,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<DispatchLookupWork>
{
    public async Task Handle(IncomingMessage<DispatchLookupWork> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var plan = LookupPlanningPolicy.Build(request);
        Activity.Current?.SetTag("soundtrail.lookup_attempt_count", plan.Attempts.Count);

        // Dispatch only the first attempt. LookupCompletedHandler advances the plan so
        // Completions never race on the same discovery stream.
        if (plan.Attempts.Count == 0)
        {
            return;
        }

        var attempt = plan.Attempts[0];
        var workerCommand = WorkerCommandFactory.Create(request, attempt);
        var freshness = await TryEvaluateDumpFreshnessAsync(attempt, clock.UtcNow, cancellationToken);
        if (freshness is { UseCatalog: true })
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.CreatedAt,
                    request.CorrelationId,
                    new LookupResult.Succeeded(
                        new LookupResultContext(
                            CatalogWorkId.From(request.Target),
                            workerCommand.Id),
                        new LookedUpData.CatalogEntries(freshness.CatalogEntries),
                        clock.UtcNow)),
                cancellationToken);
            return;
        }

        await commandBus.SendAsync(workerCommand, cancellationToken);
    }

    private async Task<MusicBrainzDumpFreshnessDecision?> TryEvaluateDumpFreshnessAsync(
        LookupAttempt attempt,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        attempt switch
        {
            LookupAttempt.MusicbrainzArtistAlbums(var artistId, _) =>
                await dumpFreshnessEvaluator.EvaluateArtistAlbumsAsync(artistId, utcNow, cancellationToken),
            LookupAttempt.MusicbrainzArtistTracks(var artistId, _) =>
                await dumpFreshnessEvaluator.EvaluateArtistTracksAsync(artistId, utcNow, cancellationToken),
            LookupAttempt.MusicbrainzAlbumTracks(var albumId, _) =>
                await dumpFreshnessEvaluator.EvaluateAlbumTracksAsync(albumId, utcNow, cancellationToken),
            _ => null
        };
}
