using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupWorkReady;

public sealed class LookupPlanningPolicyTests
{
    [Fact]
    public void Given_A_Search_Target_When_Building_A_Plan_Then_A_Musicbrainz_Search_Is_Planned()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreateSearchRequest());

        var intent = plan.Intents.Should().ContainSingle().Subject.Should().BeOfType<LookupIntent.SearchCatalogItems>().Subject;
        intent.SearchCriteria.Should().Be(new SearchCriteria("u2", SearchType.Artist));
        intent.Priority.Should().Be(LookupPriorityBand.High);
        intent.Attempts.Should().ContainSingle().Which.Should().BeOfType<LookupAttempt.MusicbrainzSearchCatalogItems>();
    }

    [Fact]
    public void Given_A_Streaming_Location_Target_When_Building_A_Plan_Then_Isrc_And_Metadata_Are_Planned_For_Each_Provider()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest());

        var intent = plan.Intents.Should().ContainSingle().Subject.Should().BeOfType<LookupIntent.StreamingLocation>().Subject;

        intent.Attempts.Should().HaveCount(ProviderName.All.Length * 2);
        intent.Attempts.OfType<LookupAttempt.StreamingLocationByIsrc>().Select(x => x.Provider)
            .Should().BeEquivalentTo(ProviderName.All);
        intent.Attempts.OfType<LookupAttempt.StreamingLocationByTrackMetadata>().Select(x => x.Provider)
            .Should().BeEquivalentTo(ProviderName.All);
    }

    [Fact]
    public void Given_A_Playlist_Target_When_Building_A_Plan_Then_Playlist_Tracks_Are_Planned_For_Each_Provider()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreatePlaylistRequest());

        var intent = plan.Intents.Should().ContainSingle().Subject.Should().BeOfType<LookupIntent.PlaylistTracks>().Subject;

        intent.Attempts.Should().HaveCount(ProviderName.All.Length);
        intent.Attempts.Should().AllBeOfType<LookupAttempt.PlaylistTracksByProvider>();
    }

    [Fact]
    public void Given_An_Artist_Albums_Target_When_Building_A_Plan_Then_An_Artist_Albums_Lookup_Is_Planned()
    {
        var request = new DispatchLookupWork(
            new EnrichmentTarget.KnownCatalogItemOperation(
                new CatalogItemOperation.ChildAlbumsForArtist(Soundtrail.Domain.Catalog.Artists.ArtistId.From("artist-42"))),
            LookupPriorityBand.High,
            MessageId.For("cmd-artist-albums"),
            CorrelationId.From("corr-artist-albums"),
            new DateTimeOffset(2026, 7, 18, 9, 20, 0, TimeSpan.Zero));

        var plan = LookupPlanningPolicy.Build(request);

        var intent = plan.Intents.Should().ContainSingle().Subject.Should().BeOfType<LookupIntent.ArtistAlbums>().Subject;
        intent.ArtistId.Value.Should().Be("artist-42");
        intent.Attempts.Should().ContainSingle().Which.Should().BeOfType<LookupAttempt.MusicbrainzArtistAlbums>();
    }
}
