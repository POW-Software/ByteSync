using Autofac;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ByteSync.Interfaces;
using ByteSync.Services.Converters;

namespace ByteSync.Services.Localizations;

// https://www.sakya.it/wordpress/avalonia-ui-framework-localization/
public class LocExtension : MarkupExtension
{
    public LocExtension(string key) : this (key, null)
    {
            
    }
        
    public LocExtension(string key, bool? uppercase)
    {
        this.Key = key;
        this.UpperCase = uppercase;
    }

    public string Key { get; set; }

    public string? Context { get; set; }
        
    public bool? UpperCase { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var keyToUse = Key;
        if (!string.IsNullOrWhiteSpace(Context))
        {
            keyToUse = $"{Context}/{Key}";
        }

        if (Design.IsDesignMode)
        {
            return keyToUse;
        }

        //var c = ContainerProvider.Container;
        //if (c == null)
        //{
        //    return "Container is null";
        //}

        //return keyToUse;

        IValueConverter? converter = null;
        if (UpperCase != null)
        {
            converter = new CaseConverter(UpperCase);
        }
            
        var localizationService = ContainerProvider.Container.Resolve<ILocalizationService>()!;
        var binding = new ReflectionBindingExtension($"[{keyToUse}]")
        {
            Mode = BindingMode.OneWay,
            Source = localizationService,
            Converter = converter
        };

        return binding.ProvideValue(serviceProvider);
    }
}