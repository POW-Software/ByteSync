# ByteSync Client Security Architecture

**Last Updated:** November 6, 2025
**Analyzed Codebase:** ByteSync.Client (.NET 8.0)

---

This document provides a comprehensive technical overview of the security mechanisms implemented in **ByteSync.Client**. It covers the
cryptographic protocols, trust establishment procedures, session security, and data protection strategies used to ensure end-to-end
encryption and secure peer-to-peer synchronization.

---

## Overview

ByteSync.Client implements a multi-layered security architecture designed to protect data both in transit and at rest:

- **Asymmetric Cryptography (RSA)**: Used for identity establishment, trust validation, and secure key exchange
- **Symmetric Cryptography (AES-256)**: Used for high-performance encryption of session communications and file transfers
- **Digital Signatures (RSA-SHA256)**: Used for authentication and non-repudiation
- **Local Data Encryption**: Protects sensitive data stored on disk using machine-specific encryption

The security model follows a **Trust-on-First-Use (TOFU)** approach with out-of-band verification, ensuring that clients can establish
secure connections without requiring a centralized certificate authority.

---

## RSA Key Pair and Client Identity

### Key Generation

Each ByteSync client generates a unique RSA key pair on first launch:

```csharp
// ApplicationSettings.cs
public void InitializeRsa()
{
    var rsa = RSA.Create();  // Default: 2048-bit key
    
    DecodedRsaPrivateKey = rsa.ExportRSAPrivateKey();
    DecodedRsaPublicKey = rsa.ExportRSAPublicKey();
    
    InitializeClientId();
}
```

**Key Properties:**

- **Key Size**: 2048 bits (default in .NET 8.0)
- **Algorithm**: RSA with PKCS#1 padding
- **Storage**: Encrypted with machine-specific `EncryptionPassword`

### Client Identifier

The `ClientId` is cryptographically derived from the RSA public key:

```
ClientId = Base26(SHA256(InstallationId + SHA256(PublicKeyRSA)))
Format: XXXX-XXX-XXX (e.g., "ABCD-EFG-HIJ")
```

This creates a stable, human-readable identifier that is unique to each client's cryptographic identity.

### Local Key Storage

RSA keys are stored encrypted in `ApplicationSettings.xml`:

1. **Private Key**:
    - Encrypted with `EncryptionPassword` (machine-specific)
    - Stored as `RsaPrivateKey` property
    - Loaded into memory only when needed

2. **Public Key**:
    - Also encrypted with `EncryptionPassword`
    - Stored as `RsaPublicKey` property
    - Shared with other clients during trust establishment

### Encryption Password Derivation

The `EncryptionPassword` is derived from machine-specific properties:

```
EncryptionPassword = Caesar(StaticSeed + MachineSpecificData, 2020)
```

**MachineSpecificData Sources (in priority order):**

1. Environment variable `BYTESYNC_SYMMETRIC_KEY` (GUID, created on first run)
2. Windows: Registry `ProductId` from `HKLM\Software\Microsoft\Windows NT\CurrentVersion`
3. Linux: Machine ID from `/var/lib/dbus/machine-id` or `/etc/machine-id`
4. macOS: Hardware UUID from `ioreg`
5. Fallback: Static seed (if all above fail)

**Properties:**

- Keys are protected by machine-specific encryption
- Copying `ApplicationSettings.xml` to another machine renders keys unusable
- Renaming the machine does not invalidate keys

---

## Trusted Client System

### Trust Model

ByteSync uses a **decentralized trust model** where:

- No central authority issues certificates
- Each client maintains its own list of trusted public keys
- Trust is established through mutual validation with out-of-band verification

### TrustedPublicKey Structure

```csharp
public class TrustedPublicKey
{
    public string ClientId { get; set; }
    public byte[] PublicKey { get; set; }
    public string PublicKeyHash { get; set; }  // Formatted for display
    public string SafetyKey { get; set; }      // MD5 for OOB verification
    public DateTimeOffset ValidationDate { get; set; }
}
```

### Trust Establishment Process

When client A wants to join a session with client B, the following protocol is executed:

#### 1. Public Key Exchange

Both clients exchange `PublicKeyCheckData` via the server:

```csharp
public class PublicKeyCheckData
{
    public PublicKeyInfo IssuerPublicKeyInfo { get; set; }
    public string IssuerClientInstanceId { get; set; }
    public PublicKeyInfo? OtherPartyPublicKeyInfo { get; set; }
    public string Salt { get; set; }  // 12 random letters (~52^12 combinations)
    public bool? IsTrustedByOtherParty { get; set; }
}
```

The `Salt` is a randomly generated 12-character string providing ~52^12 (390 trillion) possible combinations, preventing pre-computation
attacks.

#### 2. SafetyKey Computation

Both parties compute an identical `SafetyKey` for out-of-band verification:

```
SHA256_Keys = [SHA256(PublicKeyA), SHA256(PublicKeyB)].Sort()
Precomputed = SHA256_Keys[0] + "_" + SHA256_Keys[1] + "_" + Salt
SafetyKey = MD5(Precomputed)
```

The keys are sorted to ensure both parties compute the same value regardless of initiator/responder role. The SafetyKey is used for
human-readable out-of-band verification and displayed in the UI.

#### 3. Out-of-Band Verification

Users compare the `SafetyKey` through an independent channel (phone call, video conference, etc.):

```
Client A displays: "AB3F 9C2D E847 1A05"
Client B displays: "AB3F 9C2D E847 1A05"

If matching → Users click "Validate"
If different → Man-in-the-Middle attack detected → Abort
```

This protects against a malicious server injecting rogue public keys.

#### 4. Trust Storage

After successful validation, the trusted public key is stored:

```csharp
public void Trust(TrustedPublicKey trustedPublicKey)
{
    _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings => 
        settings.AddTrustedKey(trustedPublicKey));
}
```

Trusted keys are:

- Serialized to JSON
- Encrypted with `EncryptionPassword`
- Stored in `ApplicationSettings.xml`
- Indexed by `ClientId` (allowing key rotation if needed)

### Trust Verification

Before any cryptographic operation with a remote client:

```csharp
public bool IsTrusted(PublicKeyInfo publicKeyInfo)
{
    var trustedKey = applicationSettings.DecodedTrustedPublicKeys
        .Where(tk => Equals(tk.ClientId, publicKeyInfo.ClientId))
        .MaxBy(tk => tk.ValidationDate);  // Most recent if multiple
    
    return trustedKey?.PublicKey.SequenceEqual(publicKeyInfo.PublicKey) ?? false;
}
```

This ensures that:

- The `ClientId` matches a known trusted client
- The public key is byte-for-byte identical to the stored trusted key
- Key rotation is supported (most recent key wins)

### Trust Revocation

To revoke trust for a compromised client:

```csharp
_publicKeysManager.Delete(trustedPublicKey);
```

This removes the entry from the trusted keys list. The revoked client:

- Cannot join new sessions with this client
- Must go through trust establishment again
- Existing sessions are unaffected (session keys already exchanged)

Revocation is local to the client that performs the deletion.

---

## Session Security

### Session Join Protocol

When a client joins an existing session, the following security protocol is executed:

#### Phase 1: Trust Check (30s timeout)

```
[JOINER]                        [SERVER]                      [MEMBERS]
   |                                |                              |
   |--- StartTrustCheck ---------->|                              |
   |    (MyPublicKeyInfo)           |                              |
   |                                |--- AskPublicKeyCheckData -->|
   |                                |                              |
   |                                |<-- PublicKeyCheckData ------|
   |<-- PublicKeyCheckData ---------|    (IsTrustedByOtherParty)  |
   |                                |                              |
```

**Behavior on Timeout:**

- If any member fails to respond within 30 seconds, the join fails completely
- Status: `JoinSessionStatus.TrustCheckFailed`
- This ensures all members are aware of and consent to the new participant

**Trust Decision Matrix:**

| Joiner Trusts Member | Member Trusts Joiner | Result                               |
|----------------------|----------------------|--------------------------------------|
| Yes                  | Yes                  | Auto-trust (skip UI)                 |
| Yes                  | No                   | UI validation required (member side) |
| No                   | Yes                  | UI validation required (joiner side) |
| No                   | No                   | UI validation required (both sides)  |

For non-mutually-trusted clients, the UI displays the `SafetyKey` for out-of-band verification.

#### Phase 2: Password Exchange (RSA-encrypted)

Once trust is established, the session password is exchanged:

```
[JOINER]                        [SERVER]                      [VALIDATOR]
   |                                |                              |
   |--- AskPasswordExchangeKey --->|                              |
   |    (MyPublicKeyInfo)           |--- GivePasswordExchangeKey ->|
   |                                |    (JoinerPublicKeyInfo)     |
   |                                |                              |
   |                                |       [Validates IsTrusted]  |
   |                                |                              |
   |                                |<-- EncryptedPassword --------|
   |<-- EncryptedPassword ----------|    RSA(SessionId + JoinerId + Password)
   |                                |                              |
   |    [Decrypts with RSA private] |                              |
   |                                |                              |
   |--- AskJoinCloudSession ------->|                              |
   |    RSA-Encrypted(ExchangePassword)                           |
   |                                |                              |
   |                                |       [Validates password]   |
   |                                |       [Encrypts AES key]     |
   |                                |                              |
   |<-- EncryptedAesKey ------------|                              |
   |    RSA(AES-256 Session Key)    |                              |
```

**ExchangePassword Format:**

```
Data = SessionId + "___" + JoinerId + "___" + SessionPassword
Encrypted = RSA.Encrypt(Data, JoinerPublicKey, PKCS1Padding)
```

This format includes:

- SessionId for session binding
- JoinerId for client identification
- SessionPassword for access control

**Properties:**

- Session password is transmitted encrypted
- Only trusted clients can decrypt the password
- Validator verifies password before providing AES key
- Password is protected by RSA encryption

#### Phase 3: AES-256 Key Distribution

After password validation, the validator encrypts the session's AES-256 key:

```csharp
var encryptedAesKey = _publicKeysManager.EncryptBytes(
    joinerPublicKeyInfo,
    _cloudSessionConnectionRepository.GetAesEncryptionKey());
```

The joiner decrypts with its RSA private key:

```csharp
var aesEncryptionKey = _publicKeysManager.DecryptBytes(encryptedAesKey);
_cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);
```

**Key Properties:**

- **Key Size**: 256 bits (AES-256)
- **Mode**: CBC (Cipher Block Chaining)
- **Padding**: PKCS7
- **IV**: Randomly generated per encrypted object/file

**Characteristics:**

- Server does not have access to the AES key
- New AES key generated per session
- Each session uses a unique random AES-256 key

#### Phase 4: Digital Signatures (Authentication)

To prevent impersonation attacks, all members exchange digital signatures:

```
Signature = SHA256("USERAUTH_REQUEST_" + SessionId + "_" + 
                   IssuerClientInstanceId + "_" + 
                   SHA512(IssuerPublicKey) + "_" + 
                   RecipientClientInstanceId)

DigitalSignature = RSA.SignData(Signature, PrivateKey, SHA256, PKCS1Padding)
```

Each member:

1. Computes expected signature for every other member
2. Verifies received signatures with trusted RSA public keys
3. Confirms match → Member is authenticated

**Verification:**

```csharp
var expectedSignature = ComputeOtherPartyExpectedSignature(sessionId, issuerId, issuerPublicKey);
var isValid = RSA.VerifyData(receivedSignature, expectedSignature, issuerPublicKey);
```

This protocol is inspired by **SSH User Authentication (RFC 4252)** and includes:

- Authentication through private key possession
- Digital signature verification
- Session-specific and member-specific binding
- Private key requirement for signature creation

**Cross-Check Protocol:**

- Joiner sends signatures to all members (`NeedsCrossCheck = true`)
- Members verify and respond with their own signatures
- Joiner verifies member signatures
- Server marks authentication complete only after bidirectional verification

If any signature fails verification, the join process is aborted with status `AuthIsNotChecked`.

---

## Data Encryption

### Session Data Encryption

All session-related data structures are encrypted with AES-256:

```csharp
public T Encrypt<T>(object data) where T : IEncryptedSessionData, new()
{
    var aes = Aes.Create();
    aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey();
    aes.GenerateIV();  // Random IV per object
    
    var json = JsonHelper.Serialize(data);
    
    // Encrypt with AES-CBC
    using var encryptor = aes.CreateEncryptor();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    using (var sw = new StreamWriter(cs))
    {
        sw.Write(json);
    }
    
    return new T 
    { 
        Id = Guid.NewGuid().ToString(),
        IV = aes.IV,
        Data = ms.ToArray() 
    };
}
```

**Encrypted Data Types:**

- `SessionMemberPrivateData`: Machine name, local paths
- `DataSource`: Source directory information
- `DataNode`: File tree structures

**Characteristics:**

- Each encrypted object has a unique random IV
- Server stores encrypted blobs without decryption capability
- Only session members with AES key can decrypt

### File Transfer Encryption

Files are encrypted during transfer using a streaming approach:

```csharp
public class SlicerEncrypter
{
    private void EndInitialize()
    {
        Aes = Aes.Create();
        Aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey();
        Aes.IV = SharedFileDefinition.IV;  // Unique per file
    }
    
    public async Task<FileUploaderSlice?> SliceAndEncrypt()
    {
        var memoryStream = new MemoryStream();
        var encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV);
        var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        
        // Read file in chunks, encrypt on-the-fly
        await cryptoStream.WriteAsync(bytes, CancellationToken.None);
        
        return new FileUploaderSlice(sliceNumber, memoryStream);
    }
}
```

**File Encryption Properties:**

- **Chunk Size**: Adaptive, ranges from 64 KB to 16 MB based on upload performance
    - Initial size: 500 KB
    - Upscales (up to 2x) if upload time < 22 seconds
    - Downscales (0.75x) if upload time > 30 seconds
    - Adjusts parallelism (2-4 concurrent uploads) based on chunk size
- **IV**: Unique per file (stored in `SharedFileDefinition`)
- **Streaming**: Files are encrypted on-the-fly (no need to load entire file in memory)
- **Incremental**: Each slice is part of the same encrypted stream

**Decryption** (receiver side):

```csharp
public class MergerDecrypter
{
    private void Initialize(string finalFile, DownloadTarget downloadTarget)
    {
        Aes = Aes.Create();
        Aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey();
        Aes.IV = SharedFileDefinition.IV;  // Same IV as encryption
    }
    
    public async Task MergeAndDecrypt(byte[] encryptedSlice)
    {
        var decryptor = Aes.CreateDecryptor(Aes.Key, Aes.IV);
        using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Write);
        
        await cryptoStream.WriteAsync(encryptedSlice);
    }
}
```

**Characteristics:**

- Server cannot read file contents
- Only session members can decrypt
- CBC mode detects tampering through decryption failure
- Streaming avoids memory exhaustion on large files

---

## Security Features

ByteSync.Client implements the following security mechanisms:

1. **Cryptographic Algorithms**:
    - RSA 2048-bit keys for asymmetric operations
    - AES-256 encryption for symmetric operations
    - SHA-256/SHA-512 hashing for integrity verification

2. **Trust Management**:
    - Decentralized model without certificate authorities
    - Out-of-band verification mechanism (SafetyKey)
    - Each client maintains its own trust relationships

3. **Data Protection**:
    - Server acts as relay, cannot decrypt session data
    - Files are encrypted before upload, decrypted after download
    - Sensitive local data is encrypted at rest

4. **Authentication Layers**:
    - RSA key-based identity
    - Session password protection
   - Digital signatures
    - Cross-verification between peers

5. **Session Management**:
    - Unique AES-256 key per session
    - Session-specific digital signatures
   - 30-second timeout for trust check

---

## Technical Specifications

### Cryptographic Algorithms

| Purpose            | Algorithm  | Key/Hash Size | Mode/Padding |
|--------------------|------------|---------------|--------------|
| Identity Keys      | RSA        | 2048 bits     | PKCS#1 v1.5  |
| Encryption         | RSA        | 2048 bits     | PKCS#1 v1.5  |
| Signatures         | RSA-SHA256 | 2048 bits     | PKCS#1 v1.5  |
| Session Encryption | AES        | 256 bits      | CBC + PKCS7  |
| File Encryption    | AES        | 256 bits      | CBC + PKCS7  |
| Hashing            | SHA-256    | 256 bits      | -            |
| Hashing (Extended) | SHA-512    | 512 bits      | -            |
| Local Storage      | AES        | 256 bits      | CBC + PKCS7  |

### Key Lifecycle

| Key Type            | Generation           | Storage                                    | Rotation                  | Revocation               |
|---------------------|----------------------|--------------------------------------------|---------------------------|--------------------------|
| RSA Private         | On first launch      | Encrypted in `ApplicationSettings.xml`     | Manual (loses all trusts) | Manual (regenerate keys) |
| RSA Public          | Derived from private | Encrypted in `ApplicationSettings.xml`     | With private key          | With private key         |
| AES Session         | By session creator   | In memory, optionally in encrypted profile | Per session               | Session end              |
| Encryption Password | Derived from machine | Not stored (recomputed)                    | Never                     | N/A                      |

### Network Protocol

1. **Transport Layer**: HTTPS (TLS 1.2+)
2. **Application Layer**: SignalR over WebSockets
3. **Server Role**: Relay and orchestration (cannot decrypt)
4. **Peer Authentication**: RSA signatures (not TLS certificates)

### Storage Security

| Data Type                 | Encryption         | Key                                             |
|---------------------------|--------------------|-------------------------------------------------|
| RSA Keys                  | AES-256-CBC        | `EncryptionPassword` (machine-specific)         |
| Trusted Keys List         | AES-256-CBC        | `EncryptionPassword`                            |
| Session Profile (AES Key) | Double AES-256-CBC | `EncryptionPassword` + `ProfileDetailsPassword` |
| Email/Serial              | AES-256-CBC        | `EncryptionPassword`                            |

---

## References

### Standards and Recommendations

- **NIST SP 800-57 Part 1**: Recommendation for Key Management
- **NIST SP 800-131A**: Transitioning the Use of Cryptographic Algorithms and Key Lengths
- **RFC 8017**: PKCS #1: RSA Cryptography Specifications Version 2.2
- **RFC 4252**: The Secure Shell (SSH) Authentication Protocol (inspiration for digital signatures)
- **FIPS 197**: Advanced Encryption Standard (AES)

### Microsoft Documentation

- [RSA Class (.NET)](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsa)
- [Aes Class (.NET)](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes)
- [Cryptographic Security in .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/cryptographic-services)

---

## Summary

ByteSync.Client implements a multi-layered security architecture for peer-to-peer file synchronization. The system combines RSA-based trust
establishment, AES-256 session encryption, and digital signature authentication.

The decentralized trust model uses manual out-of-band verification to establish trust between clients without relying on certificate
authorities.

For questions, please open an issue on the [ByteSync GitHub repository](https://github.com/POW-Software/ByteSync).

