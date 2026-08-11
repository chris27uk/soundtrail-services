namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// CI must pre-provision Redis/ASB (compose + env). Testcontainers is a local-only fallback.
/// </summary>
internal static class TestInfrastructurePolicy
{
    public const string DisableTestcontainersEnvironmentVariable = "SOUNDTRAIL_TEST_NO_TESTCONTAINERS";

    /// <summary>
    /// True in GitHub Actions or when <see cref="DisableTestcontainersEnvironmentVariable"/> is set.
    /// </summary>
    public static bool IsContinuousIntegration =>
        IsTruthy(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
        || IsTruthy(Environment.GetEnvironmentVariable(DisableTestcontainersEnvironmentVariable));

    /// <summary>
    /// When false, Redis/ASB must come from env or an already-running local instance.
    /// </summary>
    public static bool AllowTestcontainers => !IsContinuousIntegration;

    public static Exception MissingInfrastructure(string name, string envVar) =>
        IsContinuousIntegration
            ? new InvalidOperationException(
                $"{name} is required in CI but was not found. " +
                $"Start test infra before the build and set {envVar}. " +
                "Testcontainers is disabled outside local development.")
            : TestInfrastructureException.Unavailable(
                $"{name} was not found (set {envVar} or start a local instance). " +
                "Testcontainers fallback is disabled.");

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}
