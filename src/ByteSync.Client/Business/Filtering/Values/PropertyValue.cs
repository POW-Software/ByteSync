namespace ByteSync.Business.Filtering.Values;

public class PropertyValue
{
    public PropertyValue(object value)
    {
        Value = value;
        Type = DetermineType(value);
    }

    public object Value { get; set; }
    public PropertyValueType Type { get; private set; }

    private static PropertyValueType DetermineType(object value)
    {
        return value switch
        {
            string => PropertyValueType.String,
            DateTime => PropertyValueType.DateTime,
            int or long or float or double or decimal => PropertyValueType.Numeric,
            _ => PropertyValueType.Unknown
        };
    }
}