using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Translations;

public static class InternalProjectorTypeTranslator
{
    private static readonly Lazy<ITypeTranslator> DefaultValue =
        new(() => TypeTranslationRegistry.CreateFromAssemblies(typeof(InternalProjectorTypeTranslator).Assembly));

    public static ITypeTranslator Default => DefaultValue.Value;
}
