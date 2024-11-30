namespace ByteSync.Common.Business.Auth;

public class RefreshTokensResponse
{
    public RefreshTokensResponse()
    {

    }
    
    public RefreshTokensResponse(RefreshTokensStatus refreshTokensStatus)
    {
        RefreshTokensStatus = refreshTokensStatus;
    }
    
    public RefreshTokensResponse(RefreshTokensStatus refreshTokensStatus, AuthenticationTokens authenticationTokens)
    {
        RefreshTokensStatus = refreshTokensStatus;
        AuthenticationTokens = authenticationTokens;
    }
    
    public RefreshTokensStatus RefreshTokensStatus { get; set; }

    public AuthenticationTokens? AuthenticationTokens { get; set; }
    
    public bool IsSuccess
    {
        get => RefreshTokensStatus == RefreshTokensStatus.RefreshTokenOk;
    }
}