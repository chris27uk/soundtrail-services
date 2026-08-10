using Xunit.Sdk;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

internal static class TestInfrastructureException
{
    /// <summary>
    /// xUnit v2: <c>new SkipException(message)</c>. v3: <c>SkipException.ForSkip(message)</c>.
    /// </summary>
    public static Exception Unavailable(string message)
    {
        var forSkip = typeof(SkipException).GetMethod(
            "ForSkip",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);
        if (forSkip is not null)
        {
            return (Exception)forSkip.Invoke(null, [message])!;
        }

        return (Exception)Activator.CreateInstance(typeof(SkipException), message)!;
    }
}
