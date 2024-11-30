using ByteSync.Common.Business.Misc;
using ByteSync.ServerCommon.Constants;
using Newtonsoft.Json;

namespace ByteSync.ServerCommon.Entities;

public class ProductSerial : ICloneable
{
    public ProductSerial()
    {
    }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "serialNumber")]
    public string SerialNumber { get; set; }

    [JsonProperty(PropertyName = "email")]
    public string Email { get; set; }

    [JsonProperty(PropertyName = "productName")]
    public string ProductName { get; set; }

    [JsonProperty(PropertyName = "expirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonProperty(PropertyName = "priceId")]
    public string PriceId { get; set; }

    [JsonProperty(PropertyName = "lastValidityCheck")]
    public DateTime LastValidityCheck { get; set; }

    public bool IsFreeVersion
    {
        get
        {
            return ProductName == null || ProductName.ToLower().Contains("free") || Equals(PriceId, "1");
        }
    }
        
    public bool IsBetaVersion
    {
        get
        {
            return (ProductName ?? "").ToLower().Contains("beta") || Equals(PriceId, "8");
        }
    }
        
    public bool IsSlotLimited
    {
        get
        {
            return Slots != null;
        }
    }

    public bool IsExpired
    {
        get
        {
            // 24/06/2022: Modifié sinon, on était bloqué par l'expiration de la licence le dernier jour possible de son utilisation
            // (le jour où elle est renouvelée)
            return ExpirationDate != null && ExpirationDate.Value.Date.AddDays(1) < DateTime.Today;
        }
    }
        
    public int? Slots
    {
        get
        {
            // ReSharper disable once RedundantAssignment
            int? slots = null;
                
            switch (PriceId)
            {
                // Free
                case "1":
                    slots = 2;
                    break;
                    
                // Lite
                case "2": 
                case "5": 
                    slots = 5;
                    break;
                    
                // Professional
                case "3": 
                case "6": 
                    slots = 20;
                    break;
                    
                // Enterprise
                case "4": 
                case "7": 
                    // ReSharper disable once RedundantAssignment
                    slots = null;
                    break;
                    
                // Beta
                case "8":
                    slots = 5;
                    break;
                    
            #if DEBUG
                
                default:
                    throw new ApplicationException("Unkown PriceId: " + PriceId);
                    
            #endif
            }

            return slots;
        }
    }

    public string Subscription
    {
        get
        {
            // ReSharper disable once RedundantAssignment
            string subscription = null;

            if (Email.Equals(SerialConstants.EMAIL_HI_HN, StringComparison.InvariantCultureIgnoreCase))
            {
                return "HN";
            }
                
            switch (PriceId)
            {
                case "1":
                    subscription = "Free";
                    break;
                case "2":
                    subscription = "Lite - Monthly";
                    break;
                case "3":
                    subscription = "Professional - Monthly";
                    break;
                case "4":
                    subscription = "Enterprise - Monthly";
                    break;
                case "5":
                    subscription = "Lite - Yearly";
                    break;
                case "6":
                    subscription = "Professional - Yearly";
                    break;
                case "7":
                    subscription = "Enterprise - Yearly";
                    break;
                case "8":
                    subscription = "Open Beta";
                    break;
            #if DEBUG
                
                default:
                    throw new ApplicationException("Unkown PriceId: " + PriceId);
                    
            #endif
            }

            return subscription;
        }
    }
        
    public long AllowedCloudSynchronizationVolumeInBytes
    {
        get
        {
            // ReSharper disable once RedundantAssignment
            long? allowedCloudSynchronizationVolumeInBytes = null;

            if (Email.Equals(SerialConstants.EMAIL_HI_HN, StringComparison.InvariantCultureIgnoreCase))
            {
                return SizeConstants.ONE_TERA_BYTES;
            }
                
            switch (PriceId)
            {
                case "1": // Free
                    allowedCloudSynchronizationVolumeInBytes = (long)10 * SizeConstants.ONE_GIGA_BYTES;
                    break;
                case "2": // Lite - Monthly
                case "5": // Lite - Yearly
                    allowedCloudSynchronizationVolumeInBytes = SizeConstants.ONE_TERA_BYTES;
                    break;
                case "3": // Professional - Monthly
                case "6": // Professional - Yearly
                    allowedCloudSynchronizationVolumeInBytes = 10 * SizeConstants.ONE_TERA_BYTES;
                    break;
                case "4": // Enterprise - Monthly
                case "7": // Enterprise - Yearly
                    allowedCloudSynchronizationVolumeInBytes = 100 * SizeConstants.ONE_TERA_BYTES;
                    break;
                case "8": // Open Beta
                    allowedCloudSynchronizationVolumeInBytes = SizeConstants.ONE_TERA_BYTES;
                    break;
            #if DEBUG
                default:
                    throw new ApplicationException("Unkown PriceId: " + PriceId);
                    
            #endif
            }

            return allowedCloudSynchronizationVolumeInBytes.Value;
        }
    }

    protected bool Equals(ProductSerial other)
    {
        return SerialNumber == other.SerialNumber && Email == other.Email;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ProductSerial) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SerialNumber, Email);
    }

    public object Clone()
    {
        var clone = (ProductSerial) this.MemberwiseClone();
        //
        // clone.RefreshTokens = new List<RefreshToken>();
        // foreach (var refreshToken in this.RefreshTokens)
        // {
        //     clone.RefreshTokens.Add((RefreshToken) refreshToken.Clone());
        // }

        return clone;
    }
}