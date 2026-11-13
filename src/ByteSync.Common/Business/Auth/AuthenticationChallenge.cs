using System;

namespace ByteSync.Common.Business.Auth;

public class AuthenticationChallenge
{
    public string Id { get; set; } = "";
    public string Nonce { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}
