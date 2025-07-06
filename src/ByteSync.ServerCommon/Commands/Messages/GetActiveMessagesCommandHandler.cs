using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Messages;

public class GetActiveMessagesCommandHandler : IRequestHandler<GetActiveMessagesRequest, List<MessageDefinition>>
{
    private readonly IMessageDefinitionRepository _repository;

    public GetActiveMessagesCommandHandler(IMessageDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MessageDefinition>> Handle(GetActiveMessagesRequest request, CancellationToken cancellationToken)
    {
        var allMessages = await _repository.GetAll();
        if (allMessages is null)
        {
            return new List<MessageDefinition>();
        }

        var now = DateTime.UtcNow;
        return allMessages.Where(m => m.StartDate <= now && now < m.EndDate).ToList();
    }
}
