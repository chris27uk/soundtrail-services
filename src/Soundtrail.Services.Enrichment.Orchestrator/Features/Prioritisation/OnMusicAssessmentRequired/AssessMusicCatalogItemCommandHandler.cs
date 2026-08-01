using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;

public sealed class AssessMusicCatalogItemCommandHandler(
    ITypeRegistry typeRegistry,
    OnMusicAssessmentRequiredHandler innerHandler) : IHandler<AssessMusicCatalogItemCommandDto>
{
    public Task Handle(IncomingMessage<AssessMusicCatalogItemCommandDto> context, CancellationToken cancellationToken = default) =>
        innerHandler.Handle(
            context.WithMessage(typeRegistry.ToDomainObject<AssessWorkMessage>(context.Message)),
            cancellationToken);
}
