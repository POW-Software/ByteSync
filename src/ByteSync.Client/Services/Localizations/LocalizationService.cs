using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Resources;
using System.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Localizations;

/// Dynamic localization management
/// https://www.codinginfinity.me/posts/localization-of-a-wpf-app-the-simple-approach/
/// https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class LocalizationService : ILocalizationService
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly ILogger<LocalizationService> _logger;
    
    private readonly ResourceManager _resourceManager = Resources.ResourceManager;
    
    private readonly BehaviorSubject<CultureDefinition> _cultureDefinition;


    
    public LocalizationService(IApplicationSettingsRepository applicationSettingsManager, ILogger<LocalizationService> logger)
    {
        _applicationSettingsRepository = applicationSettingsManager;
        _logger = logger;
            
        AvailableCultures = BuildAvailableCultures();
        
        InitialCultureInfo = CultureInfo.CurrentCulture;

        _cultureDefinition = new BehaviorSubject<CultureDefinition>(new CultureDefinition(InitialCultureInfo));
    }

    private List<CultureDefinition> AvailableCultures { get; }

    public IObservable<CultureDefinition> CurrentCultureObservable => _cultureDefinition.AsObservable();
    
    public CultureDefinition CurrentCultureDefinition => _cultureDefinition.Value;
    
    private CultureInfo InitialCultureInfo { get; }

    public void Initialize()
    {
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();

        CultureDefinition? cultureDefinition = null;
        if (applicationSettings.CultureCode.IsNotEmpty())
        {
            cultureDefinition = GetCultureByCode(applicationSettings.CultureCode!);
        }
            
        cultureDefinition ??= GetBestCulture(InitialCultureInfo);
        cultureDefinition ??= GetCultureByCode("en");

        DoSetCurrentCulture(cultureDefinition!);
    }

    private CultureDefinition? GetBestCulture(CultureInfo cultureInfo)
    {
        CultureDefinition? cultureDefinition = AvailableCultures.FirstOrDefault(c => c.Code.Equals(cultureInfo.Name));

        if (cultureDefinition == null)
        {
            cultureDefinition = AvailableCultures.FirstOrDefault(c => c.Code.Equals(cultureInfo.Parent.Name));
        }

        return cultureDefinition;
    }

    private CultureDefinition? GetCultureByCode(string code)
    {
        return AvailableCultures.FirstOrDefault(c => c.Code.Equals(code));
    }

    private List<CultureDefinition> BuildAvailableCultures()
    {
        var cultureDefinitions = new HashSet<CultureDefinition>();
        var culturesToLoad = new[] { "fr", "en" };

        foreach (var cultureCode in culturesToLoad)
        {
            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(cultureCode);
                cultureDefinitions.Add(new CultureDefinition
                {
                    Code = cultureInfo.Name,
                    Description = cultureInfo.NativeName,
                    CultureInfo = cultureInfo
                });
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning("Culture \'{CultureCode}\' not found: {ExMessage}", cultureCode, ex.Message);
            }
        }

        if (!cultureDefinitions.Any())
        {
            _logger.LogWarning("No valid cultures found. Adding invariant culture");
            var invariantCulture = CultureInfo.InvariantCulture;
            cultureDefinitions.Add(new CultureDefinition
            {
                Code = invariantCulture.Name,
                Description = invariantCulture.NativeName,
                CultureInfo = invariantCulture
            });
        }

        return cultureDefinitions.ToList();
    }

    public void SetCurrentCulture(CultureDefinition cultureDefinition)
    {
        DoSetCurrentCulture(cultureDefinition);
        
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(
            settings => settings.CultureCode = CurrentCultureDefinition.Code);
        
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
        
    public void DoSetCurrentCulture(CultureDefinition cultureDefinition)
    {
        _cultureDefinition.OnNext(cultureDefinition);
        
        if (Equals(InitialCultureInfo.Parent, CurrentCultureDefinition.CultureInfo))
        {
            Thread.CurrentThread.CurrentCulture = InitialCultureInfo;
            Thread.CurrentThread.CurrentUICulture = InitialCultureInfo;
        }
        else
        {
            Thread.CurrentThread.CurrentCulture = CurrentCultureDefinition.CultureInfo;
            Thread.CurrentThread.CurrentUICulture = CurrentCultureDefinition.CultureInfo;
        }
    }

    public ReadOnlyCollection<CultureDefinition> GetAvailableCultures()
    {
        return AvailableCultures.AsReadOnly();
    }
    
    public string this[string key]
    {
        get
        {
            string? result = _resourceManager.GetString(key, CurrentCultureDefinition.CultureInfo) ?? 
                             _resourceManager.GetString(key.Replace("_", "."), CurrentCultureDefinition.CultureInfo);

            return result!;
        }
    }
    
    public string GetMonthName(int monthNumber0To11)
    {
        string key = monthNumber0To11 switch
        {
            0 => "General_January",
            1 => "General_February",
            2 => "General_March",
            3 => "General_April",
            4 => "General_May",
            5 => "General_June",
            6 => "General_July",
            7 => "General_August",
            8 => "General_September",
            9 => "General_October",
            10 => "General_November",
            11 => "General_December",
            _ => throw new ArgumentOutOfRangeException(nameof(monthNumber0To11))
        };

        return this[key];
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}