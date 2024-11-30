using System.Reflection;

namespace ByteSync.Common.Helpers;

public class ReflectionUtils
{
    public static TResult GetPrivatePropertyValue<TResult>(object @object, string propertyName)
    {
        PropertyInfo prop = @object.GetType().
            GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        var getter = prop.GetGetMethod(nonPublic: true);
        var value = getter?.Invoke(@object, null);

        TResult result;
        if (value is TResult)
        {
            result = (TResult)value;
        }
        else
        {
            result = default;
        }

        return result;
    }
}