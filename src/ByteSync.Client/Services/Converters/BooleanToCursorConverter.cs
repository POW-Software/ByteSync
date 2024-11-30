using Avalonia.Input;

namespace ByteSync.Services.Converters;

public class BooleanToCursorConverter : BooleanConverter<StandardCursorType>
{
    public BooleanToCursorConverter() : base(StandardCursorType.Wait, StandardCursorType.Arrow) {}
}