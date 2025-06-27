using System.IO;
using System.Security.Cryptography;
using ByteSync.Business.DataSources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Interfaces.Business;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Encryptions;

public class DataEncrypter : IDataEncrypter
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;

    public DataEncrypter(ICloudSessionConnectionRepository cloudSessionConnectionRepository)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
    }

    public EncryptedSessionSettings EncryptSessionSettings(SessionSettings sessionSettings)
    {
        return Encrypt<EncryptedSessionSettings>(sessionSettings);
    }

    public SessionSettings DecryptSessionSettings(EncryptedSessionSettings encryptedSessionSettings)
    {
        return Decrypt<SessionSettings>(encryptedSessionSettings);
    }

    public EncryptedDataSource EncryptDataSource(DataSource dataSource)
    {
        var encryptedDataSource = Encrypt<EncryptedDataSource>(dataSource);
        encryptedDataSource.Code = dataSource.Code;

        return encryptedDataSource;
    }

    public EncryptedDataNode EncryptDataNode(DataNode dataNode)
    {
        return Encrypt<EncryptedDataNode>(dataNode);
    }

    public DataSource DecryptDataSource(EncryptedDataSource encryptedDataSource)
    {
        var dataSource = Decrypt<DataSource>(encryptedDataSource);
        dataSource.Code = encryptedDataSource.Code;

        return dataSource;
    }

    public DataNode DecryptDataNode(EncryptedDataNode encryptedDataNode)
    {
        return Decrypt<DataNode>(encryptedDataNode);
    }
    
    public EncryptedSessionMemberPrivateData EncryptSessionMemberPrivateData(SessionMemberPrivateData sessionMemberPrivateData)
    {
        return Encrypt<EncryptedSessionMemberPrivateData>(sessionMemberPrivateData);
    }

    public SessionMemberPrivateData DecryptSessionMemberPrivateData(EncryptedSessionMemberPrivateData encryptedSessionMemberPrivateData)
    {
        return Decrypt<SessionMemberPrivateData>(encryptedSessionMemberPrivateData);
    }

    public T Encrypt<T>(object data) where T : IEncryptedSessionData, new()
    {
        var aes = Aes.Create();
        aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey()!;
        aes.GenerateIV();
        
        var json = JsonHelper.Serialize(data);
        
        using var ms = new MemoryStream();
        using var encryptor = aes.CreateEncryptor();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(json);
        }
        
        var encryptedData = new T();
        encryptedData.IV = aes.IV;
        encryptedData.Data = ms.ToArray();

        return encryptedData;
    }
    
    public T Decrypt<T>(IEncryptedSessionData encryptedSessionData)
    {
        var aes = Aes.Create();
        aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey()!;
        aes.IV = encryptedSessionData.IV;
        
        string json;
        using var ms = new MemoryStream(encryptedSessionData.Data);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using (var streamReader = new StreamReader(cs))
        {
            json = streamReader.ReadToEnd();
        }
        
        var decryptedData = JsonHelper.Deserialize<T>(json);

        return decryptedData;
    }
}