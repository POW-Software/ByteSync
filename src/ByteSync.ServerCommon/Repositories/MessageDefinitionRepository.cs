using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class MessageDefinitionRepository : BaseRepository<List<MessageDefinition>>, IMessageDefinitionRepository
{
    public const string UniqueKey = "All";

    public MessageDefinitionRepository(IRedisInfrastructureService redisInfrastructureService, ICacheRepository<List<MessageDefinition>> cacheRepository)
        : base(redisInfrastructureService, cacheRepository)
    {
    }

    public override EntityType EntityType => EntityType.MessageDefinition;

    public Task<List<MessageDefinition>?> GetAll()
    {
        return Get(UniqueKey);
    }

    public Task SaveAll(List<MessageDefinition> definitions)
    {
        return Save(UniqueKey, definitions);
    }
}
