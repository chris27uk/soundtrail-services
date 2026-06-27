namespace Soundtrail.Services.Tests.Integration.Api.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RavenEmbeddedCollection
{
    public const string Name = "RavenEmbedded";
}
