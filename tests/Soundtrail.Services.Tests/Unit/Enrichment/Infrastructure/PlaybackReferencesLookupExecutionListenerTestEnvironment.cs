using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class PlaybackReferencesLookupExecutionListenerTestEnvironment
{
    private static readonly ResolvePlaybackReferencesCommandDto DefaultCommand =
        new(
            CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new PlaybackReferenceSearchTermDto("isrc-1", null, null, null));

    private PlaybackReferencesLookupExecutionListenerTestEnvironment(LookupExecutionReceiptStoreFake.State state)
    {
        GetMusicTrackReference = new FakeGetMusicTrackReference();
        DiscoveryStatus = new InMemoryUpsertCatalogSearchStatus();
        var catalogSearchTrackings = new CatalogSearchTrackingStoreFake();
        catalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)));
        Listener = new PlaybackReferencesLookupExecutionListener(
            new ExecutePlaybackReferencesLookupHandler(
                new LookupExecutionReceiptStoreFake(state),
                GetMusicTrackReference),
            catalogSearchTrackings,
            DiscoveryStatus);
    }

    public PlaybackReferencesLookupExecutionListener Listener { get; }

    public FakeGetMusicTrackReference GetMusicTrackReference { get; }

    public InMemoryUpsertCatalogSearchStatus DiscoveryStatus { get; }

    public static PlaybackReferencesLookupExecutionListenerTestEnvironment WithANewExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public static PlaybackReferencesLookupExecutionListenerTestEnvironment WithADuplicateExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public void Seed(MusicSearchTerm lookupKey, params ExternalReference[] references) =>
        GetMusicTrackReference.Seed(lookupKey, references);

    public Task<object[]> HandleNewExecutionCommand() =>
        Listener.Handle(DefaultCommand, null!);

    public Task<object[]> HandleNewExecutionCommand(ResolvePlaybackReferencesCommandDto command) =>
        Listener.Handle(command, null!);

    public async Task<object[]> HandleDuplicateExecutionCommand()
    {
        await Listener.Handle(DefaultCommand, null!);
        return await Listener.Handle(DefaultCommand, null!);
    }

    public void Throw(Exception ex) => GetMusicTrackReference.Throw(ex);
}
