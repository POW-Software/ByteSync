using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Interfaces;

namespace ByteSync.Services.Converters;

public class LobbyMemberStatusConverter : IValueConverter
{
    private ILocalizationService _localizationService;

    public LobbyMemberStatusConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public LobbyMemberStatusConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
        
        var lobbyMemberStatus = value as LobbyMemberStatuses?;
        
        if (lobbyMemberStatus == null)
        {
            return "";
        }

        string status;
        switch (lobbyMemberStatus)
        {
            case LobbyMemberStatuses.WaitingForJoin:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_WaitingForJoin)];
                break;
            case LobbyMemberStatuses.Joined:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_Joined)];
                break;
            case LobbyMemberStatuses.TrustCheckError:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_TrustCheckError)];
                break;
            case LobbyMemberStatuses.SecurityChecksSuccess:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_CheckSuccess)];
                break;
            case LobbyMemberStatuses.SecurityChecksInProgress:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_SecurityChecksInProgress)];
                break;
            case LobbyMemberStatuses.CrossCheckError:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_CrossCheckError)];
                break;
            case LobbyMemberStatuses.JoinedSession:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_JoinedSession)];
                break;
            case LobbyMemberStatuses.CreatedSession:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_CreatedSession)];
                break;
            case LobbyMemberStatuses.UnexpectedError:
                status = _localizationService[nameof(Resources.LobbyMemberStatus_UnexpectedError)];
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }

        return status;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}