using Raven.Client.Documents;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Support;

internal sealed class CatalogReadTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action seed;

    private CatalogReadTestEnvironment(ICatalogReadPort port, Action seed, RavenEmbeddedTestDatabase? raven)
    {
        Port = port;
        this.seed = seed;
        this.raven = raven;
    }

    public ICatalogReadPort Port { get; }

    public static CatalogReadTestEnvironment Create(CatalogReadPortMode mode) =>
        mode switch
        {
            CatalogReadPortMode.InProcessFake => CreateFake(),
            CatalogReadPortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed() => this.seed();

    public void Dispose() => this.raven?.Dispose();

    private static CatalogReadTestEnvironment CreateFake()
    {
        var fake = new FakeCatalogReadPort();
        return new CatalogReadTestEnvironment(
            fake,
            () => fake.Seed(
                ApiKnownTracks.TheKillersArtistDetails(),
                ApiKnownTracks.HotFussAlbum(),
                ApiKnownTracks.MrBrightsideTrackDetails(),
                [ApiKnownTracks.MrBrightsideTrackSummary()],
                [ApiKnownTracks.MrBrightsideTrackSummary()]),
            null);
    }

    private static CatalogReadTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new CatalogReadTestEnvironment(
            new RavenCatalogReadPort(raven.Store),
            () => SeedRaven(raven.Store),
            raven);
    }

    private static void SeedRaven(IDocumentStore store)
    {
        using var session = store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        session.Store(Create(ArtistType, new Dictionary<string, object?>
        {
            ["Id"] = "catalog/artists/artist_the_killers",
            ["ArtistId"] = "artist_the_killers",
            ["Name"] = "The Killers",
            ["NormalizedName"] = "the killers",
            ["AvailableProviders"] = new[] { "Spotify", "AppleMusic" },
            ["TerminallyUnavailableProviders"] = Array.Empty<string>(),
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        }));

        session.Store(Create(AlbumType, new Dictionary<string, object?>
        {
            ["Id"] = "catalog/albums/album_hot_fuss",
            ["ArtistId"] = "artist_the_killers",
            ["AlbumId"] = "album_hot_fuss",
            ["Name"] = "Hot Fuss",
            ["NormalizedName"] = "hot fuss",
            ["ArtistName"] = "The Killers",
            ["AvailableProviders"] = new[] { "Spotify", "AppleMusic" },
            ["TerminallyUnavailableProviders"] = Array.Empty<string>(),
            ["ReleaseDate"] = new DateOnly(2004, 6, 7),
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        }));

        session.Store(Create(TrackType, new Dictionary<string, object?>
        {
            ["Id"] = "catalog/tracks/track_mr_brightside",
            ["ArtistId"] = "artist_the_killers",
            ["AlbumId"] = "album_hot_fuss",
            ["TrackId"] = "track_mr_brightside",
            ["Title"] = "Mr. Brightside",
            ["NormalizedTitle"] = "mr brightside",
            ["ArtistName"] = "The Killers",
            ["AlbumName"] = "Hot Fuss",
            ["SearchText"] = "mr brightside the killers",
            ["Isrc"] = "USIR20400274",
            ["DurationMs"] = 222000,
            ["AvailableProviders"] = new[] { "Spotify", "AppleMusic" },
            ["TerminallyUnavailableProviders"] = Array.Empty<string>(),
            ["ProviderReferences"] = Array.CreateInstance(CatalogProviderReferenceType, 0),
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        }));

        session.SaveChanges();
    }

    private static object Create(Type type, IReadOnlyDictionary<string, object?> values)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var pair in values)
        {
            type.GetProperty(pair.Key)!.SetValue(instance, pair.Value);
        }

        return instance;
    }

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;
    private static readonly Type ArtistType = ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogArtistRecordDto", true)!;
    private static readonly Type AlbumType = ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogAlbumRecordDto", true)!;
    private static readonly Type TrackType = ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogTrackRecordDto", true)!;
    private static readonly Type CatalogProviderReferenceType = ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogProviderReferenceRecordDto", true)!;
}
