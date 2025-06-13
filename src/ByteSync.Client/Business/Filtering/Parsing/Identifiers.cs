namespace ByteSync.Business.Filtering.Parsing;

public class Identifiers
{
    public const string ACTION_COPY = "copy";
    public const string ACTION_COPY_CONTENTS = "copy-contents";
    public const string ACTION_COPY_DATES = "copy-dates";
    public const string ACTION_SYNCHRONIZE_CREATE = "create";
    public const string ACTION_SYNCHRONIZE_DELETE = "delete";
    public const string ACTION_DO_NOTHING = "do-nothing";
    public const string ACTION_TARGETED = "targeted";
    public const string ACTION_RULES = "rules";
    
    public const string OPERATOR_ON = "on";
    public const string OPERATOR_ONLY = "only";
    public const string OPERATOR_IS = "is";
    public const string OPERATOR_ACTIONS = "actions";
    public const string OPERATOR_NAME = "name";
    
    public const string PROPERTY_CONTENTS = "contents";
    public const string PROPERTY_CONTENTS_AND_DATE = "contents-and-date";
    public const string PROPERTY_LAST_WRITE_TIME = "last-write-time";
    public const string PROPERTY_SIZE = "size";
    
    public const string PROPERTY_FILE = "file";
    public const string PROPERTY_DIRECTORY = "directory";
    public const string PROPERTY_DIR = "dir";
    
    public const string PROPERTY_PLACEHOLDER = "_";

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
