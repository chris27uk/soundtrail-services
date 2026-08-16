using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class LowPriorityStreamingLocationRequestedTests
{
    [Fact]
    public async Task When_Flushing_Track_Without_Streaming_Locations_Then_Low_Priority_Odesli_Is_Requested()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        var request = environment.CommandBus.SentMessages
            .OfType<RequestKnownMusicDataMessage>()
            .Should()
            .ContainSingle()
            .Subject;
        request.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public async Task When_Flushing_Track_Without_Streaming_Locations_Then_Request_Targets_The_Track()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        environment.CommandBus.SentMessages
            .OfType<RequestKnownMusicDataMessage>()
            .Should()
            .ContainSingle()
            .Subject
            .Operation
            .Should()
            .BeOfType<CatalogItemOperation.StreamingLocationForTrack>()
            .Which.Id.Should().Be(environment.TrackId);
    }
}
