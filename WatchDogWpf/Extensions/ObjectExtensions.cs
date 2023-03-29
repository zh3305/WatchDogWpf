using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

public static class ObjectExtensions
{
    public static T Clone<T>(this T source)where T:new()
    {
        var target = new T();
        source.CopyTo(target);
        return target;
    }

    public static void CopyTo<T>(this T source, T target)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var sourceProperties = source.GetType().GetProperties();
        var targetProperties = target.GetType().GetProperties();

        foreach (var sourceProperty in sourceProperties)
        {
            var targetProperty = targetProperties.FirstOrDefault(p => p.Name == sourceProperty.Name && p.PropertyType == sourceProperty.PropertyType);
            if (targetProperty != null && targetProperty.CanWrite)
            {
                var value = sourceProperty.GetValue(source);
                targetProperty.SetValue(target, value);
            }
        }
    }
}