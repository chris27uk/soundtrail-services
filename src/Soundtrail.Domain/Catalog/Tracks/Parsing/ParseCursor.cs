namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public ref struct ParseCursor
{
    private readonly ReadOnlySpan<char> source;
    private int position;

    public ParseCursor(ReadOnlySpan<char> source)
    {
        this.source = source;
        position = 0;
    }

    public bool End => position >= source.Length;

    public int Position => position;

    public char Current => End ? '\0' : source[position];

    public char Lookahead(int offset = 1)
    {
        var index = position + offset;
        return index >= 0 && index < source.Length ? source[index] : '\0';
    }

    public bool Match(char value)
    {
        if (Current != value)
        {
            return false;
        }

        position++;
        return true;
    }

    public void Advance()
    {
        if (!End)
        {
            position++;
        }
    }

    public void SkipWhitespace()
    {
        while (!End && char.IsWhiteSpace(Current))
        {
            position++;
        }
    }

    public ReadOnlySpan<char> Slice(int start, int end) => source[start..end];
}
