using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete.Orchestrator;

public sealed class LookupStreamingLocationByIsrcMessageEnqueuedTests
{
    public static TheoryData<ProviderName> Providers => new()
    {
        ProviderName.Spotify,
        ProviderName.AppleMusic,
        ProviderName.YoutubeMusic
    };

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Track_Id_Comes_From_The_Input_Catalog_Track(ProviderName provider)
    {
        var input = InputTrack();
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(input);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).TrackId.Value.Should().Be(((CatalogItem.MusicTrack)input.CatalogEntry.Item).Track.TrackId.Value);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Provider_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Provider.Should().Be(provider);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Has_High_Priority(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Created_Time_Comes_From_The_Request(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 41, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(requestTime, InputTrack(requestTime));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).CreatedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Request_Time_Comes_From_The_Request(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 42, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(requestTime, InputTrack(requestTime));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).RequestedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Id_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Id.Should().NotBe(default(MessageId));
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Correlation_Id_Is_Preserved(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).CorrelationId.Should().Be(StreamingDispatch(environment).CorrelationId);
    }

    private static LookupStreamingLocationByIsrcMessage Message(
        GetTracksForPlaylistSociableTestEnvironment environment,
        ProviderName provider) =>
        environment.SentMessages<LookupStreamingLocationByIsrcMessage>()
            .Single(message => message.Provider == provider);

    private static DispatchLookupWork StreamingDispatch(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier.StartsWith("streaming_location_for_track:", StringComparison.Ordinal));

    private static LookupDataCompleteTrack InputTrack(DateTimeOffset catalogUpdatedAt = default) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            "Playlist Input Artist", "Playlist Input Title", "Catalog Input Artist", "Catalog Input Title",
            "Catalog Input Album", new DateOnly(2025, 3, 4), null, 130000, catalogUpdatedAt);
}
