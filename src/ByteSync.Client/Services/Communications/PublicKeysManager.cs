using System.Threading;
using ByteSync.Common.Business.Auth;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Services.Communications;

public class PublicKeysManager : IPublicKeysManager
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IConnectionService _connectionService;
    private readonly ILogger<PublicKeysManager> _logger;
    private readonly IEnvironmentService _environment;
    private readonly IAuthenticationTokensRepository _tokens;
    private readonly Lazy<ISessionService> _session;
    private readonly ICloudSessionConnectionRepository _sessionConnection;
    private readonly SemaphoreSlim _renewalGate = new(1, 1);
    
    public PublicKeysManager(IApplicationSettingsRepository applicationSettingsManager, IConnectionService connectionService,
        ILogger<PublicKeysManager> logger, IEnvironmentService environment,
        IAuthenticationTokensRepository tokens, Lazy<ISessionService> session, ICloudSessionConnectionRepository sessionConnection)
    {
        _applicationSettingsRepository = applicationSettingsManager;
        _connectionService = connectionService;
        _logger = logger;
        _environment = environment;
        _tokens = tokens;
        _session = session;
        _sessionConnection = sessionConnection;
    }
    
    public PublicKeyInfo GetMyPublicKeyInfo()
    {
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        
        var myPublicKeyInfo = new PublicKeyInfo
        {
            ClientId = applicationSettings.ClientId,
            PublicKey = applicationSettings.DecodedRsaPublicKey!,
            ProtocolVersion = ProtocolVersion.CURRENT,
        };
        
        return myPublicKeyInfo;
    }
    
    public PublicKeyCheckData BuildJoinerPublicKeyCheckData(PublicKeyCheckData memberPublicKeyCheckData)
    {
        var joinerPublicKeyCheckData =
            BuildPublicKeyCheckData(memberPublicKeyCheckData.IssuerPublicKeyInfo, null, memberPublicKeyCheckData.Salt);
        
        return joinerPublicKeyCheckData;
    }
    
    public PublicKeyCheckData BuildMemberPublicKeyCheckData(PublicKeyInfo joinerPublicKeyInfo, bool isTrustedByMember)
    {
        var joinerPublicKeyCheckData =
            BuildPublicKeyCheckData(joinerPublicKeyInfo, isTrustedByMember, null);
        
        return joinerPublicKeyCheckData;
    }
    
    private PublicKeyCheckData BuildPublicKeyCheckData(PublicKeyInfo? otherPartyPublicKeyInfo, bool? checkResponse, string? salt)
    {
        var publicKeyCheckData = new PublicKeyCheckData();
        
        publicKeyCheckData.IssuerPublicKeyInfo = GetMyPublicKeyInfo();
        publicKeyCheckData.IssuerClientInstanceId = _connectionService.ClientInstanceId!;
        
        publicKeyCheckData.OtherPartyPublicKeyInfo = otherPartyPublicKeyInfo;
        
        var publicKeyFormatter = new PublicKeyFormatter();
        publicKeyCheckData.IssuerPublicKeyHash = publicKeyFormatter.Format(publicKeyCheckData);
        
        if (salt != null)
        {
            publicKeyCheckData.Salt = salt;
        }
        else
        {
            // https://stackoverflow.com/questions/184112/what-is-the-optimal-length-for-user-password-salt
            // 64 bits :
            //  - 18446744073709551616
            // ici, 26 + 26 lettres sur 12 chars => 52^12 :
            //  - 390877006486250192896
            // C'est supérieur à une recommandation "pseudo standard"
            publicKeyCheckData.Salt = RandomUtils.GetRandomLetters(12, null);
        }
        
        publicKeyCheckData.OtherPartyCheckResponse = checkResponse;
        
        publicKeyCheckData.ProtocolVersion = ProtocolVersion.CURRENT;
        
        return publicKeyCheckData;
    }
    
    public bool IsTrusted(PublicKeyCheckData publicKeyCheckData)
    {
        return IsTrusted(publicKeyCheckData.IssuerPublicKeyInfo);
    }
    
    public bool IsTrusted(PublicKeyInfo publicKeyInfo)
    {
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        
        var trustedKey = applicationSettings.DecodedTrustedPublicKeys!
            .Where(tk => Equals(tk.ClientId, publicKeyInfo.ClientId))
            .MaxBy(tk => tk.ValidationDate);
        
        var result = false;
        if (trustedKey != null)
        {
            result = trustedKey.PublicKey.SequenceEqual(publicKeyInfo.PublicKey);
        }
        
        if (!result)
        {
            _logger.LogWarning("Public Key {@publicKeyInfo} is not trusted", publicKeyInfo);
        }
        
        return result;
    }
    
    public ReadOnlyCollection<TrustedPublicKey>? GetTrustedPublicKeys()
    {
        return _applicationSettingsRepository.GetCurrentApplicationSettings().DecodedTrustedPublicKeys;
    }
    
    public TrustedPublicKey BuildTrustedPublicKey(PublicKeyCheckData publicKeyCheckData)
    {
        var trustedPublicKey = new TrustedPublicKey();
        trustedPublicKey.ClientId = publicKeyCheckData.IssuerPublicKeyInfo.ClientId;
        trustedPublicKey.PublicKey = publicKeyCheckData.IssuerPublicKeyInfo.PublicKey;
        trustedPublicKey.ValidationDate = DateTimeOffset.Now;
        
        var publicKeyFormatter = new PublicKeyFormatter();
        trustedPublicKey.PublicKeyHash = publicKeyFormatter.Format(trustedPublicKey.PublicKey);
        
        var sha256PublicKeys = new List<string>();
        sha256PublicKeys.Add(CryptographyUtils.ComputeSHA256(publicKeyCheckData.IssuerPublicKeyInfo.PublicKey));
        sha256PublicKeys.Add(CryptographyUtils.ComputeSHA256(GetMyPublicKeyInfo().PublicKey));
        sha256PublicKeys.Sort();
        
        var precomputed = sha256PublicKeys[0] + "_" + sha256PublicKeys[1] + "_" + publicKeyCheckData.Salt;
        
        var md5 = CryptographyUtils.ComputeMD5FromText(precomputed);
        
        trustedPublicKey.SafetyKey = md5;
        
        return trustedPublicKey;
    }
    
    public void Trust(TrustedPublicKey trustedPublicKey)
    {
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
            settings.AddTrustedKey(trustedPublicKey));
        
        _logger.LogInformation("Added Trusted Public Key {@publicKey}", trustedPublicKey);
    }
    
    public void Delete(TrustedPublicKey trustedPublicKey)
    {
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
            settings.RemoveTrustedKey(trustedPublicKey));
        
        _logger.LogInformation("Removed Trusted Public Key {@publicKey}", trustedPublicKey);
    }
    
    public async Task InitializeRsaAndTrustedPublicKeys()
    {
        await _renewalGate.WaitAsync();
        try
        {
            if (_session.Value.CurrentSession != null || _sessionConnection.CurrentConnectionStatus != SessionConnectionStatus.NoSession ||
                _connectionService.IsStarting || _connectionService.CurrentConnectionStatus == ByteSync.Business.Communications.ConnectionStatuses.Connecting)
                throw new InvalidOperationException("Leave the current session and wait for the connection to finish before renewing the key");
            var reconnect = _connectionService.CurrentConnectionStatus == ConnectionStatuses.Connected;
            await _connectionService.StopConnection();
            if (_session.Value.CurrentSession != null || _sessionConnection.CurrentConnectionStatus != SessionConnectionStatus.NoSession)
                throw new InvalidOperationException("A session started while stopping the connection; the key was not renewed");
            _connectionService.CurrentEndPoint = null;
            await _tokens.Clear();
            var updatedSettings = _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
            {
                settings.InitializeRsa();
                settings.InitializeTrustedPublicKeys();
                _environment.SetClientIdentity(settings.ClientId, AuthenticationProtocol.GetIdentity(settings.DecodedRsaPublicKey!));
            });
            _logger.LogInformation("Initialized Local RSA Keys and removed all Trusted Public Keys");
            _logger.LogInformation("ClientId is now {@clientId}", updatedSettings.ClientId);
            if (reconnect) _ = ReconnectAfterRenewal();
        }
        finally { _renewalGate.Release(); }
    }

    private async Task ReconnectAfterRenewal()
    {
        try { await _connectionService.StartConnectionAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "The key was renewed but reconnection failed"); }
    }

    public byte[] DecryptBytes(byte[] messageToDecrypt)
    {
        // On décrypte avec la clé privée
        var privateRsa = _applicationSettingsRepository.GetCurrentApplicationSettings()
            .PrivateRsa;
        
        var decryptedBytes = privateRsa.Decrypt(messageToDecrypt, RSAEncryptionPadding.Pkcs1);
        
        return decryptedBytes;
    }
    
    public string DecryptString(byte[] messageToDecrypt)
    {
        // On décrypte avec la clé privée
        var privateRsa = _applicationSettingsRepository.GetCurrentApplicationSettings()
            .PrivateRsa;
        
        var decryptedBytes = privateRsa.Decrypt(messageToDecrypt, RSAEncryptionPadding.Pkcs1);
        
        var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
        
        return decryptedMessage;
    }
    
    public byte[] EncryptString(PublicKeyInfo publicKeyInfo, string messageToEncrypt)
    {
        var bytes = Encoding.UTF8.GetBytes(messageToEncrypt);
        
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyInfo.PublicKey, out _);
        
        // On encrypte avec la clé de la tierce partie
        var result = rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
        
        return result;
    }
    
    public byte[] SignData(string dataToEncrypt)
    {
        var bytes = Encoding.UTF8.GetBytes(dataToEncrypt);
        
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(applicationSettings.DecodedRsaPrivateKey, out _);
        
        // On encrypte avec la clé de la tierce partie
        var result = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return result;
    }
    
    public bool VerifyData(PublicKeyInfo publicKeyInfo, byte[] dataToVerify, string signature)
    {
        // var bytes = Encoding.UTF8.GetBytes(dataToEncrypt);
        //
        // var applicationSettings = _applicationSettingsManager.GetCurrentApplicationSettings();
        
        var signatureBytes = Encoding.UTF8.GetBytes(signature);
        
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyInfo.PublicKey, out _);
        
        // bool result = rsa.VerifyData(dataToVerify, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var result = rsa.VerifyData(signatureBytes, dataToVerify, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return result;
    }
    
    public byte[] EncryptBytes(PublicKeyInfo publicKeyInfo, byte[] messageToEncrypt)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyInfo.PublicKey, out _);
        
        // On encrypte avec la clé de la tierce partie
        var result = rsa.Encrypt(messageToEncrypt, RSAEncryptionPadding.Pkcs1);
        
        return result;
    }
}