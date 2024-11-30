using ByteSync.Business.Updates;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateProgressRepository
{
    public Progress<UpdateProgress> Progress { get; }
}