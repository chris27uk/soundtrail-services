namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Redis/ASB default to OpenServiceBus + Redis via Testcontainers (~1s cold start each).
/// Opt out with <see cref="DisableTestcontainersEnvironmentVariable"/> and provide env vars
/// or a running local instance (AppHost, compose, etc.).
/// Raven defaults to Embedded locally; set <see cref="RavenUrlEnvironmentVariable"/> to use a server.
/// </summary>
internal static class TestInfrastructurePolicy
{
    public const string DisableTestcontainersEnvironmentVariable = "SOUNDTRAIL_TEST_NO_TESTCONTAINERS";

    public const string RavenUrlEnvironmentVariable = "SOUNDTRAIL_TEST_RAVEN";

    /// <summary>
    /// When false, Redis/ASB must come from env or an already-running local instance.
    /// </summary>
    public static bool AllowTestcontainers =>
        !IsTruthy(Environment.GetEnvironmentVariable(DisableTestcontainersEnvironmentVariable));

    public static Exception MissingInfrastructure(string name, string envVar) =>
        TestInfrastructureException.Unavailable(
            $"{name} was not found (set {envVar} or start a local instance). " +
            "Testcontainers is disabled via SOUNDTRAIL_TEST_NO_TESTCONTAINERS.");

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}
