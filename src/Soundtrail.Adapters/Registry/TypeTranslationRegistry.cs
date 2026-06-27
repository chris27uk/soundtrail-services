using System.Reflection;

namespace Soundtrail.Adapters.Registry;

public sealed class TypeTranslationRegistry : ITypeTranslator
{
    private static readonly Lazy<TypeTranslationRegistry> DefaultValue = new(CreateDefault);
    private readonly Dictionary<(Type SourceType, Type TargetType), Func<object, object>> translators = [];
    private readonly Dictionary<(Type SourceType, Type TargetType), Action<object, object>> mapOntoHandlers = [];

    public static ITypeTranslator Default => DefaultValue.Value;

    public static TypeTranslationRegistry CreateFromAssemblies(params Assembly[] assemblies)
    {
        var registry = new TypeTranslationRegistry();
        var registrationTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(ITypeTranslationRegistration).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var registrationType in registrationTypes)
        {
            var registration = (ITypeTranslationRegistration)Activator.CreateInstance(registrationType)!;
            registration.Register(registry);
        }

        return registry;
    }

    public void Register<TSource, TTarget>(
        Func<TSource, TTarget>? translate = null,
        Action<TSource, TTarget>? mapOnto = null)
        where TSource : class
        where TTarget : class
    {
        var key = (typeof(TSource), typeof(TTarget));

        if (translate is not null)
        {
            translators[key] = source => translate((TSource)source);
        }

        if (mapOnto is not null)
        {
            mapOntoHandlers[key] = (source, target) => mapOnto((TSource)source, (TTarget)target);
        }
    }

    public TTarget Translate<TTarget>(object source)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(source);

        var key = (source.GetType(), typeof(TTarget));
        if (translators.TryGetValue(key, out var translate))
        {
            return (TTarget)translate(source);
        }

        throw new InvalidOperationException(
            $"No type translation exists from '{source.GetType().FullName}' to '{typeof(TTarget).FullName}'.");
    }

    public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var key = (typeof(TSource), typeof(TTarget));
        if (mapOntoHandlers.TryGetValue(key, out var mapOnto))
        {
            mapOnto(source, target);
            return;
        }

        throw new InvalidOperationException(
            $"No map-onto translation exists from '{typeof(TSource).FullName}' to '{typeof(TTarget).FullName}'.");
    }

    private static TypeTranslationRegistry CreateDefault() =>
        CreateFromAssemblies(typeof(TypeTranslationRegistry).Assembly);
}
