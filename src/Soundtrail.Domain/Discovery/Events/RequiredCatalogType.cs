namespace Soundtrail.Domain.Discovery.Events
{
    public enum RequiredCatalogType
    {
        None = 0,
        Tracks = 1,
        Albums = 2,
        All = Tracks | Albums
    }
}
