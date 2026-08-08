using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForAlbum.NoTracksFound.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForNoTracksFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_No_Catalog_Entries()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForNoTracksFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        CatalogEntries(environment).Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Artist()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForNoTracksFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Result(environment).Context.StreamId.StableValue
            .Should().Be($"child_tracks_for_album:{environment.AlbumId.StableValue}");
    }

    private static CatalogLookupCompleted Message(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.StreamId.StableValue ==
                    $"child_tracks_for_album:{environment.AlbumId.StableValue}");

    private static LookupResult.Succeeded Result(GetTracksForAlbumSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(GetTracksForAlbumSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
