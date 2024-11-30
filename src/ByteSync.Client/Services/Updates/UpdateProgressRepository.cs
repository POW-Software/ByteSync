using ByteSync.Business.Updates;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class UpdateProgressRepository : IUpdateProgressRepository
{
    public UpdateProgressRepository()
    {
        Progress = new Progress<UpdateProgress>();
    }
    
    public Progress<UpdateProgress> Progress { get; }
}