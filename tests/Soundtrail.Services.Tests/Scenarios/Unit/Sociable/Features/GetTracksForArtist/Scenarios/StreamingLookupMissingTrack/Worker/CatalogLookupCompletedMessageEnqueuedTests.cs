using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.StreamingLookupMissingTrack.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Isrc_Lookup_Throws_Track_Lookup_Not_Ready()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupMissingTrack();

        var act = () => environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        await act.Should().ThrowAsync<TrackLookupNotReadyException>();

        environment.SentMessages<CatalogLookupCompleted>()
            .Where(message =>
                message.Result is LookupResult.Failed failed &&
                environment.SentMessages<LookupStreamingLocationByIsrcMessage>().Any(lookup =>
                    failed.Context.OriginalCommandId == lookup.Id))
            .Should()
            .BeEmpty();
    }
}
