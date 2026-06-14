using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record RequestDiscoveryCommand(
    DiscoveryQueryKey QueryKey,
    LookupMusicRequest Request);
