using System.ComponentModel;
using ByteSync.Interfaces;

namespace ByteSync.Services.Localizations;

/// <summary>
/// Gestion dynamique de la localisation
/// https://www.codinginfinity.me/posts/localization-of-a-wpf-app-the-simple-approach/
/// https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
/// </summary>
public class TGGGranslationSource : IGGGTranslationSource
{
    // private readonly ResourceManager _resManager = Resources.ResourceManager;
    //
    // private CultureInfo? _currentCulture;
    //     
    // public TranslationSource()
    // {
    //
    // }
    //
    // public string this[string key]
    // {
    //     get
    //     {
    //         // var rs = _resManager.GetResourceSet(this._currentCulture, true, true);
    //
    //         string? result = this._resManager.GetString(key, this._currentCulture);
    //
    //         if (result == null)
    //         {
    //             result = this._resManager.GetString(key.Replace("_", "."), this._currentCulture);
    //         }
    //
    //         if (result == null)
    //         {
    //
    //         }
    //
    //         return result!;
    //     }
    // }
    //
    // public CultureInfo CurrentCulture
    // {
    //     get { return this._currentCulture!; }
    //     set
    //     {
    //         if (!Equals(this._currentCulture, value))
    //         {
    //             this._currentCulture = value;
    //
    //             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    //             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    //         }
    //     }
    // }
    //
    // /// <summary>
    // /// Renvoie le nom d'un mois dans la culture actuel
    // /// </summary>
    // /// <param name="monthNumber0to11">de 0 à 11, janvier à décembre</param>
    // /// <returns></returns>
    // public string GetMonthName(int monthNumber0to11)
    // {
    //     string key = monthNumber0to11 switch
    //     {
    //         0 => "General_January",
    //         1 => "General_February",
    //         2 => "General_March",
    //         3 => "General_April",
    //         4 => "General_May",
    //         5 => "General_June",
    //         6 => "General_July",
    //         7 => "General_August",
    //         8 => "General_September",
    //         9 => "General_October",
    //         10 => "General_November",
    //         11 => "General_December",
    //         _ => throw new ArgumentOutOfRangeException(nameof(monthNumber0to11))
    //     };
    //
    //     return this[key];
    // }
    //
    public event PropertyChangedEventHandler? PropertyChanged;
}