namespace Soundtrail.Services.Tests.Integration.Api.Ports.TrackSearch.KnownQuery
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