using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class ExternalTransportTranslationRegistrationTests
{
    [Fact]
    public void Assess_work_search_target_round_trips_search_types()
    {
        var message = new AssessWorkMessage(
            MessageId.From("AssessWork:search:nirvana"),
            CorrelationId.From("corr-123"),
            DateTimeOffset.Parse("2026-07-29T22:48:31Z"),
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("nirvana", SearchType.Track)),
            LookupPriorityBand.High,
            90,
            5);

        var dto = TypeTranslationRegistry.Default.ToDto<AssessMusicCatalogItemCommandDto>(message);
        var roundTripped = TypeTranslationRegistry.Default.ToDomainObject<AssessWorkMessage>(dto);

        dto.ResourceKindDto.Should().Be(CatalogItemResourceKindDto.SearchCriteria);
        dto.SearchTypes.Should().Be((int)SearchType.Track);
        roundTripped.Target.Should().Be(message.Target);
    }

    [Fact]
    public void Assess_work_all_search_target_preserves_all_search_type()
    {
        var message = new AssessWorkMessage(
            MessageId.From("AssessWork:search:all:nirvana"),
            CorrelationId.From("corr-456"),
            DateTimeOffset.Parse("2026-07-29T22:48:31Z"),
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("nirvana", SearchType.All)),
            LookupPriorityBand.High,
            null,
            null);

        var dto = TypeTranslationRegistry.Default.ToDto<AssessMusicCatalogItemCommandDto>(message);
        var roundTripped = TypeTranslationRegistry.Default.ToDomainObject<AssessWorkMessage>(dto);

        dto.SearchTypes.Should().Be((int)SearchType.All);
        roundTripped.Target.Should().Be(message.Target);
    }
}
