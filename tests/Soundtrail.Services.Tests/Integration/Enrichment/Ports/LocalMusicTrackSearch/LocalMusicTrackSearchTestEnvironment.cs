using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.LocalMusicTrackSearch;

internal sealed class LocalMusicTrackSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<LocalMusicTrackSearchResult> seed;

    private LocalMusicTrackSearchTestEnvironment(
        ILocalMusicTrackSearch search,
        Action<LocalMusicTrackSearchResult> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Search = search;
        this.seed = seed;
        this.raven = raven;
    }

    public ILocalMusicTrackSearch Search { get; }

    public static LocalMusicTrackSearchTestEnvironment Create(LocalMusicTrackSearchMode mode) =>
        mode switch
        {
            LocalMusicTrackSearchMode.InProcessFake => CreateFake(),
            LocalMusicTrackSearchMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(LocalMusicTrackSearchResult result) => seed(result);

    public void Dispose() => raven?.Dispose();

    private static LocalMusicTrackSearchTestEnvironment CreateFake()
    {
        var fake = new LocalMusicTrackSearchFake();
        return new LocalMusicTrackSearchTestEnvironment(fake, fake.Seed, raven: null);
    }

    private static LocalMusicTrackSearchTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new LocalMusicTrackSearchTestEnvironment(
            new RavenLocalMusicTrackSearch(raven.Store),
            result => SeedRaven(raven, result),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, LocalMusicTrackSearchResult result)
    {
        using var session = raven.Store.OpenSession();
        session.Store(new RavenTrackRecordDto
        {
            Id = RavenTrackRecordDto.GetDocumentId(result.MusicCatalogId.Value),
            Title = result.Title ?? string.Empty,
            Artist = result.Artist ?? string.Empty,
            AlbumTitle = result.AlbumTitle,
            SearchText = RavenTrackRecordDto.BuildSearchText(result.Title ?? string.Empty, result.Artist ?? string.Empty),
            Isrc = result.Isrc,
            Mbid = result.Mbid,
            DurationMs = result.DurationMs,
            CanonicalMetadata = !string.IsNullOrWhiteSpace(result.Title)
                                && !string.IsNullOrWhiteSpace(result.Artist)
                ? new RavenSongMetadataRecordDto
                {
                    Title = result.Title!,
                    Artist = result.Artist!,
                    Isrc = result.Isrc,
                    Mbid = result.Mbid,
                    DurationMs = result.DurationMs
                }
                : null,
            IsPlayable = result.IsPlayable
        });
        session.SaveChanges();
    }
}
