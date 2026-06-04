using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest
{
    public sealed class LookupMusicRequestCapture
    {
        private readonly TaskCompletionSource<LookupMusicRequest> received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(LookupMusicRequest request) => this.received.TrySetResult(request);

        public async Task<LookupMusicRequest> WaitAsync(TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completed = await Task.WhenAny(this.received.Task, timeoutTask);
            if (completed == timeoutTask)
            {
                throw new TimeoutException("LookupMusicRequest was not captured.");
            }

            return await this.received.Task;
        }
    }
}