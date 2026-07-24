namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public static class SongTitleParser
{
    public static SongTitleParseResult Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new SongTitleParseResult.Failure(SongTitleParseFailure.MissingInput);
        }

        var trimmed = value.Trim();
        var parser = new ParseCursor(trimmed.AsSpan());

        var rawSongTitle = ParseSongTitle(ref parser).Trim();
        parser.SkipWhitespace();

        if (parser.End)
        {
            return BuildSuccessOrFailure(
                SongTitle.From(rawSongTitle),
                null);
        }

        var separator = ParseOpenReleaseTypeSeparator(ref parser);
        if (separator == ReleaseTypeSeparator.None)
        {
            return BuildSuccessOrFailure(
                SongTitle.From(trimmed),
                null);
        }

        var rawReleaseType = ParseReleaseType(ref parser).Trim();
        if (!ParseCloseReleaseTypeSeparator(ref parser, separator))
        {
            var fallbackSongTitle = SongTitle.From(rawSongTitle);
            return string.IsNullOrWhiteSpace(fallbackSongTitle.Value)
                ? new SongTitleParseResult.Failure(SongTitleParseFailure.MissingCanonicalMeaning)
                : new SongTitleParseResult.Failure(SongTitleParseFailure.UnclosedReleaseTypeQualifier);
        }

        parser.SkipWhitespace();

        if (!parser.End || !ReleaseTypeVocabulary.IsRecognised(rawReleaseType))
        {
            return BuildSuccessOrFailure(
                SongTitle.From(trimmed),
                null);
        }

        return BuildSuccessOrFailure(
            SongTitle.From(rawSongTitle),
            ReleaseTypeVocabulary.Normalize(rawReleaseType));
    }

    private static string ParseSongTitle(ref ParseCursor parser)
    {
        var start = parser.Position;

        while (!parser.End)
        {
            if (IsReleaseTypeStart(ref parser))
            {
                break;
            }

            parser.Advance();
        }

        return parser.Slice(start, parser.Position).ToString();
    }

    private static ReleaseTypeSeparator ParseOpenReleaseTypeSeparator(ref ParseCursor parser)
    {
        parser.SkipWhitespace();

        if (parser.Match('('))
        {
            return ReleaseTypeSeparator.Parenthesis;
        }

        if (parser.Match('['))
        {
            return ReleaseTypeSeparator.Bracket;
        }

        if (parser.Match('-'))
        {
            parser.SkipWhitespace();
            return ReleaseTypeSeparator.Hyphen;
        }

        return ReleaseTypeSeparator.None;
    }

    private static bool ParseCloseReleaseTypeSeparator(ref ParseCursor parser, ReleaseTypeSeparator separator)
    {
        parser.SkipWhitespace();

        switch (separator)
        {
            case ReleaseTypeSeparator.Parenthesis:
                return parser.Match(')');

            case ReleaseTypeSeparator.Bracket:
                return parser.Match(']');

            case ReleaseTypeSeparator.Hyphen:
            case ReleaseTypeSeparator.None:
                return true;
        }

        return true;
    }

    private static string ParseReleaseType(ref ParseCursor parser)
    {
        var start = parser.Position;

        while (!parser.End)
        {
            if (parser.Current is ')' or ']')
            {
                break;
            }

            parser.Advance();
        }

        return parser.Slice(start, parser.Position).ToString();
    }

    private static bool IsReleaseTypeStart(ref ParseCursor parser)
    {
        if (parser.Current == '-')
        {
            return true;
        }

        if (parser.Current is not ('(' or '['))
        {
            return false;
        }

        var next = parser.Lookahead();
        return next != '\0' && !char.IsWhiteSpace(next);
    }

    private enum ReleaseTypeSeparator
    {
        None = 0,
        Parenthesis = 1,
        Bracket = 2,
        Hyphen = 3
    }

    private static SongTitleParseResult BuildSuccessOrFailure(
        SongTitle canonicalSongTitle,
        ReleaseType? canonicalReleaseType)
    {
        if (string.IsNullOrWhiteSpace(canonicalSongTitle.Value))
        {
            return new SongTitleParseResult.Failure(SongTitleParseFailure.MissingCanonicalMeaning);
        }

        return new SongTitleParseResult.Success(
            new CanonicalSongTitle(canonicalSongTitle, canonicalReleaseType));
    }
}
