namespace ByteSync.Business.Filtering.Values;

public class PropertyValue
{
    public PropertyValue(object value)
    {
        Value = value;
    }

    public object Value { get; set; }
}