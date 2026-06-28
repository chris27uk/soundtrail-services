namespace Soundtrail.Adapters.Registry;

public interface ITypeRegistry
{
    TDto ToDto<TDto>(object domainObject)
        where TDto : class;

    object ToDto(object domainObject);

    TDomain ToDomainObject<TDomain>(object dto)
        where TDomain : class;

    object ToDomainObject(object dto);

    Type GetDtoTypeForDomain(Type domainType);

    Type GetDomainTypeForDto(Type dtoType);

    void MapOnto<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class;
}
