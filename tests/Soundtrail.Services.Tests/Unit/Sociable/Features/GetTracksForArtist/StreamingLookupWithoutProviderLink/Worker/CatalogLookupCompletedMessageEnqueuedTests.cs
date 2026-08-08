using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.StreamingLookupWithoutProviderLink.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Metadata_Lookup_Result_Is_Not_Found()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupWithoutProviderLink();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        MetadataResult(environment).Should().BeOfType<LookupResult.NotFound>()
            .Which.Reason.Should().Be("Streaming location was not found for the requested provider.");
    }

    private static LookupResult MetadataResult(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message =>
                message.Result is LookupResult.NotFound notFound &&
                notFound.Reason == "Streaming location was not found for the requested provider." &&
                notFound.Context.OriginalCommandId ==
                    environment.SentMessages<LookupStreamingLocationByTrackMetadataMessage>().First().Id)
            .Result;
}
