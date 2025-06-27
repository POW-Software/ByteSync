using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Services.Communications;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Services.Communications.SignalR;

public class HubPushHandler2 : IHubPushHandler2
{
    private readonly SemaphoreSlim _semaphore;
    
    private HubConnection? _connection;
    
    public HubPushHandler2(IConnectionService connectionService, ILogger<HubPushHandler2> logger)
    {
        Logger = logger;
        _semaphore = new(1, 1);
        
        connectionService.Connection
            .Where(connection => connection != null)
            .SelectMany(async connection =>
            {
                await SetConnection(connection!);
                return connection;
            })
            .Subscribe();
    }
    
    internal static ILogger<HubPushHandler2> Logger { get; private set; } = null!;

    private readonly List<ISubjectInfo> _subjectInfos =
    [
        new SubjectInfo<CloudSessionResult, ValidateJoinCloudSessionParameters>(nameof(IHubByteSyncPush.YouJoinedSession)),
        new SubjectInfo<string>(nameof(IHubByteSyncPush.YouGaveAWrongPassword)),
        new SubjectInfo<SessionMemberInfoDTO>(nameof(IHubByteSyncPush.MemberJoinedSession)),
        new SubjectInfo<SessionMemberInfoDTO>(nameof(IHubByteSyncPush.MemberQuittedSession)),
        new SubjectInfo<SessionSettingsUpdatedDTO>(nameof(IHubByteSyncPush.SessionSettingsUpdated)),
        new SubjectInfo<CloudSessionFatalError>(nameof(IHubByteSyncPush.SessionOnFatalError)),
        new SubjectInfo<InventoryStartedDTO>(nameof(IHubByteSyncPush.InventoryStarted)),
        new SubjectInfo<DataNodeDTO>(nameof(IHubByteSyncPush.DataNodeAdded)),
        new SubjectInfo<DataNodeDTO>(nameof(IHubByteSyncPush.DataNodeRemoved)),
        new SubjectInfo<DataSourceDTO>(nameof(IHubByteSyncPush.DataSourceAdded)),
        new SubjectInfo<DataSourceDTO>(nameof(IHubByteSyncPush.DataSourceRemoved)),
        new SubjectInfo<FileTransferPush>(nameof(IHubByteSyncPush.FilePartUploaded)),
        new SubjectInfo<FileTransferPush>(nameof(IHubByteSyncPush.UploadFinished)),
        new SubjectInfo<string>(nameof(IHubByteSyncPush.OnReconnected)),
        new SubjectInfo<Synchronization>(nameof(IHubByteSyncPush.SynchronizationStarted)),
        new SubjectInfo<Synchronization>(nameof(IHubByteSyncPush.SynchronizationUpdated)),
        new SubjectInfo<UpdateSessionMemberGeneralStatusParameters>(nameof(IHubByteSyncPush.SessionMemberGeneralStatusUpdated)),
        new SubjectInfo<SynchronizationProgressPush>(nameof(IHubByteSyncPush.SynchronizationProgressUpdated)),
        new SubjectInfo<string, string, PublicKeyInfo>(nameof(IHubByteSyncPush.AskPublicKeyCheckData)),
        new SubjectInfo<string, PublicKeyCheckData>(nameof(IHubByteSyncPush.GiveMemberPublicKeyCheckData)),
        new SubjectInfo<RequestTrustProcessParameters>(nameof(IHubByteSyncPush.RequestTrustPublicKey)),
        new SubjectInfo<DigitalSignatureCheckInfo>(nameof(IHubByteSyncPush.RequestCheckDigitalSignature)),
        new SubjectInfo<PublicKeyValidationParameters>(nameof(IHubByteSyncPush.InformPublicKeyValidationIsFinished)),
        new SubjectInfo<AskCloudSessionPasswordExchangeKeyPush>(nameof(IHubByteSyncPush.AskCloudSessionPasswordExchangeKey)),
        new SubjectInfo<GiveCloudSessionPasswordExchangeKeyParameters>(nameof(IHubByteSyncPush.GiveCloudSessionPasswordExchangeKey)),
        new SubjectInfo<AskJoinCloudSessionParameters>(nameof(IHubByteSyncPush.CheckCloudSessionPasswordExchangeKey)),
        new SubjectInfo<BaseSessionDto>(nameof(IHubByteSyncPush.SessionResetted)),
        new SubjectInfo<string, LobbyMemberInfo>(nameof(IHubByteSyncPush.MemberJoinedLobby)),
        new SubjectInfo<string, string>(nameof(IHubByteSyncPush.MemberQuittedLobby)),
        new SubjectInfo<string, LobbyCheckInfo>(nameof(IHubByteSyncPush.LobbyCheckInfosSent)),
        new SubjectInfo<string, string, LobbyMemberStatuses>(nameof(IHubByteSyncPush.LobbyMemberStatusUpdated)),
        new SubjectInfo<LobbyCloudSessionCredentials>(nameof(IHubByteSyncPush.LobbyCloudSessionCredentialsSent))
    ];
    
    public Subject<(CloudSessionResult, ValidateJoinCloudSessionParameters)> YouJoinedSession => 
        GetSubject<CloudSessionResult, ValidateJoinCloudSessionParameters>(nameof(IHubByteSyncPush.YouJoinedSession));
    
    public Subject<string> YouGaveAWrongPassword => 
        GetSubject<string>(nameof(IHubByteSyncPush.YouGaveAWrongPassword));
    
    public Subject<SessionMemberInfoDTO> MemberJoinedSession => 
        GetSubject<SessionMemberInfoDTO>(nameof(IHubByteSyncPush.MemberJoinedSession));
    
    public Subject<SessionMemberInfoDTO> MemberQuittedSession => 
        GetSubject<SessionMemberInfoDTO>(nameof(IHubByteSyncPush.MemberQuittedSession));
    
    public Subject<SessionSettingsUpdatedDTO> SessionSettingsUpdated => 
        GetSubject<SessionSettingsUpdatedDTO>(nameof(IHubByteSyncPush.SessionSettingsUpdated));
    
    public Subject<CloudSessionFatalError> SessionOnFatalError => 
        GetSubject<CloudSessionFatalError>(nameof(IHubByteSyncPush.SessionOnFatalError));
    
    public Subject<InventoryStartedDTO> InventoryStarted =>
        GetSubject<InventoryStartedDTO>(nameof(IHubByteSyncPush.InventoryStarted));

    public Subject<DataNodeDTO> DataNodeAdded =>
        GetSubject<DataNodeDTO>(nameof(IHubByteSyncPush.DataNodeAdded));

    public Subject<DataNodeDTO> DataNodeRemoved =>
        GetSubject<DataNodeDTO>(nameof(IHubByteSyncPush.DataNodeRemoved));

    public Subject<DataSourceDTO> DataSourceAdded =>
        GetSubject<DataSourceDTO>(nameof(IHubByteSyncPush.DataSourceAdded));
    
    public Subject<DataSourceDTO> DataSourceRemoved => 
        GetSubject<DataSourceDTO>(nameof(IHubByteSyncPush.DataSourceRemoved));
    
    public Subject<FileTransferPush> FilePartUploaded => 
        GetSubject<FileTransferPush>(nameof(IHubByteSyncPush.FilePartUploaded));
    
    public Subject<FileTransferPush> UploadFinished => 
        GetSubject<FileTransferPush>(nameof(IHubByteSyncPush.UploadFinished));
    
    public Subject<string> OnReconnected => 
        GetSubject<string>(nameof(IHubByteSyncPush.OnReconnected));
    
    public Subject<Synchronization> SynchronizationStarted => 
        GetSubject<Synchronization>(nameof(IHubByteSyncPush.SynchronizationStarted));
    
    public Subject<Synchronization> SynchronizationUpdated => 
        GetSubject<Synchronization>(nameof(IHubByteSyncPush.SynchronizationUpdated));
    
    public Subject<UpdateSessionMemberGeneralStatusParameters> SessionMemberGeneralStatusUpdated => 
        GetSubject<UpdateSessionMemberGeneralStatusParameters>(nameof(IHubByteSyncPush.SessionMemberGeneralStatusUpdated));
    
    public Subject<SynchronizationProgressPush> SynchronizationProgressUpdated => 
        GetSubject<SynchronizationProgressPush>(nameof(IHubByteSyncPush.SynchronizationProgressUpdated));
    
    public Subject<(string, string, PublicKeyInfo)> AskPublicKeyCheckData => 
        GetSubject<string, string, PublicKeyInfo>(nameof(IHubByteSyncPush.AskPublicKeyCheckData));
    
    public Subject<(string, PublicKeyCheckData)> GiveMemberPublicKeyCheckData => 
        GetSubject<string, PublicKeyCheckData>(nameof(IHubByteSyncPush.GiveMemberPublicKeyCheckData));
    
    public Subject<RequestTrustProcessParameters> RequestTrustPublicKey => 
        GetSubject<RequestTrustProcessParameters>(nameof(IHubByteSyncPush.RequestTrustPublicKey));
    
    public Subject<DigitalSignatureCheckInfo> RequestCheckDigitalSignature => 
        GetSubject<DigitalSignatureCheckInfo>(nameof(IHubByteSyncPush.RequestCheckDigitalSignature));
    
    public Subject<PublicKeyValidationParameters> InformPublicKeyValidationIsFinished => 
        GetSubject<PublicKeyValidationParameters>(nameof(IHubByteSyncPush.InformPublicKeyValidationIsFinished));
    
    public Subject<AskCloudSessionPasswordExchangeKeyPush> AskCloudSessionPasswordExchangeKey=> 
        GetSubject<AskCloudSessionPasswordExchangeKeyPush>(nameof(IHubByteSyncPush.AskCloudSessionPasswordExchangeKey));
    
    public Subject<GiveCloudSessionPasswordExchangeKeyParameters> GiveCloudSessionPasswordExchangeKey => 
        GetSubject<GiveCloudSessionPasswordExchangeKeyParameters>(nameof(IHubByteSyncPush.GiveCloudSessionPasswordExchangeKey));
    
    public Subject<AskJoinCloudSessionParameters> CheckCloudSessionPasswordExchangeKey => 
        GetSubject<AskJoinCloudSessionParameters>(nameof(IHubByteSyncPush.CheckCloudSessionPasswordExchangeKey));
    
    public Subject<BaseSessionDto> SessionResetted => 
        GetSubject<BaseSessionDto>(nameof(IHubByteSyncPush.SessionResetted));
    
    public Subject<(string, LobbyMemberInfo)> MemberJoinedLobby => 
        GetSubject<string, LobbyMemberInfo>(nameof(IHubByteSyncPush.MemberJoinedLobby));
    
    public Subject<(string, string)> MemberQuittedLobby => 
        GetSubject<string, string>(nameof(IHubByteSyncPush.MemberQuittedLobby));
    
    public Subject<(string, LobbyCheckInfo)> LobbyCheckInfosSent => 
        GetSubject<string, LobbyCheckInfo>(nameof(IHubByteSyncPush.LobbyCheckInfosSent));
    
    public Subject<(string, string, LobbyMemberStatuses)> LobbyMemberStatusUpdated => 
        GetSubject<string, string, LobbyMemberStatuses>(nameof(IHubByteSyncPush.LobbyMemberStatusUpdated));
    
    public Subject<LobbyCloudSessionCredentials> LobbyCloudSessionCredentialsSent => 
        GetSubject<LobbyCloudSessionCredentials>(nameof(IHubByteSyncPush.LobbyCloudSessionCredentialsSent));

    private async Task SetConnection(HubConnection connection)
    {
        await _semaphore.WaitAsync();
        
        try 
        {
            if (_connection != null)
            {
                foreach (var subjectInfo in _subjectInfos)
                {
                    _connection.Remove(subjectInfo.MethodName);
                }
            }
            
            Logger.LogInformation("HubPushHandler2: Setting connection");

            _connection = connection;
            SetupObservables(connection);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private Subject<T> GetSubject<T>(string methodName)
    {
        var subjectInfo = _subjectInfos.OfType<SubjectInfo<T>>().FirstOrDefault(s => s.MethodName == methodName);

        if (subjectInfo == null)
        {
            throw new ArgumentOutOfRangeException(nameof(methodName),
                $"Can not find a SubjectInfo with methodName '{methodName}' and type '{typeof(T).Name}'");
        }
        else
        {
            return subjectInfo.Subject;
        }
    }
    
    private Subject<(T1, T2)> GetSubject<T1, T2>(string methodName)
    {
        var subjectInfo = _subjectInfos.OfType<SubjectInfo<T1, T2>>().FirstOrDefault(s => s.MethodName == methodName);

        if (subjectInfo == null)
        {
            throw new ArgumentOutOfRangeException(nameof(methodName),
                $"Can not find a SubjectInfo with methodName '{methodName}' and types '{typeof(T1).Name}', '{typeof(T2).Name}'");
        }
        else
        {
            return subjectInfo.Subject;
        }
    }
    
    private Subject<(T1, T2, T3)> GetSubject<T1, T2, T3>(string methodName)
    {
        var subjectInfo = _subjectInfos.OfType<SubjectInfo<T1, T2, T3>>().FirstOrDefault(s => s.MethodName == methodName);

        if (subjectInfo == null)
        {
            throw new ArgumentOutOfRangeException(nameof(methodName),
                $"Can not find a SubjectInfo with methodName '{methodName}' and types " +
                $"'{typeof(T1).Name}', '{typeof(T2).Name}', '{typeof(T3).Name}'");
        }
        else
        {
            return subjectInfo.Subject;
        }
    }

    private void SetupObservables(HubConnection connection)
    {
        foreach (var subjectInfo in _subjectInfos)
        {
            subjectInfo.SetupOnCall(connection);
        }
    }
}

public interface ISubjectInfo
{
    string MethodName { get; }
    void SetupOnCall(HubConnection connection);
    
    // protected void LogDebug(string methodName)
    // {
    //     Log.Debug("HubPushHandler2.{methodName}", methodName);
    // }
}

public class SubjectInfo<T> : ISubjectInfo
{
    public Subject<T> Subject { get; }
    public string MethodName { get; }

    public SubjectInfo(string methodName)
    {
        Subject = new Subject<T>();
        MethodName = methodName;
    }

    public void SetupOnCall(HubConnection connection)
    {
        connection.On<T>(MethodName, value =>
        {
            this.LogDebug(MethodName);
            Subject.OnNext(value);
        });
    }
}

public class SubjectInfo<T1, T2> : ISubjectInfo
{
    public Subject<(T1, T2)> Subject { get; }
    public string MethodName { get; }

    public SubjectInfo(string methodName)
    {
        Subject = new Subject<(T1, T2)>();
        MethodName = methodName;
    }

    public void SetupOnCall(HubConnection connection)
    {
        connection.On<T1, T2>(MethodName, (arg1, arg2) =>
        {
            this.LogDebug(MethodName);
            Subject.OnNext((arg1, arg2));
        });
    }
}

public class SubjectInfo<T1, T2, T3> : ISubjectInfo
{
    public Subject<(T1, T2, T3)> Subject { get; }
    public string MethodName { get; }

    public SubjectInfo(string methodName)
    {
        Subject = new Subject<(T1, T2, T3)>();
        MethodName = methodName;
    }

    public void SetupOnCall(HubConnection connection)
    {
        connection.On<T1, T2, T3>(MethodName, (arg1, arg2, arg3) =>
        {
            this.LogDebug(MethodName);
            Subject.OnNext((arg1, arg2, arg3));
        });
    }
    
    // private void LogDebug(string methodName)
    // {
    //     Log.Debug("HubPushHandler2.{methodName}", methodName);
    // }
}

internal static class SubjectInfoHelper
{
    internal static void LogDebug(this ISubjectInfo subjectInfo, string methodName)
    {
        HubPushHandler2.Logger.LogDebug("HubPushHandler2.{methodName}", methodName);
    }
}