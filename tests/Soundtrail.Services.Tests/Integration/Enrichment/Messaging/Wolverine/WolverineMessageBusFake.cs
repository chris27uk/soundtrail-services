using Wolverine;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class WolverineMessageBusFake : IMessageBus
{
    private readonly List<object> sentMessages = [];

    public string? TenantId { get; set; }

    public IReadOnlyList<object> SentMessages => sentMessages;

    public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        sentMessages.Add(message!);
        return ValueTask.CompletedTask;
    }

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        sentMessages.Add(message!);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
    {
        sentMessages.Add(message);
        return ValueTask.CompletedTask;
    }

    public IDestinationEndpoint EndpointFor(string endpointName) => throw new NotSupportedException();

    public IDestinationEndpoint EndpointFor(Uri uri) => throw new NotSupportedException();

    public Task InvokeForTenantAsync(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public Task<T> InvokeForTenantAsync<T>(
        string tenantId,
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public Task InvokeAsync(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public Task InvokeAsync(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public Task<T> InvokeAsync<T>(
        object message,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public Task<T> InvokeAsync<T>(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default,
        TimeSpan? timeout = null) => throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(
        object message,
        CancellationToken cancellation = default) => throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(
        object message,
        DeliveryOptions options,
        CancellationToken cancellation = default) => throw new NotSupportedException();

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message) => [];

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options) => [];
}
