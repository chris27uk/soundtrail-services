using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Catalog;

public static class ArtistCatalogIdentity
{
    public static ArtistId? ResolveArtistIdOrNull(MusicCatalogMetadataFetched fetched)
    {
        if (fetched.Hierarchy?.ArtistId is not null)
        {
            return fetched.Hierarchy.ArtistId.Value;
        }

        return ResolveArtistIdOrNull(
            fetched.Metadata?.SourceArtistId,
            fetched.Metadata?.Artist);
    }

    public static ArtistId ResolveArtistId(MusicCatalogMetadataFetched fetched)
    {
        var resolved = ResolveArtistIdOrNull(fetched);
        if (resolved is not null)
        {
            return resolved.Value;
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
