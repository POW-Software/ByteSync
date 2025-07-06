using ByteSync.ServerCommon.Business.Messages;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Messages;

public class GetActiveMessagesRequest : IRequest<List<MessageDefinition>>;
