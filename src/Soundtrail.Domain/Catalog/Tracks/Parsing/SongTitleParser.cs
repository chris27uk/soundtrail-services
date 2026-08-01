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
        while (!parser.End)
        {
            if (!IsReleaseTypeStart(ref parser))
            {
                parser.Advance();
                continue;
            }

            var separatorStart = parser.Position;
            var candidate = TryParseTrailingReleaseType(trimmed.AsSpan(), separatorStart);
            if (candidate is ParsedSongTitleCandidate.Success success)
            {
                return BuildSuccessOrFailure(
                    SongTitle.From(success.Value.RawSongTitle),
                    ReleaseTypeVocabulary.Normalize(success.Value.RawReleaseType));
            }

            if (candidate is ParsedSongTitleCandidate.Failure failure)
            {
                var fallbackSongTitle = SongTitle.From(trimmed[..separatorStart]);
                return string.IsNullOrWhiteSpace(fallbackSongTitle.Value)
                    ? new SongTitleParseResult.Failure(SongTitleParseFailure.MissingCanonicalMeaning)
                    : new SongTitleParseResult.Failure(failure.Reason);
            }

            parser.Advance();
        }

        return BuildSuccessOrFailure(SongTitle.From(trimmed), null);
    }

    private static ParsedSongTitleCandidate TryParseTrailingReleaseType(
        ReadOnlySpan<char> source,
        int separatorStart)
    {
        var parser = new ParseCursor(source[separatorStart..]);
        var separator = ParseOpenReleaseTypeSeparator(ref parser);
        if (separator == ReleaseTypeSeparator.None)
        {
            return new ParsedSongTitleCandidate.None();
        }

        var rawReleaseType = ParseReleaseType(ref parser).Trim();
        if (!ParseCloseReleaseTypeSeparator(ref parser, separator))
        {
            return new ParsedSongTitleCandidate.Failure(SongTitleParseFailure.UnclosedReleaseTypeQualifier);
        }

        parser.SkipWhitespace();
        if (!parser.End || !ReleaseTypeVocabulary.IsRecognised(rawReleaseType))
        {
            return new ParsedSongTitleCandidate.None();
        }

        var rawSongTitle = source[..separatorStart].ToString().Trim();
        return new ParsedSongTitleCandidate.Success(
            new ParsedSongTitle(rawSongTitle, rawReleaseType));
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

    private sealed record ParsedSongTitle(
        string RawSongTitle,
        string RawReleaseType);

    private abstract record ParsedSongTitleCandidate
    {
        public sealed record None : ParsedSongTitleCandidate;

        public sealed record Success(ParsedSongTitle Value) : ParsedSongTitleCandidate;

        public sealed record Failure(SongTitleParseFailure Reason) : ParsedSongTitleCandidate;
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
