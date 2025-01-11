using ByteSync.Business.Updates;

namespace ByteSync.Interfaces.Repositories.Updates;

public interface IUpdateRepository
{
    public Progress<UpdateProgress> Progress { get; }
    
    UpdateData UpdateData { get; set; }
    
    void ReportProgress(UpdateProgress updateProgress);
}