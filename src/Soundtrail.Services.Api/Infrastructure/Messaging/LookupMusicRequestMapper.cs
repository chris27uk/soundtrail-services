using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Api.Infrastructure.Messaging
{
    public static class LookupMusicRequestMapper
    {
        public static LookupMusicRequestDto ToDto(LookupMusicRequest request)
        {
            return new LookupMusicRequestDto(
                request.QueryKey,
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }

        public static LookupMusicRequest? FromDto(LookupMusicRequestDto request)
        {
            return new LookupMusicRequest(
                DiscoveryQueryKey.From(request.QueryKey),
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }
    }
}
