using System.Text.Json;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.StreamBrowser;

internal sealed class StreamBrowseService(IDocumentStore store)
{
    private const int MaxListResults = 50_000;
    private const int RavenPageSize = 1_024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<StreamListResult> ListStreamsAsync(
        string kind,
        string? query,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        take = take <= 0 ? MaxListResults : Math.Clamp(take, 1, MaxListResults);
        skip = Math.Max(0, skip);

        var aggregateType = StreamKinds.AggregateType(kind);
        var prefix = StreamKinds.MetadataPrefix(kind);
        var matches = string.IsNullOrWhiteSpace(query) ? null : $"{query.Trim()}*";

        using var session = store.OpenAsyncSession();
        var collected = new List<RavenEventStreamMetadataRecord>();
        var start = 0;

        // Enumerate by document-id prefix so we do not depend on auto-indexes / page caps.
        while (collected.Count < MaxListResults)
        {
            var batch = (await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
                prefix,
                matches: matches,
                start: start,
                pageSize: RavenPageSize,
                token: cancellationToken)).ToArray();

            if (batch.Length == 0)
            {
                break;
            }

            collected.AddRange(batch);
            start += batch.Length;

            if (batch.Length < RavenPageSize)
            {
                break;
            }
        }

        var ordered = collected
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.StreamId, StringComparer.Ordinal)
            .ToArray();

        var page = ordered
            .Skip(skip)
            .Take(take)
            .Select(x => new StreamSummary(
                StreamKinds.KindFromAggregateType(string.IsNullOrWhiteSpace(x.AggregateType) ? aggregateType : x.AggregateType),
                string.IsNullOrWhiteSpace(x.AggregateType) ? aggregateType : x.AggregateType,
                x.StreamId,
                x.Id,
                x.Version,
                x.UpdatedAtUtc,
                InferWorkKeying(x.StreamId)))
            .ToArray();

        return new StreamListResult(
            kind,
            aggregateType,
            page,
            ordered.Length,
            skip + page.Length < ordered.Length);
    }

    public async Task<StreamDetailResult?> GetStreamAsync(
        string kind,
        string streamId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            return null;
        }

        streamId = Uri.UnescapeDataString(streamId.Trim());
        var aggregateType = StreamKinds.AggregateType(kind);
        var metadataId = $"{aggregateType}-streams/{streamId}";
        var eventPrefix = StreamKinds.EventPrefix(kind, streamId);

        using var session = store.OpenAsyncSession();
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken);

        var storedEvents = new List<BrowseEventDocument>();
        var eventStart = 0;
        while (true)
        {
            var batch = (await session.Advanced.LoadStartingWithAsync<BrowseEventDocument>(
                eventPrefix,
                start: eventStart,
                pageSize: RavenPageSize,
                token: cancellationToken)).ToArray();

            if (batch.Length == 0)
            {
                break;
            }

            storedEvents.AddRange(batch);
            eventStart += batch.Length;
            if (batch.Length < RavenPageSize)
            {
                break;
            }
        }

        var orderedEvents = storedEvents
            .OrderBy(x => x.Version)
            .ToArray();

        if (metadata is null && orderedEvents.Length == 0)
        {
            return null;
        }

        return new StreamDetailResult(
            kind,
            aggregateType,
            streamId,
            metadataId,
            eventPrefix,
            metadata?.Version ?? orderedEvents.LastOrDefault()?.Version ?? 0,
            metadata?.UpdatedAtUtc,
            metadata?.AppliedOperationIds ?? [],
            InferWorkKeying(streamId),
            orderedEvents.Select(ToEventDto).ToArray());
    }

    private static StreamEventDto ToEventDto(BrowseEventDocument storedEvent) =>
        new(
            storedEvent.Id ?? string.Empty,
            storedEvent.Version,
            storedEvent.EventId ?? string.Empty,
            storedEvent.EventType ?? string.Empty,
            storedEvent.BodyType,
            storedEvent.OccurredAtUtc,
            storedEvent.CorrelationId,
            storedEvent.CausationId,
            storedEvent.ProjectionHint ?? "live",
            SerializeBody(storedEvent.Body));

    private static string? SerializeBody(object? body)
    {
        if (body is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(body, JsonOptions);
        }
        catch
        {
            return body.ToString();
        }
    }

    private static string? InferWorkKeying(string streamId)
    {
        if (streamId.StartsWith("search:", StringComparison.Ordinal))
        {
            return "search";
        }

        var separator = streamId.IndexOf(':', StringComparison.Ordinal);
        return separator > 0 ? streamId[..separator] : null;
    }
}

/// <summary>
/// Loose document shape so Raven can deserialize event bodies without the type registry.
/// </summary>
internal sealed class BrowseEventDocument
{
    public string? Id { get; set; }
    public string? StreamId { get; set; }
    public string? AggregateType { get; set; }
    public int Version { get; set; }
    public string? EventId { get; set; }
    public string? EventType { get; set; }
    public string? BodyType { get; set; }
    public object? Body { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? ProjectionHint { get; set; }
}

internal sealed record StreamListResult(
    string Kind,
    string AggregateType,
    IReadOnlyList<StreamSummary> Streams,
    int Total,
    bool HasMore);

internal sealed record StreamSummary(
    string Kind,
    string AggregateType,
    string StreamId,
    string MetadataDocumentId,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    string? KeyingHint);

internal sealed record StreamDetailResult(
    string Kind,
    string AggregateType,
    string StreamId,
    string MetadataDocumentId,
    string EventDocumentPrefix,
    int Version,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<string> AppliedOperationIds,
    string? KeyingHint,
    IReadOnlyList<StreamEventDto> Events);

internal sealed record StreamEventDto(
    string Id,
    int Version,
    string EventId,
    string EventType,
    string? BodyType,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    string? CausationId,
    string ProjectionHint,
    string? BodyJson);
