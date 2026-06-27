namespace Soundtrail.Translators.Registry;

public interface ITypeTranslator
{
    TTarget Translate<TTarget>(object source)
        where TTarget : class;

    void MapOnto<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class;
}
