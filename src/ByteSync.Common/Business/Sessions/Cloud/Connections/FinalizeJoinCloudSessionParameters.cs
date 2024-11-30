namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class FinalizeJoinCloudSessionParameters
{
    public FinalizeJoinCloudSessionParameters()
    {
        
    }
    
    public FinalizeJoinCloudSessionParameters(ValidateJoinCloudSessionParameters askParameters,
        EncryptedSessionMemberPrivateData encryptedSessionMemberPrivateData)
    {
        SessionId = askParameters.SessionId;
        JoinerInstanceId = askParameters.JoinerClientInstanceId;
        ValidatorInstanceId = askParameters.ValidatorInstanceId;
        FinalizationPassword = askParameters.FinalizationPassword;
        EncryptedSessionMemberPrivateData = encryptedSessionMemberPrivateData;
    }
    
    public string SessionId { get; set; }
    public string JoinerInstanceId { get; set; }
    
    public string ValidatorInstanceId { get; set; }
    
    public string FinalizationPassword { get; set; }
    
    public EncryptedSessionMemberPrivateData EncryptedSessionMemberPrivateData { get; set; }
}