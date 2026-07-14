using System.Net;
using System.Text.RegularExpressions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart.Adapters;

public sealed partial class KworbChartPort(HttpClient httpClient) : IReadKworbChartPort
{
    public const string HttpClientName = "KworbChart";

    [GeneratedRegex(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TableRowPattern();

    [GeneratedRegex(@"<t[dh]\b[^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CellPattern();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TagPattern();

    public async Task<IReadOnlyList<TrackReference>> ReadAsync(CancellationToken cancellationToken)
    {
        var html = await httpClient.GetStringAsync("/ww/", cancellationToken);
        return ParseRows(html);
    }

    internal static IReadOnlyList<TrackReference> ParseRows(string html)
    {
        var chartEntries = new List<(int Position, TrackReference TrackReference)>();

        foreach (Match rowMatch in TableRowPattern().Matches(html))
        {
            var cells = CellPattern()
                .Matches(rowMatch.Groups[1].Value)
                .Select(match => CleanCell(match.Groups[1].Value))
                .ToArray();

            if (cells.Length < 3)
            {
                continue;
            }

            if (!int.TryParse(cells[0], out var position))
            {
                continue;
            }

            if (!TryParseTrackReference(cells[2], out var trackReference))
            {
                continue;
            }

            chartEntries.Add((position, trackReference));
        }

        return chartEntries
            .OrderBy(chartEntry => chartEntry.Position)
            .Select(chartEntry => chartEntry.TrackReference)
            .ToArray();
    }

    private static string CleanCell(string html) =>
        WebUtility.HtmlDecode(TagPattern().Replace(html, " "))
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static bool TryParseTrackReference(string artistAndTitle, out TrackReference trackReference)
    {
        trackReference = new TrackReference(ArtistName.Empty, string.Empty);

        var separatorIndex = artistAndTitle.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= artistAndTitle.Length - 3)
        {
            return false;
        }

        var artistName = ArtistName.From(artistAndTitle[..separatorIndex]);
        var trackTitle = artistAndTitle[(separatorIndex + 3)..].Trim();

        if (!artistName.HasValue || string.IsNullOrWhiteSpace(trackTitle))
        {
            return false;
        }

        trackReference = new TrackReference(artistName, trackTitle);
        return true;
    }
}
