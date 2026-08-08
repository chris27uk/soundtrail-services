using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete.Orchestrator;

public sealed class LookupPlaylistTracksByProviderMessageEnqueuedTests
{
    public static TheoryData<ProviderName> Providers => new()
    {
        ProviderName.Spotify,
        ProviderName.AppleMusic,
        ProviderName.YoutubeMusic
    };

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Id_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Id.Should().NotBe(default(MessageId));
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Correlation_Id_Is_Preserved(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).CorrelationId.Should().Be(environment.SentMessages<DispatchLookupWork>().First().CorrelationId);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Created_Time_Is_Set(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 41, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(requestTime, MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).CreatedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Request_Time_Is_Set(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 42, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(requestTime, MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).RequestedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Has_High_Priority(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Playlist_Id_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).PlaylistId.Should().Be(environment.PlaylistId);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Requested_Provider_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment, provider).Provider.Should().Be(provider);
    }

    private static LookupPlaylistTracksByProviderMessage Message(
        GetTracksForPlaylistSociableTestEnvironment environment,
        ProviderName provider) =>
        environment.SentMessages<LookupPlaylistTracksByProviderMessage>().Single(message => message.Provider == provider);

    private static LookupDataCompleteTrack MidnightSignals() =>
        LookupDataCompleteTrackScenarios.MidnightSignals(default);
}
