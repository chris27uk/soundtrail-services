namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Ports.MusicBrainzDumpReader;

public static class MusicBrainzDumpReaderContractModes
{
    public static IReadOnlyList<object[]> All { get; } =
    [
        [MusicBrainzDumpReaderMode.InProcessFake],
        [MusicBrainzDumpReaderMode.FileSystemJson]
    ];
}
