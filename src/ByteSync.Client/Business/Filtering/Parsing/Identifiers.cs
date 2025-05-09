namespace ByteSync.Business.Filtering.Parsing;

public class Identifiers
{
    public const string ACTION_SYNCHRONIZE_CONTENT = "synchronizecontent";
    public const string ACTION_SYNCHRONIZE_CONTENT_AND_DATE = "synchronizecontentanddate";
    public const string ACTION_SYNCHRONIZE_DATE = "synchronizecontentdate";
    public const string ACTION_SYNCHRONIZE_CREATE = "create";
    public const string ACTION_SYNCHRONIZE_DELETE = "delete";
    public const string ACTION_DO_NOTHING = "do_nothing";
    public const string ACTION_TARGETED = "targeted";
    public const string ACTION_RULES = "rules";

    private static List<string>? _cachedAll;

    public static List<string> All()
    {
        if (_cachedAll == null)
        {
            _cachedAll = typeof(Identifiers)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetValue(null))
                .ToList();
        }
        
        return _cachedAll;
    }
}
