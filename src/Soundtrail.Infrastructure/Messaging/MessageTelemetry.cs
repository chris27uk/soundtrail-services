using System.Diagnostics;
using System.Text;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Adapters.Messaging;

public static class MessageTelemetry
{
    public const string HandleMessageActivityName = "handle message";
    public const string PublishMessageActivityName = "publish message";

    private static readonly ActivitySource ActivitySource = new("Soundtrail.Messaging");

    public static Activity? StartPublishActivity(
        IMessage message,
        object transportMessage,
        string queueName)
    {
        var activity = ActivitySource.StartActivity(
            PublishMessageActivityName,
            ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        var timestamp = DateTimeOffset.UtcNow;
        activity.SetTag("soundtrail.dto_type_name", transportMessage.GetType().FullName);
        activity.SetTag("soundtrail.domain_event_name", message.GetType().FullName);
        activity.SetTag("soundtrail.correlation_id", message.CorrelationId.Value);
        activity.SetTag("messaging.conversation_id", message.CorrelationId.Value);
        activity.SetTag("soundtrail.timestamp", timestamp.UtcDateTime);
        activity.SetTag("soundtrail.queue_name", queueName);
        activity.SetTag("message.id", message.Id.Value);
        activity.SetTag("soundtrail.requested_at_utc", message.RequestedAt.UtcDateTime);

        if (message is IPrioritisedMessage prioritisedMessage)
        {
            activity.SetTag("soundtrail.trust_level", prioritisedMessage.TrustLevel);
            activity.SetTag("soundtrail.risk_score", prioritisedMessage.RiskScore);
        }

        if (message is ITargetedMessage targetedMessage)
        {
            var target = targetedMessage.Target;
            activity.SetTag("soundtrail.target", target.NormalisedIdentifier);
            activity.SetTag("soundtrail.target_kind", target.GetType().Name);
        }

        activity.AddEvent(
            new ActivityEvent(
                PublishMessageActivityName,
                tags: CreatePublishEventTags(
                    transportMessage.GetType().FullName,
                    message.GetType().FullName,
                    message.CorrelationId.Value,
                    timestamp,
                    queueName)));

        return activity;
    }

    public static Activity? StartHandlerActivity(IMessage message, string stage)
    {
        var activity = StartHandlerActivity(message.GetType(), stage);
        if (activity is null)
        {
            return null;
        }

        EnrichActivity(activity, message, stage);
        return activity;
    }

    public static Activity? StartHandlerActivity(Type payloadType, string stage)
    {
        ArgumentNullException.ThrowIfNull(payloadType);

        var activity = ActivitySource.StartActivity(
            ToKebabCase(stage),
            ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("soundtrail.workflow_stage", stage);
        activity.SetTag("soundtrail.domain_event_name", payloadType.FullName);
        activity.AddEvent(new ActivityEvent($"{stage}.started"));
        return activity;
    }

    internal static Activity? StartHandleActivity(
        TransportEnvelope envelope,
        Type dtoType,
        Type domainType) =>
        StartHandleActivity(
            dtoTypeName: dtoType.FullName,
            domainEventName: domainType.FullName,
            correlationId: envelope.Metadata.CorrelationId,
            sourceName: envelope.Metadata.QueueName,
            isRetry: envelope.Metadata.RetryCount > 0,
            retryCount: envelope.Metadata.RetryCount,
            messageId: envelope.Metadata.MessageId,
            deliveryCount: envelope.DeliveryCount);

    public static Activity? StartHandleActivity(
        string? dtoTypeName,
        string? domainEventName,
        string? correlationId,
        string? sourceName,
        bool isRetry = false,
        int retryCount = 0,
        string? messageId = null,
        int? deliveryCount = null)
    {
        var activity = ActivitySource.StartActivity(
            HandleMessageActivityName,
            ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        var timestamp = DateTimeOffset.UtcNow;

        activity.SetTag("soundtrail.dto_type_name", dtoTypeName);
        activity.SetTag("soundtrail.domain_event_name", domainEventName);
        activity.SetTag("soundtrail.correlation_id", correlationId);
        activity.SetTag("messaging.conversation_id", correlationId);
        activity.SetTag("soundtrail.timestamp", timestamp.UtcDateTime);
        activity.SetTag("soundtrail.queue_name", sourceName);
        activity.SetTag("soundtrail.is_retry", isRetry);
        activity.SetTag("soundtrail.retry_count", retryCount);

        if (messageId is not null)
        {
            activity.SetTag("messaging.message.id", messageId);
        }

        if (deliveryCount is not null)
        {
            activity.SetTag("soundtrail.delivery_count", deliveryCount.Value);
        }

        activity.AddEvent(
            new ActivityEvent(
                HandleMessageActivityName,
                tags: CreateHandleEventTags(
                    dtoTypeName,
                    domainEventName,
                    correlationId,
                    timestamp,
                    sourceName,
                    isRetry,
                    retryCount)));

        return activity;
    }

    public static void EnrichCurrentActivity(IMessage message, string stage)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        EnrichActivity(activity, message, stage);
    }

    public static void EnrichCurrentActivity(
        string stage,
        MessageId messageId,
        CorrelationId correlationId,
        DateTimeOffset requestedAt,
        EnrichmentTarget target,
        int? trustLevel = null,
        int? riskScore = null)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("soundtrail.workflow_stage", stage);
        activity.SetTag("message.id", messageId.Value);
        activity.SetTag("messaging.conversation_id", correlationId.Value);
        activity.SetTag("soundtrail.correlation_id", correlationId.Value);
        activity.SetTag("soundtrail.requested_at_utc", requestedAt.UtcDateTime);
        activity.SetTag("soundtrail.target", target.NormalisedIdentifier);
        activity.SetTag("soundtrail.target_kind", target.GetType().Name);
        activity.SetTag("soundtrail.trust_level", trustLevel);
        activity.SetTag("soundtrail.risk_score", riskScore);
    }

    public static void AddCurrentEvent(string eventName) =>
        Activity.Current?.AddEvent(new ActivityEvent(eventName));

    public static string StageNameFor(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var name = messageType.Name;
        if (name.EndsWith("Message", StringComparison.Ordinal))
        {
            name = name[..^"Message".Length];
        }
        else if (name.EndsWith("Command", StringComparison.Ordinal))
        {
            name = name[..^"Command".Length];
        }

        return ToKebabCase(name);
    }

    private static void EnrichActivity(Activity activity, IMessage message, string stage)
    {
        activity.SetTag("soundtrail.workflow_stage", stage);
        activity.SetTag("message.id", message.Id.Value);
        activity.SetTag("messaging.conversation_id", message.CorrelationId.Value);
        activity.SetTag("soundtrail.correlation_id", message.CorrelationId.Value);
        activity.SetTag("soundtrail.domain_event_name", message.GetType().FullName);
        activity.SetTag("soundtrail.message_type", message.GetType().FullName);
        activity.SetTag("soundtrail.requested_at_utc", message.RequestedAt.UtcDateTime);

        if (message is IPrioritisedMessage prioritisedMessage)
        {
            activity.SetTag("soundtrail.trust_level", prioritisedMessage.TrustLevel);
            activity.SetTag("soundtrail.risk_score", prioritisedMessage.RiskScore);
        }

        if (message is ITargetedMessage targetedMessage)
        {
            var target = targetedMessage.Target;
            activity.SetTag("soundtrail.target", target.NormalisedIdentifier);
            activity.SetTag("soundtrail.target_kind", target.GetType().Name);
        }
    }

    private static ActivityTagsCollection CreateHandleEventTags(
        string? dtoTypeName,
        string? domainEventName,
        string? correlationId,
        DateTimeOffset timestamp,
        string? queueName,
        bool isRetry,
        int retryCount) =>
        new()
        {
            { "soundtrail.dto_type_name", dtoTypeName },
            { "soundtrail.domain_event_name", domainEventName },
            { "soundtrail.correlation_id", correlationId },
            { "soundtrail.timestamp", timestamp.UtcDateTime },
            { "soundtrail.queue_name", queueName },
            { "soundtrail.is_retry", isRetry },
            { "soundtrail.retry_count", retryCount }
        };

    private static ActivityTagsCollection CreatePublishEventTags(
        string? dtoTypeName,
        string? domainEventName,
        string correlationId,
        DateTimeOffset timestamp,
        string queueName) =>
        new()
        {
            { "soundtrail.dto_type_name", dtoTypeName },
            { "soundtrail.domain_event_name", domainEventName },
            { "soundtrail.correlation_id", correlationId },
            { "soundtrail.timestamp", timestamp.UtcDateTime },
            { "soundtrail.queue_name", queueName }
        };

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
