// using ByteSync.Common.Business.EndPoints;
// using MediatR;
//
// namespace ByteSync.Commands.Sessions.Connecting;
//
// public class OnCloudSessionPasswordExchangeKeyAskedRequest : IRequest
// {
//     public OnCloudSessionPasswordExchangeKeyAskedRequest(string sessionId, PublicKeyInfo publicKeyInfo, string requesterInstanceId)
//     {
//         SessionId = sessionId;
//         PublicKeyInfo = publicKeyInfo;
//         RequesterInstanceId = requesterInstanceId;
//     }
//     
//     public string SessionId { get; set; }
//     
//     public PublicKeyInfo PublicKeyInfo { get; set; }
//     
//     public string RequesterInstanceId { get; set; }
// }