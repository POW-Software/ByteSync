using ByteSync.ServerCommon.Business.Messages;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IMessageDefinitionRepository : IRepository<List<MessageDefinition>>
{
    Task<List<MessageDefinition>?> GetAll();

    Task SaveAll(List<MessageDefinition> definitions);
}
