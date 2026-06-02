using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenDevelopmentSeedHostedService(
    IDocumentStore documentStore,
    IHostEnvironment hostEnvironment) : IHostedService
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
        var trackId = RavenTrackDocument.GetDocumentId("mr-brightside");
        var existing = await session.LoadAsync<RavenTrackDocument>(trackId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await session.StoreAsync(
            new Track(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                DurationMs.From(222000))
                .ToDocument("mr-brightside"),
            cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
