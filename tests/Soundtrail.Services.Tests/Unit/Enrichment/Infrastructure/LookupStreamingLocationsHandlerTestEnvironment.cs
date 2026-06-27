using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class LookupStreamingLocationsHandlerTestEnvironment
{
    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private LookupStreamingLocationsHandlerTestEnvironment()
    {
        GetMusicTrackReference = new FakeGetMusicTrackReference();
        Admission = new LookupExecutionAdmissionPortFake();
        var coreHandler = new LookupStreamingLocationsHandler(GetMusicTrackReference);
        Handler = new LookupStreamingLocationsExecutionAdmissionDecorator(Admission, coreHandler);
    }

    public ILookupStreamingLocationsHandler Handler { get; }

    public FakeGetMusicTrackReference GetMusicTrackReference { get; }

    public FakeGetMusicTrackReference References => GetMusicTrackReference;

    public LookupExecutionAdmissionPortFake Admission { get; }

    public static LookupStreamingLocationsHandlerTestEnvironment Create() => new();

    public void Seed(MusicSearchCriteria lookupKey, params ExternalReference[] references) =>
        GetMusicTrackReference.Seed(lookupKey, references);

    public Task<MusicCatalogLookupAttempted> HandleNewExecutionCommand(MusicSearchCriteria? searchTerm = null) =>
        Handler.Handle(
            new LookupStreamingLocationsCommand(
                CommandId.For("LookupStreamingLocations:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                DefaultCreatedAt,
                CorrelationId.From("corr-1"),
                searchTerm ?? MusicSearchCriteria.ByIsrc("isrc-1")),
            CancellationToken.None);

    public async Task<MusicCatalogLookupAttempted> HandleDuplicateExecutionCommand(MusicSearchCriteria? searchTerm = null)
    {
        var command = new LookupStreamingLocationsCommand(
            CommandId.For("LookupStreamingLocations:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            DefaultCreatedAt,
            CorrelationId.From("corr-1"),
            searchTerm ?? MusicSearchCriteria.ByIsrc("isrc-1"));
        await Handler.Handle(command, CancellationToken.None);
        return await Handler.Handle(command, CancellationToken.None);
    }

    public void Throw(Exception ex) => GetMusicTrackReference.Throw(ex);
}
