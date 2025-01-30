// using ByteSync.Common.Business.Sessions.Cloud.Connections;
// using MediatR;
//
// namespace ByteSync.Commands.Sessions.Connecting;
//
// public class OnYouJoinedSessionRequest : IRequest
// {
//     public OnYouJoinedSessionRequest(CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters validateJoinCloudSessionParameters)
//     {
//         CloudSessionResult = cloudSessionResult;
//         Parameters = validateJoinCloudSessionParameters;
//     }
//     
//     public CloudSessionResult CloudSessionResult { get; }
//     
//     public ValidateJoinCloudSessionParameters Parameters { get; }
// }