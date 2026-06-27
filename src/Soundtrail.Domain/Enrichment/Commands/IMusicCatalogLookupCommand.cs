using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Enrichment.Commands;

public interface IMusicCatalogLookupCommand : ICommand
{
    MusicCatalogId MusicCatalogId { get; }
}
