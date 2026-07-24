using Dunet;

namespace Soundtrail.Domain.Catalog.Tracks;

[Union]
public partial record TrackIdentityCanonicalizeResult
{
    public partial record Success(CanonicalTrackIdentityParts Value);

    public partial record Failure(string Reason);
}
