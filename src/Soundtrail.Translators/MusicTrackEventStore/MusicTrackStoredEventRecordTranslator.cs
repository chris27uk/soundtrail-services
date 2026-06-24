using System.Text.Json;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Translators.MusicTrackEventStore;

public sealed class MusicTrackStoredEventRecordTranslator : IMusicTrackStoredEventRecordTranslator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<MusicTrackStoredEventRecordTranslator> DefaultValue = new(CreateDefault);

    private readonly MusicTrackStoredEventTranslationRegistry registry;

    public MusicTrackStoredEventRecordTranslator(MusicTrackStoredEventTranslationRegistry registry)
    {
        this.registry = registry;
    }

    public static IMusicTrackStoredEventRecordTranslator Default => DefaultValue.Value;

    public MusicTrackStoredEventRecordDto ToDto(
        MusicCatalogId musicCatalogId,
        int version,
        CommandId commandId,
        IMusicTrackEvent domainEvent)
    {
        var registration = registry.GetByDomainType(domainEvent.GetType());
        var bodyDto = registration.ToDto(domainEvent);

        return new MusicTrackStoredEventRecordDto
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
            MusicCatalogId = musicCatalogId.Value,
            Version = version,
            EventType = registration.EventType,
            SchemaVersion = 1,
            BodyJson = JsonSerializer.Serialize(bodyDto, registration.BodyDtoType, JsonOptions),
            OccurredAtUtc = registration.GetOccurredAtUtc(domainEvent),
            CorrelationId = registration.GetCorrelationId(domainEvent),
            CausationId = commandId.Value
        };
    }

    public IMusicTrackEvent ToDomainObject(MusicTrackStoredEventRecordDto dto)
    {
        var registration = registry.GetByEventType(dto.EventType);

        if (string.IsNullOrWhiteSpace(dto.BodyJson))
        {
            throw new InvalidOperationException($"Stored music track event '{dto.EventType}' is missing body json.");
        }

        var bodyDto = JsonSerializer.Deserialize(dto.BodyJson, registration.BodyDtoType, JsonOptions)
            ?? throw new InvalidOperationException($"Stored music track event '{dto.EventType}' body json could not be deserialized.");

        return (IMusicTrackEvent)registration.ToDomainObject(bodyDto);
    }

    private static MusicTrackStoredEventRecordTranslator CreateDefault()
    {
        var registry = new MusicTrackStoredEventTranslationRegistry();
        var registrationTypes = typeof(MusicTrackStoredEventRecordTranslator).Assembly.GetTypes()
            .Where(type => !type.IsAbstract
                && !type.IsInterface
                && typeof(IMusicTrackStoredEventTranslationRegistration).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var registrationType in registrationTypes)
        {
            var registration = (IMusicTrackStoredEventTranslationRegistration)Activator.CreateInstance(registrationType)!;
            registration.Register(registry);
        }

        return new MusicTrackStoredEventRecordTranslator(registry);
    }
}
