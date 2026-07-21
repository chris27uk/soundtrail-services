using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Shared.Execution;

public sealed class AdmittedLookupHandlerDecorator<TMessage>(
    IHandler<TMessage> inner,
    ILookupDecoratorMetadata<TMessage> metadata,
    ICommandBus commandBus,
    ILookupExecutionAdmissionPort lookupExecutionAdmissionPort,
    IClockPort clock) : IHandler<TMessage>
    where TMessage : IMessage
{
    public async Task Handle(TMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;
        var admissionResult = await lookupExecutionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(metadata.Source, request.Id, observedAt),
            cancellationToken);

        if (admissionResult.Status == LookupExecutionAdmissionStatus.Duplicate)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        metadata.CreateContext(request),
                        metadata.CreateExistingItem(request, observedAt),
                        "Lookup already executing.",
                        observedAt)),
                cancellationToken);
            throw new LookupExecutionShortCircuitException();
        }

        if (admissionResult.Status == LookupExecutionAdmissionStatus.Deferred)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Deferred(
                        metadata.CreateContext(request),
                        admissionResult.RetryAt ?? observedAt.AddMinutes(1),
                        admissionResult.Reason,
                        observedAt)),
                cancellationToken);
            throw new LookupExecutionShortCircuitException();
        }

        try
        {
            await inner.Handle(request, cancellationToken);
            await lookupExecutionAdmissionPort.CommitAsync(request.Id, cancellationToken);
        }
        catch
        {
            await lookupExecutionAdmissionPort.ReleaseAsync(request.Id, cancellationToken);
            throw;
        }
    }
}
