using System.Collections.ObjectModel;
using System.ComponentModel;
using ByteSync.Business;

namespace ByteSync.Interfaces.Services.Localizations;

public interface ILocalizationService : INotifyPropertyChanged
{
    void Initialize();
        
    ReadOnlyCollection<CultureDefinition> GetAvailableCultures();

    public IObservable<CultureDefinition> CurrentCultureObservable { get; }
        
    CultureDefinition CurrentCultureDefinition { get; }
        
    // CultureInfo CurrentCulture { get; }

    void SetCurrentCulture(CultureDefinition selectedCulture);
        
    string this[string key] { get; }

    string GetMonthName(int monthNumber0to11);
}