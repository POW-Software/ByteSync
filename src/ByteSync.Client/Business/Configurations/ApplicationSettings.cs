using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ByteSync.Business.Communications;
using ByteSync.Common.Controls.Json;
using ByteSync.Services.Converters.BaseConverters;

namespace ByteSync.Business.Configurations;

public class ApplicationSettings : ICloneable
{
    private string? _rsaPrivateKey;

    public ApplicationSettings()
    {
        RsaPrivateKey = null;
        RsaPublicKey = null;
        Email = null;
        Serial = null;
        CultureCode = null;
        ZoomLevel = 100;
        Theme = null;
        AgreesBetaWarning0 = false;
        TrustedPublicKeys = null;
        SettingsVersion = null;
        AcknowledgedAnnouncementIds = null;
        UserRatingOptOut = false;
        UserRatingLastPromptedOn = null;
    }

    public string InstallationId { get; set; } = null!;
        
    public string ClientId { get; set; } = null!;

    public string? RsaPrivateKey
    {
        get { return _rsaPrivateKey; }
        set
        {
            _rsaPrivateKey = value;
            if (_rsaPrivateKey != null && EncryptionPassword.IsNotEmpty())
            {
                var bytes = CryptographyUtils.DecryptBytes(_rsaPrivateKey!, EncryptionPassword);
                    
                PrivateRsa = RSA.Create();
                PrivateRsa.ImportRSAPrivateKey(bytes, out _);
            }
        }
    }

    [XmlIgnore]
    public byte[]? DecodedRsaPrivateKey
    {
        get
        {
            if (RsaPrivateKey.IsNullOrEmpty())
            {
                return null;
            }
                
            return CryptographyUtils.DecryptBytes(RsaPrivateKey!, EncryptionPassword);
        }
        set
        {
            if (value == null)
            {
                RsaPrivateKey = null;
            }
            else
            {
                RsaPrivateKey = CryptographyUtils.EncryptBytes(value, EncryptionPassword);

                // PrivateRsa = RSA.Create();
                // PrivateRsa.ImportRSAPrivateKey(value, out _);
            }
        }
    }

    public string? RsaPublicKey { get; set; }
        
    [XmlIgnore]
    public byte[]? DecodedRsaPublicKey
    {
        get
        {
            if (RsaPublicKey.IsNullOrEmpty())
            {
                return null;
            }
                
            return CryptographyUtils.DecryptBytes(RsaPublicKey!, EncryptionPassword);
        }
        set
        {
            if (value == null)
            {
                RsaPublicKey = null;
            }
            else
            {
                RsaPublicKey = CryptographyUtils.EncryptBytes(value, EncryptionPassword);
            }
        }
    }

    public string? Email { get; set; }
        
    [XmlIgnore]
    public string? DecodedEmail
    {
        get
        {
            if (Email.IsNullOrEmpty())
            {
                return null;
            }
            else
            {
                return CryptographyUtils.Decrypt(Email!, EncryptionPassword);
            }
        }
        set
        {
            if (value.IsNullOrEmpty())
            {
                Email = null;
            }
            else
            {
                Email = CryptographyUtils.Encrypt(value!, EncryptionPassword);
            }
        }
    }

    public string? Serial { get; set; }
        
    [XmlIgnore]
    public string? DecodedSerial
    {
        get
        {
            if (Serial.IsNullOrEmpty())
            {
                return null;
            }
                
            return CryptographyUtils.Decrypt(Serial!, EncryptionPassword);
        }
        set
        {
            if (value.IsNullOrEmpty())
            {
                Serial = null;
            }
            else
            {
                Serial = CryptographyUtils.Encrypt(value!, EncryptionPassword);
            }
        }
    }

    public string? CultureCode { get; set; }
        
    public int ZoomLevel { get; set; }
        
    public string? Theme { get; set; }
        
    public bool AgreesBetaWarning0 { get; set; }
        
    public string? TrustedPublicKeys { get; set; }
        
    [XmlIgnore]
    public ReadOnlyCollection<TrustedPublicKey>? DecodedTrustedPublicKeys
    {
        get
        {
            if (TrustedPublicKeys.IsNullOrEmpty())
            {
                return null;
            }
                
            string json = CryptographyUtils.Decrypt(TrustedPublicKeys!, EncryptionPassword);

            var decodedTrustedKeys = JsonHelper.Deserialize<List<TrustedPublicKey>>(json);

            return decodedTrustedKeys.AsReadOnly();
        }
    }

    public string? SettingsVersion { get; set; }
    
    public string? AcknowledgedAnnouncementIds { get; set; }
    
    public bool UserRatingOptOut { get; set; }

    public DateTimeOffset? UserRatingLastPromptedOn { get; set; }
        
    private string EncryptionPassword { get; set; } = null!;

    [XmlIgnore]
    public RSA PrivateRsa { get; private set; } = null!;

    [XmlIgnore]
    public List<string> DecodedAcknowledgedAnnouncementIds
    {
        get
        {
            if (AcknowledgedAnnouncementIds.IsNullOrEmpty())
            {
                return new List<string>();
            }
                
            return AcknowledgedAnnouncementIds!.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        set
        {
            if (value == null || value.Count == 0)
            {
                AcknowledgedAnnouncementIds = null;
            }
            else
            {
                AcknowledgedAnnouncementIds = string.Join(";", value);
            }
        }
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }

    public void SetEncryptionPassword(string encryptionPassword)
    {
        EncryptionPassword = encryptionPassword;
            
        if (RsaPrivateKey != null && EncryptionPassword.IsNotEmpty())
        {
            var bytes = CryptographyUtils.DecryptBytes(RsaPrivateKey!, EncryptionPassword);
                    
            PrivateRsa = RSA.Create();
            PrivateRsa.ImportRSAPrivateKey(bytes, out _);
        }
    }

    public void InitializeTrustedPublicKeys()
    {
        string json = JsonHelper.Serialize(new List<TrustedPublicKey>());
        TrustedPublicKeys = CryptographyUtils.Encrypt(json, EncryptionPassword);
    }

    public void AddTrustedKey(TrustedPublicKey trustedPublicKey)
    {
        var trustedKeys = DecodedTrustedPublicKeys!.ToList();

        trustedKeys.RemoveAll(tk => tk.ClientId.Equals(trustedPublicKey.ClientId));
            
        trustedKeys.Add(trustedPublicKey);
            
        string json = JsonHelper.Serialize(trustedKeys);
        TrustedPublicKeys = CryptographyUtils.Encrypt(json, EncryptionPassword);
    }

    public void RemoveTrustedKey(TrustedPublicKey trustedPublicKey)
    {
        var trustedKeys = DecodedTrustedPublicKeys!.ToList();

        trustedKeys.RemoveAll(tk => tk.ClientId.Equals(trustedPublicKey.ClientId));

        string json = JsonHelper.Serialize(trustedKeys);
        TrustedPublicKeys = CryptographyUtils.Encrypt(json, EncryptionPassword);
    }

    public void InitializeAcknowledgedAnnouncementIds()
    {
        AcknowledgedAnnouncementIds = null;
    }

    public void AddAcknowledgedAnnouncementId(string announcementId)
    {
        var acknowledgedIds = DecodedAcknowledgedAnnouncementIds;
        
        if (!acknowledgedIds.Contains(announcementId))
        {
            acknowledgedIds.Add(announcementId);
            DecodedAcknowledgedAnnouncementIds = acknowledgedIds;
        }
    }

    public bool IsAnnouncementAcknowledged(string announcementId)
    {
        return DecodedAcknowledgedAnnouncementIds.Contains(announcementId);
    }

    public void InitializeRsa()
    {
        var rsa = RSA.Create();
            
        DecodedRsaPrivateKey = rsa.ExportRSAPrivateKey();
        DecodedRsaPublicKey = rsa.ExportRSAPublicKey();

        InitializeClientId();
    }

    private void InitializeClientId()
    {
        string machineIdentifierBase = CryptographyUtils
            .ComputeSHA256FromText(InstallationId + CryptographyUtils.ComputeSHA256(DecodedRsaPublicKey!));

        var value = NumericUtils.ConvertHexToDouble(machineIdentifierBase);
        
        // 26^10 possibilités : 141 167 095 653 376
        var base26Converter = new Base26Converter();
        var modulo = (long) (value % Math.Pow(base26Converter.FiguresCount, 10));
        var identifier = base26Converter.ConvertTo(modulo, 10).ToUpper();

        var regex = new Regex(
            @"^(....)(...)(...)$",
            RegexOptions.None,
            TimeSpan.FromMilliseconds(200) 
        );
        // https://stackoverflow.com/questions/3968845/format-string-with-dashes
        identifier = regex.Replace(identifier, "$1-$2-$3");

        ClientId = identifier;
    }
}
