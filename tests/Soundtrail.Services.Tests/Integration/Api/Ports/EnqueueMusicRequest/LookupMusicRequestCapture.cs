using Soundtrail.Contracts;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine;
using Wolverine.Logging;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest
{
    public sealed class LookupMusicRequestCapture : IMessageTracker
    {
        private readonly TaskCompletionSource<LookupMusicRequestDto> received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(LookupMusicRequestDto requestDto) => this.received.TrySetResult(requestDto);

        public async Task<LookupMusicRequestDto> WaitAsync(TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completed = await Task.WhenAny(this.received.Task, timeoutTask);
            if (completed == timeoutTask)
            {
                throw new TimeoutException("LookupMusicRequest was not captured.");
            }

            return await this.received.Task;
        }

        public void LogStatus(string message)
        {
        }

        public void LogException(Exception ex, object? correlationId = null, string message = "Exception detected:")
        {
        }

        public void Sent(Envelope envelope)
        {
            if (envelope.Message is LookupMusicRequestDto requestDto)
            {
                Record(requestDto);
            }
        }

        public void Received(Envelope envelope)
        {
        }

        public void ExecutionStarted(Envelope envelope)
        {
        }

        public void ExecutionFinished(Envelope envelope)
        {
        }

        public void ExecutionFinished(Envelope envelope, Exception exception)
        {
        }

        public void MessageSucceeded(Envelope envelope)
        {
        }

        public void MessageFailed(Envelope envelope, Exception ex)
        {
        }

        public void NoHandlerFor(Envelope envelope)
        {
        }

        public void NoRoutesFor(Envelope envelope)
        {
        }

        public void MovedToErrorQueue(Envelope envelope, Exception ex)
        {
        }

        public void DiscardedEnvelope(Envelope envelope)
        {
        }

        public void Requeued(Envelope envelope)
        {
        }
    }
}
