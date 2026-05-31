using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling
{
    internal sealed record DeadLetteredLookupMusicRequest(LookupMusicRequest Request, string Reason);
}
