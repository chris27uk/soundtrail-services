namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest.ConfiguredRoute
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