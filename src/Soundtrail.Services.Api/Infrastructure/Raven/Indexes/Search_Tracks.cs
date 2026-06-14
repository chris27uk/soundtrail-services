using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Search_Tracks : AbstractIndexCreationTask<CatalogTrackDocument>
{
    public Search_Tracks()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.TrackId,
                            track.ArtistId,
                            track.AlbumId,
                            track.Title,
                            track.NormalizedTitle,
                            track.ArtistName,
                            track.AlbumName,
                            track.SearchText,
                            track.MusicBrainzRecordingId,
                            track.Isrc,
                            track.AvailableProviders,
                            track.TerminallyUnavailableProviders
                        };

        Index(x => x.Title, FieldIndexing.Search);
        Index(x => x.NormalizedTitle, FieldIndexing.Search);
        Index(x => x.ArtistName, FieldIndexing.Search);
        Index(x => x.AlbumName, FieldIndexing.Search);
        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
