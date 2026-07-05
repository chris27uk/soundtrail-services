namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.OperationalState;

public interface IClearPlannerOperationalStatePort
{
    Task<ClearPlannerOperationalStateResult> ClearAsync(CancellationToken cancellationToken);
}
