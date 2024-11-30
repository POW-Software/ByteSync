// using System.Collections.Generic;
// using ByteSync.Common.Business.EndPoints;
//
// namespace ByteSync.Common.Business.Sessions.Cloud.Connections;
//
// public class GetCloudSessionMembersParameters
// {
//     public GetCloudSessionMembersParameters()
//     {
//
//     }
//         
//     public GetCloudSessionMembersParameters(string sessionId, PublicKeyInfo publicKeyInfo, StartTrustCheckModes startTrustCheckMode)
//     {
//         SessionId = sessionId;
//         PublicKeyInfo = publicKeyInfo;
//         StartTrustCheckMode = startTrustCheckMode;
//     }
//
//     public string SessionId { get; set; }
//         
//     public PublicKeyInfo PublicKeyInfo { get; set; }
//     
//     public StartTrustCheckModes StartTrustCheckMode { get; set; }
//     
//     public List<string>? MembersToCheck { get; set; }
// }