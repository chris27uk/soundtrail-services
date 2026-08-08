using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.LookupDataComplete.Orchestrator;

public sealed class LookupStreamingLocationByTrackMetadataMessageEnqueuedTests
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
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(input);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).TrackId.Value
            .Should().Be(((CatalogItem.MusicTrack)input.CatalogEntry.Item).Track.TrackId.Value);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Provider_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).Provider.Should().Be(provider);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Has_High_Priority(ProviderName provider)
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Created_Time_Comes_From_The_Request(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 41, 0, TimeSpan.Zero);
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(requestTime, InputTrack(requestTime));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).CreatedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Request_Time_Comes_From_The_Request(ProviderName provider)
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 42, 0, TimeSpan.Zero);
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(requestTime, InputTrack(requestTime));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).RequestedAt.Should().Be(requestTime);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Message_Id_Is_Set(ProviderName provider)
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).Id.Should().NotBe(default(MessageId));
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Then_The_Correlation_Id_Is_Preserved(ProviderName provider)
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(InputTrack());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment, provider).CorrelationId.Should().Be(StreamingDispatch(environment).CorrelationId);
    }

    private static LookupStreamingLocationByTrackMetadataMessage Message(
        GetTracksForArtistSociableTestEnvironment environment,
        ProviderName provider) =>
        environment.SentMessages<LookupStreamingLocationByTrackMetadataMessage>()
            .Single(message => message.Provider == provider);

    private static DispatchLookupWork StreamingDispatch(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier.StartsWith("streaming_location_for_track:", StringComparison.Ordinal));

    private static LookupDataCompleteArtistTrack InputTrack(DateTimeOffset catalogUpdatedAt = default) =>
        LookupDataCompleteArtistTrack.Create(
            LookupDataCompleteArtistTrackScenarios.DefaultArtistId,
            "Artist Input Artist",
            "Artist Input Title",
            "Artist Input Album",
            new DateOnly(2025, 3, 4),
            null,
            130000,
            catalogUpdatedAt);
}
