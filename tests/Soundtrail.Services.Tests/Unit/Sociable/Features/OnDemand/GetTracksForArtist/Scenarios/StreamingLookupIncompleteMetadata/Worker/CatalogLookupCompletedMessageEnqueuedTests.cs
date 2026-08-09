using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.StreamingLookupIncompleteMetadata.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Metadata_Lookup_Throws_Track_Lookup_Not_Ready()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupIncompleteMetadata();

        var act = () => environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        await act.Should().ThrowAsync<TrackLookupNotReadyException>();

        environment.SentMessages<CatalogLookupCompleted>()
            .Where(message =>
                message.Result is LookupResult.NotFound notFound &&
                notFound.Reason == "Track metadata is incomplete for provider lookup." &&
                environment.SentMessages<LookupStreamingLocationByTrackMetadataMessage>().Any(lookup =>
                    notFound.Context.OriginalCommandId == lookup.Id))
            .Should()
            .BeEmpty();
    }
}
