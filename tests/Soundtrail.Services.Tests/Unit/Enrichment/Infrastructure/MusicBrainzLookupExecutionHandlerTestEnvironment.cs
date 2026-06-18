using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicBrainzLookupExecutionHandlerTestEnvironment
{
    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly LookupExecutionReceiptStoreFake.State state;

    private MusicBrainzLookupExecutionHandlerTestEnvironment()
    {
        state = new LookupExecutionReceiptStoreFake.State();
        Metadata = new FakeGetCanonicalMusicMetadata();
        SourceBudget = new SourceApiBudgetPortFake();
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        var catalogSearchTrackings = new CatalogSearchTrackingStoreFake();
        catalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            DefaultCreatedAt));
        Handler = new OnDemandLookupMetadataHandler(
            new LookupExecutionReceiptStoreFake(state),
            Metadata,
            SourceBudget,
            catalogSearchTrackings,
            DiscoveryRepository);
    }

    public OnDemandLookupMetadataHandler Handler { get; }

    public FakeGetCanonicalMusicMetadata Metadata { get; }

    public SourceApiBudgetPortFake SourceBudget { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public static MusicBrainzLookupExecutionHandlerTestEnvironment Create() => new();

    public void SeedMusicBrainzIsrc(string isrc, SongMetadata metadata) => Metadata.SeedIsrc(isrc, metadata);

    public void SeedMusicBrainzNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        Metadata.SeedNames(title, artist, albumName, metadata);

    public void Throw(Exception ex) => Metadata.Throw(ex);

    public Task<Soundtrail.Domain.Responses.LookupExecutionResult> HandleNewExecutionCommand(MusicSearchTerm? searchTerm = null) =>
        Handler.Handle(
            new Soundtrail.Domain.Commands.LookupMusicMetadataCommand(
                CommandId.For("LookupCanonicalMusicMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                DefaultCreatedAt,
                CorrelationId.From("corr-1"),
                searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1")),
            CancellationToken.None);

    public async Task<Soundtrail.Domain.Responses.LookupExecutionResult> HandleDuplicateExecutionCommand(MusicSearchTerm? searchTerm = null)
    {
        var command = new Soundtrail.Domain.Commands.LookupMusicMetadataCommand(
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            DefaultCreatedAt,
            CorrelationId.From("corr-1"),
            searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1"));
        await Handler.Handle(command, CancellationToken.None);
        return await Handler.Handle(command, CancellationToken.None);
    }
}
