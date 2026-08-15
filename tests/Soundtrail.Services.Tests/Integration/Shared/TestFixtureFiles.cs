namespace Soundtrail.Services.Tests.Integration.Shared;

internal static class TestFixtureFiles
{
    public static string ReadAllText(params string[] relativeSegments)
    {
        ArgumentNullException.ThrowIfNull(relativeSegments);

        var fromOutput = Path.Combine([AppContext.BaseDirectory, .. relativeSegments]);
        if (File.Exists(fromOutput))
        {
            return File.ReadAllText(fromOutput);
        }

        var fromProject = Path.GetFullPath(Path.Combine([AppContext.BaseDirectory, "..", "..", "..", .. relativeSegments]));
        return File.ReadAllText(fromProject);
    }
}
