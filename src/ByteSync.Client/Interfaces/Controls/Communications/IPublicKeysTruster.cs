﻿using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IPublicKeysTruster
{
    Task<JoinSessionResult> TrustAllMembersPublicKeys(string sessionId, CancellationToken cancellationToken = default);
    
    Task<List<string>?> TrustMissingMembersPublicKeys(string cloudSessionSessionId, CancellationToken cancellationToken = default);
    
    Task OnPublicKeyCheckDataAskedAsync((string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo) tuple);
    
    Task OnTrustPublicKeyRequestedAsync(RequestTrustProcessParameters requestTrustProcessParameters);
    
    Task OnPublicKeyValidationIsFinishedAsync(PublicKeyValidationParameters publicKeyValidationParameters);
    
    Task OnPublicKeyValidationFinished(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters, bool isValidated);
        
    Task OnPublicKeyValidationCanceled(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters);
}