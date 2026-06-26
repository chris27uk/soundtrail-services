using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
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

    public void Seed(MusicSearchTerm lookupKey, params ExternalReference[] references) =>
        GetMusicTrackReference.Seed(lookupKey, references);

    public Task<Soundtrail.Domain.Responses.MusicCatalogLookupAttempted> HandleNewExecutionCommand(MusicSearchTerm? searchTerm = null) =>
        Handler.Handle(
            new LookupStreamingLocationsCommand(
                CommandId.For("LookupStreamingLocations:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                DefaultCreatedAt,
                CorrelationId.From("corr-1"),
                searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1")),
            CancellationToken.None);

    public async Task<Soundtrail.Domain.Responses.MusicCatalogLookupAttempted> HandleDuplicateExecutionCommand(MusicSearchTerm? searchTerm = null)
    {
        var command = new LookupStreamingLocationsCommand(
            CommandId.For("LookupStreamingLocations:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            DefaultCreatedAt,
            CorrelationId.From("corr-1"),
            searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1"));
        await Handler.Handle(command, CancellationToken.None);
        return await Handler.Handle(command, CancellationToken.None);
    }

    public void Throw(Exception ex) => GetMusicTrackReference.Throw(ex);
}
