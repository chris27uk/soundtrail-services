using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.StreamingLookupIncompleteMetadata.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Metadata_Lookup_Result_Is_Not_Found()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupIncompleteMetadata();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        MetadataResult(environment).Should().BeOfType<LookupResult.NotFound>()
            .Which.Reason.Should().Be("Track metadata is incomplete for provider lookup.");
    }

    private static LookupResult MetadataResult(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message =>
                message.Result is LookupResult.NotFound notFound &&
                notFound.Reason == "Track metadata is incomplete for provider lookup." &&
                notFound.Context.OriginalCommandId ==
                    environment.SentMessages<LookupStreamingLocationByTrackMetadataMessage>().First().Id)
            .Result;
}
