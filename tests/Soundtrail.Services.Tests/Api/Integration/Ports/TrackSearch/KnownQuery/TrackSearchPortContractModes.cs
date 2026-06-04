namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.KnownQuery
{
    public static class TrackSearchPortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [TrackSearchPortMode.InProcessFake],
            [TrackSearchPortMode.RavenEmbedded]
        ];
    }
}