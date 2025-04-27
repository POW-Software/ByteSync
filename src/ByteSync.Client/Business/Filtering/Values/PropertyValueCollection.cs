namespace ByteSync.Business.Filtering.Values;

public class PropertyValueCollection : IReadOnlyCollection<PropertyValue>
{
    private readonly List<PropertyValue> _values = new();
    
    public PropertyValueCollection() { }
    
    public PropertyValueCollection(IEnumerable<PropertyValue> values)
    {
        _values.AddRange(values);
        DetermineCollectionType();
    }
    
    public PropertyValueType CollectionType { get; private set; } = PropertyValueType.Unknown;
    
    public int Count => _values.Count;
    
    public void Add(PropertyValue value)
    {
        _values.Add(value);
        DetermineCollectionType();
    }
    
    public void AddRange(IEnumerable<PropertyValue> values)
    {
        _values.AddRange(values);
        DetermineCollectionType();
    }
    
    public IEnumerator<PropertyValue> GetEnumerator() => _values.GetEnumerator();
    
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    
    private void DetermineCollectionType()
    {
        if (_values.Count == 0)
        {
            CollectionType = PropertyValueType.Unknown;
            return;
        }
        
        // Si tous les éléments ont le même type, utilisez ce type
        var firstType = _values[0].Type;
        if (_values.All(v => v.Type == firstType))
        {
            CollectionType = firstType;
        }
        else
        {
            CollectionType = PropertyValueType.Unknown;
        }
    }
    
    // Méthodes utilitaires pour faciliter l'utilisation
    public bool Any() => _values.Any();
    public bool All(Func<PropertyValue, bool> predicate) => _values.All(predicate);
    public bool Any(Func<PropertyValue, bool> predicate) => _values.Any(predicate);
}