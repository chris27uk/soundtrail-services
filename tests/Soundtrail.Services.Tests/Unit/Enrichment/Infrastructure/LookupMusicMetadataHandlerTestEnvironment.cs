using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
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
        var coreHandler = new LookupMusicMetadataHandler(Metadata);
        Handler = new LookupMusicMetadataExecutionAdmissionDecorator(Admission, coreHandler);
    }

    public ILookupMusicMetadataHandler Handler { get; }

    public FakeGetMusicMetadata Metadata { get; }

    public LookupExecutionAdmissionPortFake Admission { get; }

    public static LookupMusicMetadataHandlerTestEnvironment Create() => new();

    public void SeedMusicBrainzIsrc(string isrc, SongMetadata metadata) => Metadata.SeedIsrc(isrc, metadata);

    public void SeedMusicBrainzNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        Metadata.SeedNames(title, artist, albumName, metadata);

    public void SeedMusicBrainzQuery(string query, SongMetadata metadata) =>
        Metadata.SeedQuery(query, metadata);

    public void Throw(Exception ex) => Metadata.Throw(ex);

    public Task<Soundtrail.Domain.Responses.MusicCatalogLookupAttempted> HandleNewExecutionCommand(MusicSearchTerm? searchTerm = null) =>
        Handler.Handle(
            new LookupMusicMetadataCommand(
                CommandId.For("LookupMusicMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                DefaultCreatedAt,
                CorrelationId.From("corr-1"),
                searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1")),
            CancellationToken.None);

    public async Task<Soundtrail.Domain.Responses.MusicCatalogLookupAttempted> HandleDuplicateExecutionCommand(MusicSearchTerm? searchTerm = null)
    {
        var command = new LookupMusicMetadataCommand(
            CommandId.For("LookupMusicMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            DefaultCreatedAt,
            CorrelationId.From("corr-1"),
            searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1"));
        await Handler.Handle(command, CancellationToken.None);
        return await Handler.Handle(command, CancellationToken.None);
    }
}
