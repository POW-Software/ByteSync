using System;
using System.Threading.Tasks;

namespace ByteSync.Common.Helpers;

public static class MiscUtils
{
    public static string CreateGUID()
    {
        return Guid.NewGuid().ToString();
    }

    public static bool In<T>(this T @object, params T[] possibleValues)
    {
        return possibleValues.ToList().Contains(@object);
    }
}