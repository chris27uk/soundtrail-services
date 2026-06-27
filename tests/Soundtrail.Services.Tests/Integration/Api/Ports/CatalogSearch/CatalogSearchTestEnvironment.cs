using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch;

internal sealed class CatalogSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<SearchCatalogResult[]> seed;

    private CatalogSearchTestEnvironment(
        ICatalogSearchPort search,
        Action<SearchCatalogResult[]> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Search = search;
        this.seed = seed;
        this.raven = raven;
    }

    public ICatalogSearchPort Search { get; }

    public static CatalogSearchTestEnvironment Create(CatalogSearchPortMode mode) =>
        mode switch
        {
            CatalogSearchPortMode.InProcessFake => CreateFake(),
            CatalogSearchPortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(params SearchCatalogResult[] results) => seed(results);

    public void Dispose() => raven?.Dispose();

    private static CatalogSearchTestEnvironment CreateFake()
    {
        var fake = new FakeCatalogSearchPort();
        return new CatalogSearchTestEnvironment(fake, fake.Seed, raven: null);
    }

    private static CatalogSearchTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteIndexes(raven.Store);
        return new CatalogSearchTestEnvironment(
            new RavenCatalogSearch(raven.Store),
            results => SeedRaven(raven, results),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, params SearchCatalogResult[] results)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var result in results)
        {
            switch (result.Type)
            {
                case SearchResultType.Artist:
                    var artist = Activator.CreateInstance(CatalogArtistRecordDtoType)!;
                    Set(artist, CatalogArtistRecordDtoType, "Id", $"catalog/artists/{result.Id}");
                    Set(artist, CatalogArtistRecordDtoType, "ArtistId", result.Id);
                    Set(artist, CatalogArtistRecordDtoType, "Name", result.Name);
                    Set(artist, CatalogArtistRecordDtoType, "NormalizedName", result.Name.ToLowerInvariant());
                    Set(artist, CatalogArtistRecordDtoType, "SearchText", $"{result.Name}".ToLowerInvariant());
                    Set(artist, CatalogArtistRecordDtoType, "MusicBrainzArtistId", null);
                    Set(artist, CatalogArtistRecordDtoType, "AvailableProviders", result.AvailableProviders.Select(x => x.Value).ToArray());
                    Set(artist, CatalogArtistRecordDtoType, "TerminallyUnavailableProviders", result.TerminallyUnavailableProviders.Select(x => x.Value).ToArray());
                    Set(artist, CatalogArtistRecordDtoType, "ArtworkUrl", null);
                    Set(artist, CatalogArtistRecordDtoType, "UpdatedAt", DateTimeOffset.UtcNow);
                    session.Store(artist);
                    break;

                case SearchResultType.Album:
                    var album = Activator.CreateInstance(CatalogAlbumRecordDtoType)!;
                    Set(album, CatalogAlbumRecordDtoType, "Id", $"catalog/albums/{result.Id}");
                    Set(album, CatalogAlbumRecordDtoType, "ArtistId", result.ArtistId!);
                    Set(album, CatalogAlbumRecordDtoType, "AlbumId", result.Id);
                    Set(album, CatalogAlbumRecordDtoType, "Name", result.Name);
                    Set(album, CatalogAlbumRecordDtoType, "NormalizedName", result.Name.ToLowerInvariant());
                    Set(album, CatalogAlbumRecordDtoType, "ArtistName", result.ArtistName!);
                    Set(album, CatalogAlbumRecordDtoType, "SearchText", $"{result.Name} {result.ArtistName}".ToLowerInvariant());
                    Set(album, CatalogAlbumRecordDtoType, "MusicBrainzReleaseId", null);
                    Set(album, CatalogAlbumRecordDtoType, "AvailableProviders", result.AvailableProviders.Select(x => x.Value).ToArray());
                    Set(album, CatalogAlbumRecordDtoType, "TerminallyUnavailableProviders", result.TerminallyUnavailableProviders.Select(x => x.Value).ToArray());
                    Set(album, CatalogAlbumRecordDtoType, "ArtworkUrl", null);
                    Set(album, CatalogAlbumRecordDtoType, "ReleaseDate", null);
                    Set(album, CatalogAlbumRecordDtoType, "UpdatedAt", DateTimeOffset.UtcNow);
                    session.Store(album);
                    break;

                case SearchResultType.Track:
                    var track = Activator.CreateInstance(CatalogTrackRecordDtoType)!;
                    Set(track, CatalogTrackRecordDtoType, "Id", $"catalog/tracks/{result.Id}");
                    Set(track, CatalogTrackRecordDtoType, "ArtistId", result.ArtistId!);
                    Set(track, CatalogTrackRecordDtoType, "AlbumId", result.AlbumId!);
                    Set(track, CatalogTrackRecordDtoType, "TrackId", result.Id);
                    Set(track, CatalogTrackRecordDtoType, "Title", result.Name);
                    Set(track, CatalogTrackRecordDtoType, "NormalizedTitle", result.Name.ToLowerInvariant());
                    Set(track, CatalogTrackRecordDtoType, "ArtistName", result.ArtistName!);
                    Set(track, CatalogTrackRecordDtoType, "AlbumName", result.AlbumName!);
                    Set(track, CatalogTrackRecordDtoType, "SearchText", $"{result.Name} {result.ArtistName}".ToLowerInvariant());
                    Set(track, CatalogTrackRecordDtoType, "MusicBrainzRecordingId", null);
                    Set(track, CatalogTrackRecordDtoType, "Isrc", null);
                    Set(track, CatalogTrackRecordDtoType, "DurationMs", null);
                    Set(track, CatalogTrackRecordDtoType, "AvailableProviders", result.AvailableProviders.Select(x => x.Value).ToArray());
                    Set(track, CatalogTrackRecordDtoType, "TerminallyUnavailableProviders", result.TerminallyUnavailableProviders.Select(x => x.Value).ToArray());
                    Set(track, CatalogTrackRecordDtoType, "ProviderReferences", CreateProviderReferences(result.ProviderReferences));
                    Set(track, CatalogTrackRecordDtoType, "ArtworkUrl", null);
                    Set(track, CatalogTrackRecordDtoType, "UpdatedAt", DateTimeOffset.UtcNow);
                    session.Store(track);
                    break;
            }
        }

        session.SaveChanges();
    }

    private static void ExecuteIndexes(IDocumentStore store)
    {
        foreach (var type in IndexTypes)
        {
            var index = (AbstractIndexCreationTask)Activator.CreateInstance(type)!;
            index.Execute(store);
        }
    }

    private static void Set(object target, Type targetType, string propertyName, object? value) =>
        targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static Array CreateProviderReferences(IReadOnlyList<ProviderReference> references)
    {
        var array = Array.CreateInstance(CatalogProviderReferenceRecordDtoType, references.Count);

        for (var index = 0; index < references.Count; index++)
        {
            var dto = Activator.CreateInstance(CatalogProviderReferenceRecordDtoType)!;
            Set(dto, CatalogProviderReferenceRecordDtoType, "Provider", references[index].Provider.Value);
            Set(dto, CatalogProviderReferenceRecordDtoType, "ProviderEntityType", references[index].ProviderEntityType);
            Set(dto, CatalogProviderReferenceRecordDtoType, "ProviderId", references[index].ProviderId);
            Set(dto, CatalogProviderReferenceRecordDtoType, "Url", references[index].Url.ToString());
            Set(dto, CatalogProviderReferenceRecordDtoType, "DiscoveredAt", references[index].DiscoveredAt);
            array.SetValue(dto, index);
        }

        return array;
    }

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;

    private static readonly Type CatalogArtistRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogArtistRecordDto);

    private static readonly Type CatalogAlbumRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogAlbumRecordDto);

    private static readonly Type CatalogTrackRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogTrackRecordDto);

    private static readonly Type CatalogProviderReferenceRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogProviderReferenceRecordDto);

    private static readonly IReadOnlyList<Type> IndexTypes =
    [
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Artists", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Albums", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Tracks", true)!
    ];
}
