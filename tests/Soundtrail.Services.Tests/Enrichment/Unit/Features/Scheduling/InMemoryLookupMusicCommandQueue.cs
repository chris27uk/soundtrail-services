using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling
{
    internal sealed class InMemoryLookupMusicCommandQueue : ILookupMusicCommandQueue
    {
        private readonly List<LookupMusicCommand> commands = [];

        public IReadOnlyList<LookupMusicCommand> Commands => this.commands;

        public Task EnqueueAsync(
            LookupMusicCommand command,
            CancellationToken cancellationToken)
        {
            this.commands.Add(command);
            return Task.CompletedTask;
        }
    }
}
