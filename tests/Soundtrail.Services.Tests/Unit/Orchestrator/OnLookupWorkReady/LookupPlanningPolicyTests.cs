using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupWorkReady;

public sealed class LookupPlanningPolicyTests
{
    [Fact]
    public void Given_A_Search_Target_When_Building_A_Plan_Then_A_Musicbrainz_Search_Is_Planned()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreateSearchRequest());

        var lookup = plan.Lookups.Should().ContainSingle().Subject.Should().BeOfType<PlannedLookup.MusicbrainzSearch>().Subject;
        lookup.SearchCriteria.Should().Be(new SearchCriteria("u2", SearchType.Artist));
        lookup.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public void Given_A_Streaming_Location_Target_When_Building_A_Plan_Then_Isrc_And_Metadata_Are_Planned_For_Each_Provider()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest());

        plan.Lookups.Should().HaveCount(ProviderName.All.Length * 2);
        plan.Lookups.OfType<PlannedLookup.StreamingLocationByIsrc>().Select(x => x.Provider)
            .Should().BeEquivalentTo(ProviderName.All);
        plan.Lookups.OfType<PlannedLookup.StreamingLocationByTrackMetadata>().Select(x => x.Provider)
            .Should().BeEquivalentTo(ProviderName.All);
    }

    [Fact]
    public void Given_A_Playlist_Target_When_Building_A_Plan_Then_Playlist_Tracks_Are_Planned_For_Each_Provider()
    {
        var plan = LookupPlanningPolicy.Build(
            LookupWorkReadyHandlerUnitTestEnvironment.CreatePlaylistRequest());

        plan.Lookups.Should().HaveCount(ProviderName.All.Length);
        plan.Lookups.Should().AllBeOfType<PlannedLookup.PlaylistTracksByProvider>();
    }

    [Fact]
    public void Given_An_Artist_Albums_Target_When_Building_A_Plan_Then_An_Artist_Albums_Lookup_Is_Planned()
    {
        var request = new DispatchLookupWork(
            new EnrichmentTarget.KnownCatalogItemOperation(
                new CatalogItemOperation.ChildAlbumsForArtist(Soundtrail.Domain.Catalog.Artists.ArtistId.From("artist-42"))),
            LookupPriorityBand.High,
            CommandId.For("cmd-artist-albums"),
            CorrelationId.From("corr-artist-albums"),
            new DateTimeOffset(2026, 7, 18, 9, 20, 0, TimeSpan.Zero));

        var plan = LookupPlanningPolicy.Build(request);

        var lookup = plan.Lookups.Should().ContainSingle().Subject.Should().BeOfType<PlannedLookup.MusicbrainzArtistAlbums>().Subject;
        lookup.ArtistId.Value.Should().Be("artist-42");
    }
}
