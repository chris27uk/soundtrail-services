using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class LookupMusicMetadataHandlerTestEnvironment
{
    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private LookupMusicMetadataHandlerTestEnvironment()
    {
        Metadata = new FakeGetMusicMetadata();
        Admission = new LookupExecutionAdmissionPortFake();
        Bus = new CommandBusFake();
        var coreHandler = new LookupTrackMetadataHandler(Metadata, Bus);
        Handler = new LookupTrackMetadataExecutionAdmissionDecorator(Admission, coreHandler, Bus);
    }

    public IHandler<LookupTrackMetadataCommand> Handler { get; }

    public FakeGetMusicMetadata Metadata { get; }

    public LookupExecutionAdmissionPortFake Admission { get; }

    public CommandBusFake Bus { get; }

    public static LookupMusicMetadataHandlerTestEnvironment Create() => new();

    public void SeedMusicBrainzIsrc(string isrc, SongMetadata metadata) => Metadata.SeedIsrc(isrc, metadata);

    public void SeedMusicBrainzNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        Metadata.SeedNames(title, artist, albumName, metadata);

    public void SeedMusicBrainzQuery(string query, SongMetadata metadata) =>
        Metadata.SeedQuery(query, metadata);

    public void Throw(Exception ex) => Metadata.Throw(ex);

    public async Task<MusicCatalogLookupAttempted> HandleNewExecutionCommand(MusicSearchCriteria? searchTerm = null)
    {
        await Handler.Handle(
            new LookupTrackMetadataCommand(
                CommandId.For("LookupTrackMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                DefaultCreatedAt,
                CorrelationId.From("corr-1"),
                searchTerm ?? MusicSearchCriteria.ByIsrc("isrc-1")),
            CancellationToken.None);

        return LastAttempt();
    }

    public async Task<MusicCatalogLookupAttempted> HandleDuplicateExecutionCommand(MusicSearchCriteria? searchTerm = null)
    {
        var command = new LookupTrackMetadataCommand(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            DefaultCreatedAt,
            CorrelationId.From("corr-1"),
            searchTerm ?? MusicSearchCriteria.ByIsrc("isrc-1"));
        await Handler.Handle(command, CancellationToken.None);
        await Handler.Handle(command, CancellationToken.None);
        return LastAttempt();
    }

    private MusicCatalogLookupAttempted LastAttempt() =>
        Bus.SentCommands.OfType<MusicCatalogLookupAttempted>().Last();
}
