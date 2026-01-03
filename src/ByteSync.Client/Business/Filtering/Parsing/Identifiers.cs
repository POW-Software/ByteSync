using System.Reflection;

namespace ByteSync.Business.Filtering.Parsing;

public class Identifiers
{
    public const string ACTION_COPY = "copy";
    public const string ACTION_COPY_CONTENT = "copy-content";
    public const string ACTION_COPY_DATES = "copy-dates";
    public const string ACTION_SYNCHRONIZE_CREATE = "create";
    public const string ACTION_SYNCHRONIZE_DELETE = "delete";
    public const string ACTION_DO_NOTHING = "do-nothing";
    public const string ACTION_TARGETED = "targeted";
    public const string ACTION_RULES = "rules";
    
    public const string OPERATOR_ON = "on";
    public const string OPERATOR_ONLY = "only";
    public const string OPERATOR_IS = "is";
    public const string OPERATOR_HAS = "has";
    public const string OPERATOR_ACTIONS = "actions";
    public const string OPERATOR_NAME = "name";
    public const string OPERATOR_PATH = "path";
    
    public const string PROPERTY_ACCESS_ISSUE = "access-issue";
    public const string PROPERTY_COMPUTATION_ERROR = "computation-error";
    public const string PROPERTY_SYNC_ERROR = "sync-error";
    
    public const string PROPERTY_CONTENT = "content";
    public const string PROPERTY_CONTENT_AND_DATE = "content-and-date";
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
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetValue(null))
                .ToList();
        }
        
        return _cachedAll;
    }
}