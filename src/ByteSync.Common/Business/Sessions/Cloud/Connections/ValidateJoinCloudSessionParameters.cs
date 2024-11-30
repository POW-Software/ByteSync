namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class ValidateJoinCloudSessionParameters
{
    public ValidateJoinCloudSessionParameters()
    {
            
    }
        
    public ValidateJoinCloudSessionParameters(string sessionId, string joinerClientInstanceId, string validatorInstanceId, byte[] encryptedAesKey)
    {
        SessionId = sessionId;
        JoinerClientInstanceId = joinerClientInstanceId;
        ValidatorInstanceId = validatorInstanceId;
        EncryptedAesKey = encryptedAesKey;
    }

    public ValidateJoinCloudSessionParameters(AskJoinCloudSessionParameters askParameters, byte[] encryptedAesKey)
    {
        SessionId = askParameters.SessionId;
        JoinerClientInstanceId = askParameters.JoinerClientInstanceId;
        ValidatorInstanceId = askParameters.ValidatorInstanceId;
        EncryptedAesKey = encryptedAesKey;
    }

    public string SessionId { get; set; }
        
    public string JoinerClientInstanceId { get; set; }
        
    public string ValidatorInstanceId { get; set; }
        
    public byte[] EncryptedAesKey { get; set; }
        
    public string FinalizationPassword { get; set; }
}