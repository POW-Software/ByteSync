using System.ComponentModel;
using System.Runtime.CompilerServices;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.SessionMembers;

public class SessionMemberInfo : INotifyPropertyChanged
{
    public ByteSyncEndpoint Endpoint { get; set; }
    
    public SessionMemberPrivateData PrivateData { get; set; }
    
    public string SessionId { get; set; }
        

    public DateTimeOffset JoinedSessionOn { get; set; }
    
    // [Reactive]
    public int PositionInList { get; set; }
    
    public DateTimeOffset? LastLocalInventoryGlobalStatusUpdate { get; set; }

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
    
    public string? LobbyId { get; set; }
    
    public string? ProfileClientId { get; set; }
    
    public string ClientId => Endpoint.ClientId;
    
    public string ClientInstanceId => Endpoint.ClientInstanceId;
    
    public string IpAddress => Endpoint.IpAddress;
    
    public string MachineName => PrivateData.MachineName;
    
    public bool HasClientInstanceId(string clientInstanceId)
    {
        return Equals(ClientInstanceId, clientInstanceId);
    }
    
    protected bool Equals(SessionMemberInfoDTO other)
    {
        return ClientInstanceId == other.ClientInstanceId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SessionMemberInfoDTO)obj);
    }

    public override int GetHashCode()
    {
        return ClientInstanceId.GetHashCode();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}