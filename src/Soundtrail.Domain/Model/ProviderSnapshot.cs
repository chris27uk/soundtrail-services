using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Model;

public sealed record ProviderSnapshot(
    MusicCatalogId MusicCatalogId,
    ProviderName Provider,
    DateTimeOffset CapturedAt,
    string PayloadJson);
