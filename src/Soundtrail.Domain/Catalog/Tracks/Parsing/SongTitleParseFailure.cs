namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public enum SongTitleParseFailure
{
    MissingInput = 1,
    MissingCanonicalMeaning = 2,
    UnclosedReleaseTypeQualifier = 3
}
