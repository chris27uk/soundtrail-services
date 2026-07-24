using Dunet;

namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

[Union]
public partial record SongTitleParseResult
{
    public partial record Success(CanonicalSongTitle Value);

    public partial record Failure(SongTitleParseFailure Reason);
}
