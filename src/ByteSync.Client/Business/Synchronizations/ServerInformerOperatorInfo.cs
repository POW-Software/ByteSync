using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Business.Synchronizations;

public class ServerInformerOperatorInfo
{
    public ServerInformerOperatorInfo(ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        CloudActionCaller = cloudActionCaller;
        SynchronizationActionRequests = new List<SynchronizationActionRequest>();
        
        CreationDate = DateTime.Now;
    }

    public ISynchronizationActionServerInformer.CloudActionCaller CloudActionCaller { get; }
    
    public List<SynchronizationActionRequest> SynchronizationActionRequests { get; }
    
    public DateTime CreationDate
    {
        get;
    }

    public int ActionsCount
    {
        get
        {
            return SynchronizationActionRequests.Count;
        } 
    }

    public void Add(SynchronizationActionRequest synchronizationActionRequest)
    {
        SynchronizationActionRequests.Add(synchronizationActionRequest);
    }
}