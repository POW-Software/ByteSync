using ByteSync.ServerCommon.Business.Messages;

namespace ByteSync.ServerCommon.Interfaces.Loaders;

public interface IMessageDefinitionsLoader
{
    Task<List<MessageDefinition>> Load();
}
