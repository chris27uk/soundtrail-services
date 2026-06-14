using System.Collections.Concurrent;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class PipelineMessageCapture
{
    private readonly ConcurrentDictionary<Type, object> received = new();

    public void Record<TMessage>(TMessage message) where TMessage : class
    {
        var source = (TaskCompletionSource<TMessage>)received.GetOrAdd(
            typeof(TMessage),
            _ => new TaskCompletionSource<TMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

        source.TrySetResult(message);
    }

    public async Task<TMessage> WaitForAsync<TMessage>(TimeSpan timeout) where TMessage : class
    {
        var source = (TaskCompletionSource<TMessage>)received.GetOrAdd(
            typeof(TMessage),
            _ => new TaskCompletionSource<TMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(source.Task, timeoutTask);
        if (completed == timeoutTask)
        {
            throw new TimeoutException($"{typeof(TMessage).Name} was not captured.");
        }

        return await source.Task;
    }

    public async Task<bool> DidReceiveAsync<TMessage>(TimeSpan timeout) where TMessage : class
    {
        var source = (TaskCompletionSource<TMessage>)received.GetOrAdd(
            typeof(TMessage),
            _ => new TaskCompletionSource<TMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(source.Task, timeoutTask);
        return completed == source.Task;
    }
}
