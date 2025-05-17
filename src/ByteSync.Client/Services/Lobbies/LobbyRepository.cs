using System.Runtime.CompilerServices;
using System.Threading;
using ByteSync.Business.Events;
using ByteSync.Business.Lobbies;
using ByteSync.Business.Profiles;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Controls;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Lobbies;
using ByteSync.ViewModels.Lobbies;
using Prism.Events;
using Splat;

namespace ByteSync.Services.Lobbies;

public class LobbyRepository : BaseRepository<LobbyDetails>, ILobbyRepository
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICloudProxy _connectionManager;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ILobbyMemberViewModelFactory _lobbyMemberViewModelFactory;
    private readonly ILobbyApiClient _lobbyApiClient;

    internal const string LOCAL_LOBBY_PREFIX = "Local_Lobby_";
    
    public LobbyRepository(ILobbyMemberViewModelFactory lobbyMemberViewModelFactory, IEventAggregator eventAggregator, ICloudProxy connectionManager, 
        IPublicKeysManager publicKeysManager, ILobbyApiClient lobbyApiClient, ILogger<LobbyRepository> logger) :
            base(logger)
    {
        _lobbyMemberViewModelFactory = lobbyMemberViewModelFactory;

        _eventAggregator = eventAggregator;
        _connectionManager = connectionManager;
        _publicKeysManager = publicKeysManager;
        _lobbyApiClient = lobbyApiClient;

        _eventAggregator.GetEvent<OnServerMemberJoinedLobby>().Subscribe(OnMemberJoinedLobby);
        _eventAggregator.GetEvent<OnServerMemberQuittedLobby>().Subscribe(OnMemberQuittedLobby);
        _eventAggregator.GetEvent<OnServerLobbyCheckInfosSent>().Subscribe(OnLobbyCheckInfosSent);
        _eventAggregator.GetEvent<OnServerLobbyMemberStatusUpdated>().Subscribe(OnLobbyMemberStatusUpdated);
        _eventAggregator.GetEvent<OnServerLobbyCloudSessionCredentialsSent>().Subscribe(OnLobbyCloudSessionCredentialsSent);

        _connectionManager.HubPushHandler2.MemberJoinedSession
            .Subscribe(OnMemberJoinedSession);
    }

    private async void OnMemberJoinedLobby((string lobbyId, LobbyMemberInfo memberInfo) tuple)
    {
        try
        {
            var areAllConnected = false;

            LobbyMemberViewModel? member = null;
            await RunAsync(tuple.lobbyId, details =>
            {
                member = details.LobbyMembersViewModels.SingleOrDefault(m => m.LobbyMember.IsSameThan(tuple.memberInfo));
            });
            
            if (member != null)
            {
                member.LobbyMember.LobbyMemberInfo = tuple.memberInfo;
                // todo uihelper
                // _uiHelper.ExecuteOnUi(() => { member.LobbyMember.LobbyMemberInfo = tuple.memberInfo; }).Wait();
            }

            await RunAsync(tuple.lobbyId, details =>
            {
                areAllConnected = details.LobbyMembersViewModels.All(lmvm => lmvm.LobbyMember.LobbyMemberInfo != null);
            });

            if (areAllConnected)
            {
                var lobbyManager = Locator.Current.GetService<ILobbyManager>()!;

                await lobbyManager.RunSecurityCheckAsync(tuple.lobbyId);
            }
        }
        catch (Exception ex)
        {
            await HandleError(ex, tuple.lobbyId);
        }
    }

    private async void OnMemberQuittedLobby((string lobbyId, string clientInstanceId) tuple)
    {
        try
        {
            await RunAsync(tuple.lobbyId, details =>
            {
                var member = details.LobbyMembersViewModels
                    .SingleOrDefault(m => m.LobbyMember.LobbyMemberInfo != null &&
                                          m.LobbyMember.LobbyMemberInfo.ClientInstanceId.Equals(tuple.clientInstanceId));

                if (member != null)
                {
                    member.LobbyMember.LobbyMemberInfo = null;
                    // todo uihelper
                    // _uiHelper.ExecuteOnUi(() => { member.LobbyMember.LobbyMemberInfo = null; }).Wait();
                }
            });
        }
        catch (Exception ex)
        {
            await HandleError(ex, tuple.lobbyId);
        }
    }
    
    private async void OnLobbyCheckInfosSent((string lobbyId, LobbyCheckInfo lobbyCheckInfo) tuple)
    {
        try
        {
            await HandleOnLobbyCheckInfosSent(tuple.lobbyId, tuple.lobbyCheckInfo);
        }
        catch (Exception ex)
        {
            await HandleError(ex, tuple.lobbyId);
        }
    }

    private async Task HandleOnLobbyCheckInfosSent(string lobbyId, LobbyCheckInfo lobbyCheckInfo)
    {
        var isWaitOK = await WaitAsync(lobbyId, details => details.TrustCheckWaitHandle, details => details.ProfileDetails.Options.MaxLobbyLifeTime);
        if (!isWaitOK)
        {
            return;
        }

        await RunAsync(lobbyId, details =>
        {
            var sender = details.CheckedOtherMembers
                .SingleOrDefault(com =>
                    com.ClientInstanceId.Equals(lobbyCheckInfo.SenderClientInstanceId));

            if (sender != null)
            {
                var lobbyCheckKeyComputer = new LobbyCheckKeyComputer(lobbyId, sender.LobbyMember);

                var myProfileMember = details.ProfileDetails.Members.Single(m => m.ProfileClientId.Equals(
                    details.Profile.ProfileClientId));
                var expectedKey = lobbyCheckKeyComputer.ComputeKey(myProfileMember);

                var myRecipient = lobbyCheckInfo.Recipients.Single(r => r.ClientInstanceId.Equals(_connectionManager.ClientInstanceId));
                var providedString = _publicKeysManager.DecryptString(myRecipient.CheckData);

                sender.IsCheckSuccess = expectedKey.Equals(providedString);
            }

            if (ComputeAreAllOtherMembersCheckSuccessSet(details))
            {
                details.IsSecurityCheckSuccess = ComputeAreAllOtherMembersCheckSuccessOK(details);
                details.AllOtherMembersCheckedWaitHandle.Set();

                if (details.IsSecurityCheckSuccess.Value)
                {
                    DoUpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.SecurityChecksSuccess, details);
                }
                else
                {
                    DoUpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.CrossCheckError, details);
                }
            }
        });
    }

    private async void OnLobbyMemberStatusUpdated((string lobbyId, string clientInstanceId, LobbyMemberStatuses lobbyMemberStatus) tuple)
    {
        try
        {
            await RunAsync(tuple.lobbyId, details =>
            {
                var member = details.LobbyMembersViewModels
                    .SingleOrDefault(m => m.LobbyMember.LobbyMemberInfo != null &&
                                          m.LobbyMember.LobbyMemberInfo.ClientInstanceId.Equals(tuple.clientInstanceId));

                if (tuple.lobbyMemberStatus.In(LobbyMemberStatuses.CrossCheckError,
                        LobbyMemberStatuses.TrustCheckError))
                {
                    details.IsSecurityCheckSuccess = false;
                    details.AllOtherMembersCheckedWaitHandle.Set();
                }

                if (member != null)
                {
                    member.LobbyMember.Status = tuple.lobbyMemberStatus;
                    // todo uihelper
                    // _uiHelper.ExecuteOnUi(() => { member.LobbyMember.Status = tuple.lobbyMemberStatus; }).Wait();
                }
            });
        }
        catch (Exception ex)
        {
            await HandleError(ex, tuple.lobbyId);
        }
    }
    
    private async void OnLobbyCloudSessionCredentialsSent(LobbyCloudSessionCredentials lobbyCloudSessionCredentials)
    {
        try
        {
            var isLobbyIdOk = false;

            await RunAsync(lobbyCloudSessionCredentials.LobbyId, _ => { isLobbyIdOk = true; });

            if (isLobbyIdOk)
            {
                var lobbyManager = Locator.Current.GetService<ILobbyManager>()!;

                await lobbyManager.OnLobbyCloudSessionCredentialsSent(lobbyCloudSessionCredentials);
            }
        }
        catch (Exception ex)
        {
            await HandleError(ex, lobbyCloudSessionCredentials.LobbyId);
        }
    }
    
    private async void OnMemberJoinedSession(SessionMemberInfoDTO sessionMemberInfo)
    {
        try
        {
            if (sessionMemberInfo.LobbyId == null)
            {
                return;
            }

            if (!IsDataSet())
            {
                // Peut-être reçu alors qu'on a déjà quitté le Lobby et rejoint la Session
                return;
            }
            
            await RunAsync(sessionMemberInfo.LobbyId, details =>
            {
                if (details.ExpectedJoiningMember == null)
                {
                    return;
                }

                if (!details.ExpectedJoiningMember.SessionId.Equals(sessionMemberInfo.SessionId))
                {
                    return;
                }

                if (details.ExpectedJoiningMember.ProfileClientId
                        .Equals(sessionMemberInfo.ProfileClientId) &&
                    details.ExpectedJoiningMember.ClientInstanceId
                        .Equals(sessionMemberInfo.ClientInstanceId))
                {
                    details.ExpectedJoiningMember.LobbyMember.Status = LobbyMemberStatuses.JoinedSession;
                    // todo uihelper
                    // _uiHelper.ExecuteOnUi(() => { details.ExpectedJoiningMember.LobbyMember.Status = LobbyMemberStatuses.JoinedSession; }).Wait();
                    
                    details.ExpectedMemberWaitHandler.Set();
                }
            });
        }
        catch (Exception ex)
        {
            await HandleError(ex, sessionMemberInfo.LobbyId);
        }
    }
    
    public async Task SetTrustCheckSuccess(string lobbyId)
    {
        await RunAsync(lobbyId, details =>
        {
            details.CheckedOtherMembers.Clear();

            foreach (var lobbyMemberViewModel in details.LobbyMembersViewModels)
            {
                if (lobbyMemberViewModel.LobbyMember.LobbyMemberInfo != null &&
                    !lobbyMemberViewModel.LobbyMember.LobbyMemberInfo.ClientInstanceId.Equals(_connectionManager.ClientInstanceId))
                {
                    details.CheckedOtherMembers.Add(new CheckedOtherMember(lobbyMemberViewModel.LobbyMember, null));
                }
            }

            details.IsTrustSuccess = true;

            details.TrustCheckWaitHandle.Set();
        });
    }

    public async Task SetTrustCheckError(string lobbyId)
    {
        await RunAsync(lobbyId, details =>
        {
            details.IsTrustSuccess = false;

            details.TrustCheckWaitHandle.Set();
            
            DoUpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.TrustCheckError, details);
        });
    }

    public async Task<bool> AreAllOtherMembersCheckSuccess(string lobbyId)
    {
        var result = false;
        
        await RunAsync(lobbyId, details => { result = ComputeAreAllOtherMembersCheckSuccessOK(details); });

        return result;
    }

    private bool ComputeAreAllOtherMembersCheckSuccessSet(LobbyDetails details)
    {
        var result = details.CheckedOtherMembers.Count == details.OtherLobbyMembers.Count &&
                     details.CheckedOtherMembers.All(com => com.IsCheckSuccess != null);
        
        return result;
    }

    private bool ComputeAreAllOtherMembersCheckSuccessOK(LobbyDetails details)
    {
        var result = details.CheckedOtherMembers.Count == details.OtherLobbyMembers.Count &&
                     details.CheckedOtherMembers.All(com => com.IsCheckSuccess.GetValueOrDefault());
        
        return result;
    }

    public async Task<bool> IsEverythingOKBeforeSession(string lobbyId)
    {
        var result = false;
        
        await RunAsync(lobbyId, details =>
        {
            // Statut Local OK, Statut des Autres OK, tous les checks terminés et OK
            
            var localLobbyMemberViewModel = details.LocalLobbyMemberViewModel;
            var isOK = localLobbyMemberViewModel.LobbyMember.Status.In(LobbyMemberStatuses.SecurityChecksSuccess);

            isOK &= details.OtherLobbyMembers.All(lm => lm.Status.In(LobbyMemberStatuses.SecurityChecksSuccess, LobbyMemberStatuses.CreatedSession,
                LobbyMemberStatuses.JoinedSession));

            isOK &= ComputeAreAllOtherMembersCheckSuccessOK(details);

            isOK &= details.IsTrustSuccess.GetValueOrDefault();
            isOK &= details.IsSecurityCheckSuccess.GetValueOrDefault();

            result = isOK;
        });

        return result;
    }

    public async Task UpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses status)
    {
        await RunAsync(lobbyId, details =>
        {
            try
            {
                DoUpdateLobbyMemberStatus(lobbyId, status, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateLobbyMemberStatus");
            }
        });
    }

    public async Task<bool> IsFirstLobbyMember(string lobbyId)
    {
        var isFirstLobbyMember = false;
        
        await RunAsync(lobbyId, details =>
        {
            isFirstLobbyMember = details.LobbyMembersViewModels.First().LobbyMember.ProfileClientId.Equals(details.LocalProfileClientId);
        });

        return isFirstLobbyMember;
    }

    public async Task SetExpectedMember(LobbySessionExpectedMember lobbySessionExpectedMember)
    {
        await RunAsync(lobbySessionExpectedMember.LobbyId, details =>
        {
            details.ExpectedJoiningMember = lobbySessionExpectedMember;
        });
    }

    public async Task<RunCloudSessionProfileInfo> BuildCloudProfileSessionDetails(string lobbyId)
    {
        return await GetAsync(lobbyId, details =>
        {
            LobbySessionModes? lobbySessionModes;
            var firstMemberJoinLobbyMode = details.AllLobbyMembers.First().LobbyMemberInfo?.JoinLobbyMode;

            switch (firstMemberJoinLobbyMode)
            {
                case null:
                    lobbySessionModes = null;
                    break;
                case JoinLobbyModes.RunInventory:
                    lobbySessionModes = LobbySessionModes.RunInventory;
                    break;
                case JoinLobbyModes.RunSynchronization:
                    lobbySessionModes = LobbySessionModes.RunSynchronization;
                    break;
                default :
                    throw new ApplicationException($"Unexpected JoinLobbyModes '{firstMemberJoinLobbyMode}'");
            }

            var lobbySessionDetails = new RunCloudSessionProfileInfo(details.LobbyId, details.Profile, details.ProfileDetails, lobbySessionModes);

            return lobbySessionDetails;
        });
    }

    private void DoUpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses status, LobbyDetails details)
    {
        var localLobbyMemberViewModel = details.LocalLobbyMemberViewModel;

        localLobbyMemberViewModel.LobbyMember.Status = status;

        _lobbyApiClient.UpdateLobbyMemberStatus(lobbyId, status).Wait();
    }

    public async Task SetCloudSessionProfileDetails(CloudSessionProfile cloudSessionProfile, CloudSessionProfileDetails cloudSessionProfileDetails,
        LobbyInfo? lobbyInfo)
    {
        var newId = lobbyInfo?.LobbyId ?? $"{LOCAL_LOBBY_PREFIX}{Guid.NewGuid()}";
        
        await ResetDataAsync(newId, newLobbyDetails =>
        {
            newLobbyDetails.Profile = cloudSessionProfile;
            
            newLobbyDetails.ProfileDetails = cloudSessionProfileDetails;

            newLobbyDetails.LocalClientInstanceId = _connectionManager.ClientInstanceId;

            newLobbyDetails.LocalProfileClientId = cloudSessionProfile.ProfileClientId;

            foreach (var cloudSessionProfileMember in cloudSessionProfileDetails.Members)
            {
                var lobbyMemberInfo = lobbyInfo?.ConnectedMembers
                    .SingleOrDefault(cm => cm.ProfileClientId.Equals(cloudSessionProfileMember.ProfileClientId));

                var lobbyMember = new LobbyMember(cloudSessionProfileMember, lobbyMemberInfo);

                newLobbyDetails.LobbyMembersViewModels.Add(_lobbyMemberViewModelFactory.CreateLobbyMemberViewModel(lobbyMember));
            }
        });
    }

    protected override string GetDataId(LobbyDetails data)
    {
        return data.LobbyId;
    }

    protected override ManualResetEvent GetEndEvent(LobbyDetails data)
    {
        return data.LobbyEndedEvent;
    }
    
    private async Task HandleError(Exception exception, string? lobbyId, [CallerMemberName] string caller = "")
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogError(exception, caller);

        if (exception is ArgumentOutOfRangeException && exception.Message.Contains("dataId is not expected", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        
        if (lobbyId != null)
        {
            try
            {
                var details = Get(lobbyId, details => details);

                details.LobbyEndedEvent.Set();
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "RunSecurityCheck // LobbyEndedEvent.Set()");
            }

            try
            {
                await UpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.UnexpectedError);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "RunSecurityCheck // UpdateLobbyMemberStatus");
            }
        }
    }
}