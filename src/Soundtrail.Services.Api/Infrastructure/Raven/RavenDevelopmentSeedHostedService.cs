using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenDevelopmentSeedHostedService(
    IDocumentStore documentStore,
    IHostEnvironment hostEnvironment,
    IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await IndexCreation.CreateIndexesAsync(
            typeof(Indexes.TrackCatalogue_BySearchText).Assembly,
            documentStore);

        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        using var session = documentStore.OpenAsyncSession();
        var mrBrightsideTrackId = RavenTrackRecordDto.GetDocumentId("mr-brightside");
        var existingMrBrightside = await session.LoadAsync<RavenTrackRecordDto>(mrBrightsideTrackId, cancellationToken);
        if (existingMrBrightside is null)
        {
            await session.StoreAsync(
                new Track(
                    TrackTitle.From("Mr. Brightside"),
                    ArtistName.From("The Killers"),
                    Isrc.From("USIR20400274"),
                    Mbid.From("mr-brightside-mbid"),
                    AppleId.From("apple-mr-brightside"),
                    SpotifyId.From("spotify-mr-brightside"),
                    DurationMs.From(222000))
                    .ToRecordDto("mr-brightside"),
                cancellationToken);
        }

        if (configuration.GetValue("LocalDevelopment:SeedAsyncLookupTrack", false))
        {
            var asyncLookupTrackId = RavenTrackRecordDto.GetDocumentId("mc_track_1");
            var existingAsyncLookupTrack = await session.LoadAsync<RavenTrackRecordDto>(asyncLookupTrackId, cancellationToken);
            if (existingAsyncLookupTrack is null)
            {
                await session.StoreAsync(
                    new RavenTrackRecordDto
                    {
                        Id = asyncLookupTrackId,
                        Title = "Rare Unknown Song",
                        Artist = "Test Artist",
                        SearchText = RavenTrackRecordDto.BuildSearchText("Rare Unknown Song", "Test Artist")
                    },
                    cancellationToken);
            }
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
