using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Cloud.Managing;

public class DataTypeViewModel
{
    private readonly ILocalizationService _localizationService;

    public DataTypeViewModel(DataTypes dataType, ILocalizationService localizationService)
    {
        DataType = dataType;
        _localizationService = localizationService;

        UpdateDescription();
    }

    [Reactive]
    public DataTypes DataType { get; set; }
    
    [Reactive]
    public string? Description { get; set; }
    
    internal void UpdateDescription()
    {
        Description = DataType switch
        {
            DataTypes.FilesDirectories => _localizationService["DataTypes_FilesDirectories"],
            DataTypes.Files => _localizationService["DataTypes_Files"],
            DataTypes.Directories => _localizationService["DataTypes_Directories"],
            _ => ""
        };
    }
}