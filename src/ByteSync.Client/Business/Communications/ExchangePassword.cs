using ByteSync.Common.Helpers;

namespace ByteSync.Business.Communications;

public class ExchangePassword
{
    private const string SEPARATOR = "___";
        
    public ExchangePassword(string rawPassword)
    {
        var parts = rawPassword.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3)
        {
            throw new ArgumentOutOfRangeException(nameof(rawPassword),
                "rawPassword does not respect expected format");
        }
            
        SessionId = parts[0];
        JoinerId = parts[1];
        Password = parts[2];
    }

    public ExchangePassword(string sessionId, string joinerId, string password)
    {
        if (sessionId.IsEmpty(true))
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId),
                "sessionId can not be empty");
        }
            
        if (joinerId.IsEmpty(true))
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId),
                "sessionId can not be empty");
        }
            
        if (password.IsEmpty(true))
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId),
                "sessionId can not be empty");
        }
            
        SessionId = sessionId;
        JoinerId = joinerId;
        Password = password;
    }

    public string SessionId { get; }
        
    public string JoinerId { get; }

    public string Password { get; }

    public string Data
    {
        get
        {
            return  $"{SessionId}{SEPARATOR}{JoinerId}{SEPARATOR}{Password}";
        }
    }

    public bool IsMatch(string sessionId, string joinerId, string password)
    {
        return sessionId.IsNotEmpty(true) && joinerId.IsNotEmpty(true) && password.IsNotEmpty(true)
               && SessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase)
               && JoinerId.Equals(joinerId, StringComparison.InvariantCultureIgnoreCase)
               && Password.Equals(password, StringComparison.InvariantCultureIgnoreCase);
    }
}