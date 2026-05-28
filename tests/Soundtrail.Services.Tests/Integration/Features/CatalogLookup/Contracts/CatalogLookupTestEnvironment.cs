using Soundtrail.Services.Api.Infrastructure.TableStorage;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Tests.Integration.Features.Search.Contracts;

namespace Soundtrail.Services.Tests.Integration.Features.CatalogLookup.Contracts;

internal sealed class CatalogLookupTestEnvironment
{
    private readonly ISeedCatalogLookup seedableLookup;

    private CatalogLookupTestEnvironment(ICatalogLookupPort lookup, ISeedCatalogLookup seedableLookup)
    {
        Lookup = lookup;
        this.seedableLookup = seedableLookup;
    }

    public ICatalogLookupPort Lookup { get; }

    public static CatalogLookupTestEnvironment Create(StorageMode mode) =>
        mode switch
        {
            StorageMode.Fake => Create(new FakeCatalogLookupPort()),
            StorageMode.AzureTable => CreateAzureTable(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(params Track[] tracks) => this.seedableLookup.Seed(tracks);

    private static CatalogLookupTestEnvironment Create<TLookup>(TLookup lookup)
        where TLookup : ICatalogLookupPort, ISeedCatalogLookup =>
        new(lookup, lookup);

    private static CatalogLookupTestEnvironment CreateAzureTable()
    {
        var lookup = new AzureTableTrackLookup();
        return new CatalogLookupTestEnvironment(
            lookup,
            new AzureTableCatalogLookupSeeder(lookup));
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

internal sealed class AzureTableCatalogLookupSeeder(AzureTableTrackLookup lookup) : ISeedCatalogLookup
{
    public void Seed(params Track[] tracks) => lookup.Seed(tracks);
}
