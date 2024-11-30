using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Actions.Shared
{
    public class SharedDataPart
    {
        // Constructer présent pour la sérialisation
        public SharedDataPart()
        {

        }

        public SharedDataPart(string name, FileSystemTypes inventoryPartType, string clientInstanceId, string inventoryLetter, 
            string rootPath, string? relativePath,
            string? signatureGuid, string? signatureHash, bool hasAnalysisError)
        {
            Name = name;
            InventoryPartType = inventoryPartType;
            ClientInstanceId = clientInstanceId;
            InventoryLetter = inventoryLetter;
            RootPath = rootPath;
            RelativePath = relativePath;
            SignatureGuid = signatureGuid;
            SignatureHash = signatureHash;
            HasAnalysisError = hasAnalysisError;
        }

        public string Name { get; set; } = null!;
        
        public FileSystemTypes InventoryPartType { get; set; }

        public string ClientInstanceId { get; set; } = null!;
        
        public string InventoryLetter { get; set; }  = null!;

        public string RootPath { get; set; } = null!;
        
        public string? RelativePath { get; set; }

        public string? SignatureGuid { get; set; } 
        
        public string? SignatureHash { get; set; } 

        public bool HasAnalysisError { get; set; }

        protected bool Equals(SharedDataPart other)
        {
            return Equals(ClientInstanceId, other.ClientInstanceId) && 
                   RootPath.Equals(other.RootPath, StringComparison.InvariantCultureIgnoreCase) &&
                   Equals (RelativePath, other.RelativePath);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SharedDataPart) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ClientInstanceId.GetHashCode() * 397) ^ RootPath.GetHashCode();
            }
        }
    }
}
