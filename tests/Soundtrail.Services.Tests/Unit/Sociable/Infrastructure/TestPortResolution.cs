namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

internal static class TestPortResolution
{
    public static TFake RequireFake<TService, TFake>(IServiceProvider services)
        where TService : class
        where TFake : class, TService =>
        services.GetRequiredService<TService>() as TFake
        ?? throw new InvalidOperationException(
            $"Expected '{typeof(TService).Name}' to be '{typeof(TFake).Name}'.");
}
