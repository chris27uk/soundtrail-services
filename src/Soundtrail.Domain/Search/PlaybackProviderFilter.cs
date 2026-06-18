using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Search;

public sealed record PlaybackProviderFilter(IReadOnlyList<ProviderName> Providers)
{
    public static PlaybackProviderFilter Empty { get; } = new([]);

    public bool HasProviders => Providers.Count > 0;

    public static PlaybackProviderFilter Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Empty;
        }

        var providers = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseProvider)
            .Distinct()
            .ToArray();

        return new PlaybackProviderFilter(providers);
    }

    public bool AllowsAny(IEnumerable<ProviderName> availableProviders) =>
        !HasProviders || availableProviders.Any(provider => Providers.Contains(provider));

    private static ProviderName ParseProvider(string provider) =>
        provider switch
        {
            "spotify" => ProviderName.Spotify,
            "appleMusic" => ProviderName.AppleMusic,
            "youtubeMusic" => ProviderName.YoutubeMusic,
            _ => throw new ArgumentException($"Unknown playback provider '{provider}'.", nameof(provider))
        };
}
