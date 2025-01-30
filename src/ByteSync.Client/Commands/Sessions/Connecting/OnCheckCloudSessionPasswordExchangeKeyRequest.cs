// using ByteSync.Common.Business.Sessions.Cloud.Connections;
// using MediatR;
//
// namespace ByteSync.Commands.Sessions.Connecting;
//
// public class OnCheckCloudSessionPasswordExchangeKeyRequest : IRequest
// {
//     public OnCheckCloudSessionPasswordExchangeKeyRequest(string sessionId, string joinerClientInstanceId, 
//         string validatorInstanceId, byte[] encryptedPassword)
//     {
//         SessionId = sessionId;
//         JoinerClientInstanceId = joinerClientInstanceId;
//         ValidatorInstanceId = validatorInstanceId;
//         EncryptedPassword = encryptedPassword;
//     }
//
//     public string SessionId { get; set; }
//
//     public string JoinerClientInstanceId { get; set; }
//
//     public string ValidatorInstanceId { get; set; }
//
//     public byte[] EncryptedPassword { get; set; }
//
//     public AskJoinCloudSessionParameters ToAskJoinCloudSessionParameters()
//     {
//         return new AskJoinCloudSessionParameters
//         {
//             SessionId = SessionId,
//             JoinerClientInstanceId = JoinerClientInstanceId,
//             ValidatorInstanceId = ValidatorInstanceId,
//             EncryptedPassword = EncryptedPassword
//         };
//     }
// }