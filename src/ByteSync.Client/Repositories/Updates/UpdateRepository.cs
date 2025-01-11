using ByteSync.Business.Updates;
using ByteSync.Interfaces.Repositories.Updates;

namespace ByteSync.Repositories.Updates;

public class UpdateRepository : IUpdateRepository
{
    public UpdateRepository()
    {
        Progress = new Progress<UpdateProgress>();
        UpdateData = new UpdateData();
    }
    
    public Progress<UpdateProgress> Progress { get; }
    
    public UpdateData UpdateData { get; set; }
    
    public void ReportProgress(UpdateProgress updateProgress)
    {
        ((IProgress<UpdateProgress>)Progress).Report(updateProgress);
    }
}