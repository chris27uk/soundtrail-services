using Soundtrail.Domain.Common;

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

    public bool RequiresAnyMissing(IEnumerable<ProviderName> availableProviders) =>
        HasProviders && Providers.Any(provider => !availableProviders.Contains(provider));

    public override string ToString() =>
        HasProviders
            ? string.Join(',', Providers.Select(ToPersistentValue))
            : string.Empty;

    private static ProviderName ParseProvider(string provider) =>
        provider switch
        {
            "spotify" => ProviderName.Spotify,
            "appleMusic" => ProviderName.AppleMusic,
            "youtubeMusic" => ProviderName.YoutubeMusic,
            _ => throw new ArgumentException($"Unknown playback provider '{provider}'.", nameof(provider))
        };

    private static string ToPersistentValue(ProviderName provider) =>
        provider.Value switch
        {
            "Spotify" => "spotify",
            "AppleMusic" => "appleMusic",
            "YoutubeMusic" => "youtubeMusic",
            _ => provider.Value
        };
}
