namespace MiniCSharp.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Unpack<T>(this IEnumerable<T> source, out T target)
    {
        var array = source.ToArray();
        
        target = array.First();
        return array.Skip(1);
    }
}