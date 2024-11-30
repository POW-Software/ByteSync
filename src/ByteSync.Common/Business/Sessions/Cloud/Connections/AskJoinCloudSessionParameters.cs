namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class AskJoinCloudSessionParameters
{
    public AskJoinCloudSessionParameters()
    {
            
    }

    public AskJoinCloudSessionParameters(GiveCloudSessionPasswordExchangeKeyParameters giveParameters, byte[] encryptedPassword)
    {
        SessionId = giveParameters.SessionId;
        JoinerClientInstanceId = giveParameters.JoinerInstanceId;
        ValidatorInstanceId = giveParameters.ValidatorInstanceId;
        EncryptedPassword = encryptedPassword;
    }

    public string SessionId { get; set; }
        
    public string JoinerClientInstanceId { get; set; }
        
    public string ValidatorInstanceId { get; set; }
        
    public byte[] EncryptedPassword { get; set; }
}