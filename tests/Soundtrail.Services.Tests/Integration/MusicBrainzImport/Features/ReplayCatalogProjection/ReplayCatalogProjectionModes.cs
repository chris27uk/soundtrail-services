namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayCatalogProjection;

public static class ReplayCatalogProjectionModes
{
    public static IReadOnlyList<object[]> All { get; } =
    [
        [ReplayCatalogProjectionMode.InProcessFake],
        [ReplayCatalogProjectionMode.RavenEmbedded]
    ];
}
