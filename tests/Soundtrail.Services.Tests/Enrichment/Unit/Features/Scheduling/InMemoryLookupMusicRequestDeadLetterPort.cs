using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling
{
    internal sealed class InMemoryLookupMusicRequestDeadLetterPort : ILookupMusicRequestDeadLetterPort
    {
        private readonly List<DeadLetteredLookupMusicRequest> deadLetters = [];

        public IReadOnlyList<DeadLetteredLookupMusicRequest> DeadLetters => this.deadLetters;

        public Task DeadLetterAsync(
            LookupMusicRequest request,
            string reason,
            CancellationToken cancellationToken)
        {
            this.deadLetters.Add(new DeadLetteredLookupMusicRequest(request, reason));
            return Task.CompletedTask;
        }
    }
}
