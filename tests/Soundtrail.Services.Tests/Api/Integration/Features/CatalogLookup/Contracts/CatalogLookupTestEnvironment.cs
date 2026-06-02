using Raven.Client.Documents;
using System.Reflection;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Tests.Integration.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Tests.Integration.Features.Search.Contracts;
using Soundtrail.Services.Tests.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Features.CatalogLookup.Contracts;

internal sealed class CatalogLookupTestEnvironment : IDisposable
{
    private readonly ISeedCatalogLookup seedableLookup;
    private readonly RavenEmbeddedTestDatabase? raven;

    private CatalogLookupTestEnvironment(
        ICatalogLookupPort lookup,
        ISeedCatalogLookup seedableLookup,
        RavenEmbeddedTestDatabase? raven = null)
    {
        Lookup = lookup;
        this.seedableLookup = seedableLookup;
        this.raven = raven;
    }

    public ICatalogLookupPort Lookup { get; }

    public static CatalogLookupTestEnvironment Create() => CreateRavenEmbedded();

    public void Seed(params Track[] tracks) => this.seedableLookup.Seed(tracks);

    public void Dispose() => raven?.Dispose();

    private static CatalogLookupTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        var lookup = new RavenTrackLookup(raven.Store);
        return new CatalogLookupTestEnvironment(
            lookup,
            new RavenCatalogLookupSeeder(raven.Store),
            raven);
    }
}

internal static class ContractKnownTracks
{
    public static Track MrBrightsideTrack() =>
        new(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            DurationMs.From(222000));
}

internal interface ISeedCatalogLookup
{
    void Seed(params Track[] tracks);
}

internal sealed class RavenCatalogLookupSeeder(IDocumentStore store) : ISeedCatalogLookup
{
    private static readonly Type RavenTrackDocumentType = typeof(RavenTrackLookup).Assembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.RavenTrackDocument", throwOnError: true)!;

    public void Seed(params Track[] tracks)
    {
        using var session = store.OpenSession();

        foreach (var track in tracks)
        {
            var stableId = track.Isrc?.Value
                ?? track.Mbid?.Value
                ?? track.AppleId?.Value
                ?? track.SpotifyId?.Value
                ?? NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}").Value;

            var document = Activator.CreateInstance(
                RavenTrackDocumentType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            Set(document, "Id", $"track-catalogue/{stableId}");
            Set(document, "Title", track.Title.Value);
            Set(document, "Artist", track.Artist.Value);
            Set(document, "SearchText", NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}").Value);
            Set(document, "Isrc", track.Isrc?.Value);
            Set(document, "Mbid", track.Mbid?.Value);
            Set(document, "AppleId", track.AppleId?.Value);
            Set(document, "SpotifyId", track.SpotifyId?.Value);
            Set(document, "DurationMs", track.Duration?.Value);

            session.Store(document);
        }

        session.SaveChanges();
    }

    private static void Set(object target, string propertyName, object? value) =>
        RavenTrackDocumentType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
}
