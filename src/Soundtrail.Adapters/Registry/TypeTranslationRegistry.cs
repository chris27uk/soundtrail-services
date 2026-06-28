using System.Reflection;

namespace Soundtrail.Adapters.Registry;

public sealed class TypeTranslationRegistry : ITypeRegistry
{
    private static readonly Lazy<TypeTranslationRegistry> DefaultValue = new(CreateDefault);
    private readonly Dictionary<(Type SourceType, Type TargetType), Func<object, object>> translators = [];
    private readonly Dictionary<Type, Type> dtoTypesByDomainType = [];
    private readonly Dictionary<Type, Type> domainTypesByDtoType = [];
    private readonly Dictionary<Type, StoredEventTypeRegistration> storedEventTypesByDomainType = [];
    private readonly Dictionary<Type, StoredEventTypeRegistration> storedEventTypesByDtoType = [];
    private readonly Dictionary<string, StoredEventTypeRegistration> storedEventTypesByEventType =
        new(StringComparer.Ordinal);
    private readonly Dictionary<(Type SourceType, Type TargetType), Action<object, object>> mapOntoHandlers = [];

    public static ITypeRegistry Default => DefaultValue.Value;

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

    public void RegisterPair<TDomain, TDto>(
        Func<TDomain, TDto> toDto,
        Func<TDto, TDomain> toDomainObject,
        Action<TDomain, TDto>? mapOnto = null)
        where TDomain : class
        where TDto : class
    {
        Register(toDto, mapOnto);
        Register(toDomainObject);

        RegisterPairMetadata(typeof(TDomain), typeof(TDto));
    }

    public void RegisterStoredEventPair<TDomain, TDto>(
        string eventType,
        Func<TDomain, TDto> toDto,
        Func<TDto, TDomain> toDomainObject,
        Func<TDomain, DateTimeOffset> occurredAtUtc,
        Func<TDomain, string?>? correlationId = null)
        where TDomain : class
        where TDto : class
    {
        RegisterPair(toDto, toDomainObject);

        var registration = new StoredEventTypeRegistration(
            eventType,
            typeof(TDomain),
            typeof(TDto),
            source => occurredAtUtc((TDomain)source),
            source => correlationId?.Invoke((TDomain)source));

        storedEventTypesByDomainType[typeof(TDomain)] = registration;
        storedEventTypesByDtoType[typeof(TDto)] = registration;
        storedEventTypesByEventType[eventType] = registration;
    }

    public object ToDto(object domainObject)
    {
        ArgumentNullException.ThrowIfNull(domainObject);
        var dtoType = GetDtoTypeForDomain(domainObject.GetType());
        return Translate(domainObject, dtoType);
    }

    public TDto ToDto<TDto>(object domainObject)
        where TDto : class
    {
        return (TDto)Translate(domainObject, typeof(TDto));
    }

    public object ToDomainObject(object dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var domainType = GetDomainTypeForDto(dto.GetType());
        return Translate(dto, domainType);
    }

    public TDomain ToDomainObject<TDomain>(object dto)
        where TDomain : class
    {
        return (TDomain)Translate(dto, typeof(TDomain));
    }

    public Type GetDtoTypeForDomain(Type domainType)
    {
        ArgumentNullException.ThrowIfNull(domainType);

        if (dtoTypesByDomainType.TryGetValue(domainType, out var dtoType))
        {
            return dtoType;
        }

        throw new InvalidOperationException(
            $"No DTO type registration exists for domain type '{domainType.FullName}'.");
    }

    public Type GetDomainTypeForDto(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);

        if (domainTypesByDtoType.TryGetValue(dtoType, out var domainType))
        {
            return domainType;
        }

        throw new InvalidOperationException(
            $"No domain type registration exists for DTO type '{dtoType.FullName}'.");
    }

    public StoredEventTypeRegistration GetStoredEventRegistrationForDomain(Type domainType)
    {
        ArgumentNullException.ThrowIfNull(domainType);

        if (storedEventTypesByDomainType.TryGetValue(domainType, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException(
            $"No stored event registration exists for domain type '{domainType.FullName}'.");
    }

    public StoredEventTypeRegistration GetStoredEventRegistrationForDto(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);

        if (storedEventTypesByDtoType.TryGetValue(dtoType, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException(
            $"No stored event registration exists for DTO type '{dtoType.FullName}'.");
    }

    public StoredEventTypeRegistration GetStoredEventRegistrationForEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        if (storedEventTypesByEventType.TryGetValue(eventType, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException(
            $"No stored event registration exists for event type '{eventType}'.");
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

    private object Translate(object source, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetType);

        var key = (source.GetType(), targetType);
        if (translators.TryGetValue(key, out var translate))
        {
            return translate(source);
        }

        if (TryFindAssignableTranslation(source.GetType(), targetType, out translate))
        {
            return translate(source);
        }

        throw new InvalidOperationException(
            $"No type translation exists from '{source.GetType().FullName}' to '{targetType.FullName}'.");
    }

    private bool TryFindAssignableTranslation(
        Type sourceType,
        Type targetType,
        out Func<object, object> translate)
    {
        foreach (var candidate in translators)
        {
            if (candidate.Key.SourceType != sourceType)
            {
                continue;
            }

            if (!targetType.IsAssignableFrom(candidate.Key.TargetType))
            {
                continue;
            }

            translate = candidate.Value;
            return true;
        }

        translate = null!;
        return false;
    }

    private void RegisterPairMetadata(Type domainType, Type dtoType)
    {
        if (dtoTypesByDomainType.TryGetValue(domainType, out var existingDtoType) && existingDtoType != dtoType)
        {
            throw new InvalidOperationException(
                $"Domain type '{domainType.FullName}' is already registered to DTO type '{existingDtoType.FullName}'.");
        }

        if (domainTypesByDtoType.TryGetValue(dtoType, out var existingDomainType) && existingDomainType != domainType)
        {
            throw new InvalidOperationException(
                $"DTO type '{dtoType.FullName}' is already registered to domain type '{existingDomainType.FullName}'.");
        }

        dtoTypesByDomainType[domainType] = dtoType;
        domainTypesByDtoType[dtoType] = domainType;
    }

    private static TypeTranslationRegistry CreateDefault() =>
        CreateFromAssemblies(typeof(TypeTranslationRegistry).Assembly);
}
