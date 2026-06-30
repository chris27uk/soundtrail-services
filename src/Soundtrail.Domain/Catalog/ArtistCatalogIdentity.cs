using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Catalog;

public static class ArtistCatalogIdentity
{
    public static ArtistId ResolveArtistId(MusicCatalogMetadataFetched fetched)
    {
        if (fetched.Hierarchy?.ArtistId is not null)
        {
            return fetched.Hierarchy.ArtistId.Value;
        }

        var sourceArtistId = fetched.Metadata?.SourceArtistId;
        if (!string.IsNullOrWhiteSpace(sourceArtistId))
        {
            return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(sourceArtistId)}");
        }

        var artistName = fetched.Metadata?.Artist;
        if (!string.IsNullOrWhiteSpace(artistName))
        {
            return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(artistName)}");
        }

        throw new InvalidOperationException("Track metadata lookup must provide hierarchy artist id, source artist id, or artist name.");
    }

    public static ArtistId? ResolveArtistIdOrNull(string? sourceArtistId, string? artistName)
    {
        if (!string.IsNullOrWhiteSpace(sourceArtistId))
        {
            return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(sourceArtistId)}");
        }

        if (!string.IsNullOrWhiteSpace(artistName))
        {
            return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(artistName)}");
        }

        return null;
    }
}
