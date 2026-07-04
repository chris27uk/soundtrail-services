using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class KnownTrackRequestedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<KnownTrackRequested, KnownTrackRequestedDto>(
            request =>
                new KnownTrackRequestedDto(
                    request.TrackId.Value,
                    request.Playback.ToString(),
                    request.OccurredAt,
                    request.CorrelationId.Value),
            dto =>
                new KnownTrackRequested(
                    TrackId.From(dto.TrackId),
                    PlaybackProviderFilter.Parse(dto.Playback),
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
