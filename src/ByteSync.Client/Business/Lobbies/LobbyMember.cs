using System.ComponentModel;
using System.Runtime.CompilerServices;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Helpers;

namespace ByteSync.Business.Lobbies;

public sealed class LobbyMember : INotifyPropertyChanged
{
    private LobbyMemberInfo? _lobbyMemberInfo;
    private LobbyMemberStatuses _status;

    public LobbyMember(CloudSessionProfileMember cloudSessionProfileMember, LobbyMemberInfo? lobbyMemberInfo)
    {
        CloudSessionProfileMember = cloudSessionProfileMember;
        Status = LobbyMemberStatuses.WaitingForJoin;
        LobbyMemberInfo = lobbyMemberInfo;
    }
    
    public CloudSessionProfileMember CloudSessionProfileMember { get; }

    public LobbyMemberInfo? LobbyMemberInfo
    {
        get => _lobbyMemberInfo;
        set
        {
            SetField(ref _lobbyMemberInfo, value);
            
            if (_lobbyMemberInfo != null)
            {
                Status = _lobbyMemberInfo.Status;
            }
            else
            {
                if (Status.In(LobbyMemberStatuses.Joined))
                {
                    Status = LobbyMemberStatuses.WaitingForJoin;
                }
            }
        }
    }

    public string ProfileClientId
    {
        get
        {
            return CloudSessionProfileMember.ProfileClientId;
        }
    }

    public string MachineName
    {
        get
        {
            return CloudSessionProfileMember.MachineName;
        }
    }

    public string MemberLetter
    {
        get
        {
            return CloudSessionProfileMember.Letter;
        }
    }

    public LobbyMemberStatuses Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool IsSameThan(LobbyMemberInfo memberInfo)
    {
        return ProfileClientId.Equals(memberInfo.ProfileClientId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}