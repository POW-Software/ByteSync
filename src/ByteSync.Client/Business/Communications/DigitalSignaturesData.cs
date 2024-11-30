using System.Threading;

namespace ByteSync.Business.Communications;

public class DigitalSignaturesData
{
    public DigitalSignaturesData(string dataId)
    {
        DataId = dataId;
        
        DigitalSignatureCheckedClients = new List<string>();
        ExpectedDigitalSignaturesCheckedEvent = new ManualResetEvent(false);

        DigitalSignaturesExpectedClients = null;
    }

    public string DataId { get; }

    public HashSet<string>? DigitalSignaturesExpectedClients { get; set; }

    public List<string> DigitalSignatureCheckedClients { get; set; }
    
    public ManualResetEvent ExpectedDigitalSignaturesCheckedEvent { get; }
}