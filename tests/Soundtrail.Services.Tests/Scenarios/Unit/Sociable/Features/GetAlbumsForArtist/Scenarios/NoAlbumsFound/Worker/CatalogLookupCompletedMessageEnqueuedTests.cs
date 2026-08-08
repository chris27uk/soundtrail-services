using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.NoAlbumsFound.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoAlbumsFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_No_Catalog_Entries()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoAlbumsFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        CatalogEntries(environment).Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Artist()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoAlbumsFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Result(environment).Context.StreamId.StableValue
            .Should().Be($"child_albums_for_artist:{environment.ArtistId.Value}");
    }

    private static CatalogLookupCompleted Message(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.StreamId.StableValue ==
                    $"child_albums_for_artist:{environment.ArtistId.Value}");

    private static LookupResult.Succeeded Result(GetAlbumsForArtistSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(GetAlbumsForArtistSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
