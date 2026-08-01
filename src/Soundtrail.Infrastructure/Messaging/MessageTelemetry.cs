using System.Diagnostics;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Adapters.Messaging;

public static class MessageTelemetry
{
    private static readonly ActivitySource ActivitySource = new("Soundtrail.Messaging");

    public static Activity? StartPublishActivity(IMessage message, object transportMessage)
    {
        var activity = ActivitySource.StartActivity(
            $"{message.GetType().Name} publish",
            ActivityKind.Producer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("messaging.operation", "publish");
        activity.SetTag("message.id", message.Id.Value);
        activity.SetTag("messaging.conversation_id", message.CorrelationId.Value);
        activity.SetTag("soundtrail.message_type", message.GetType().FullName);
        activity.SetTag("soundtrail.transport_message_type", transportMessage.GetType().FullName);
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

        activity.AddEvent(new ActivityEvent("message.published"));

        return activity;
    }

    public static Activity? StartHandlerActivity(IMessage message, string stage)
    {
        var activity = ActivitySource.StartActivity(
            $"{message.GetType().Name} {stage}",
            ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        EnrichActivity(activity, message, stage);
        activity.AddEvent(new ActivityEvent($"{stage}.started"));

        return activity;
    }

    internal static Activity? StartConsumeActivity(TransportEnvelope envelope)
    {
        var activity = ActivitySource.StartActivity(
            $"{envelope.TransportMessageType.Name} consume",
            ActivityKind.Consumer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("messaging.system", envelope.TransportSystem);
        activity.SetTag("messaging.operation", "process");
        activity.SetTag("messaging.destination.name", envelope.Metadata.QueueName);
        activity.SetTag("messaging.message.id", envelope.Metadata.MessageId);
        activity.SetTag("messaging.conversation_id", envelope.Metadata.CorrelationId);
        activity.SetTag("soundtrail.transport_message_type", envelope.TransportMessageType.FullName);
        activity.SetTag("soundtrail.delivery_count", envelope.DeliveryCount);
        activity.SetTag("soundtrail.retry_count", envelope.Metadata.RetryCount);
        activity.AddEvent(new ActivityEvent("message.received"));

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
        activity.SetTag("soundtrail.requested_at_utc", requestedAt.UtcDateTime);
        activity.SetTag("soundtrail.target", target.NormalisedIdentifier);
        activity.SetTag("soundtrail.target_kind", target.GetType().Name);
        activity.SetTag("soundtrail.trust_level", trustLevel);
        activity.SetTag("soundtrail.risk_score", riskScore);
    }

    public static void AddCurrentEvent(string eventName) =>
        Activity.Current?.AddEvent(new ActivityEvent(eventName));

    private static void EnrichActivity(Activity activity, IMessage message, string stage)
    {
        activity.SetTag("soundtrail.workflow_stage", stage);
        activity.SetTag("message.id", message.Id.Value);
        activity.SetTag("messaging.conversation_id", message.CorrelationId.Value);
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
}
