using Soundtrail.Contracts;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Api.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest
{
    public sealed class LookupMusicRequestCapture
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
    }
}
