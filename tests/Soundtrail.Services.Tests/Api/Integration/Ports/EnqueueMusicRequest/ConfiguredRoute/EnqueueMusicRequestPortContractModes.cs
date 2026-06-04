namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.ConfiguredRoute
{
    public static class EnqueueMusicRequestPortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [EnqueueMusicRequestPortMode.InMemoryFake],
            [EnqueueMusicRequestPortMode.WolverineLocal]
        ];
    }
}