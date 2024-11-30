using ByteSync.Business;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.FileSystems
{
    public class FileDescription : FileSystemDescription
    {
        public FileDescription()
        {

        }

        internal FileDescription(InventoryPart inventoryPart, string relativePath)
            : base(inventoryPart, relativePath)
        {

        }

        public FingerprintModes? FingerprintMode { get; set; }

        public string? Sha256 { get; set; }

        public string? SignatureGuid { get; set; }

        public long Size { get; set; }

        public DateTime CreationTimeUtc { get; set; }
        
        public DateTime LastWriteTimeUtc { get; set; }
        
        public string? AnalysisErrorType { get; set; }
        
        public string? AnalysisErrorDescription { get; set; }

        public override FileSystemTypes FileSystemType
        {
            get
            {
                return FileSystemTypes.File;
            }
        }

        public bool HasAnalysisError
        {
            get
            {
                return AnalysisErrorDescription.IsNotEmpty();
            }
        }
    }
}
