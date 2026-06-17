using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Api.Infrastructure.Messaging
{
    public static class LookupMusicRequestMapper
    {
        public static LookupMusicRequestDto ToDto(LookupMusicRequest request)
        {
            return new LookupMusicRequestDto(
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }

        public static LookupMusicRequest? FromDto(LookupMusicRequestDto request)
        {
            return new LookupMusicRequest(
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }
    }
}
