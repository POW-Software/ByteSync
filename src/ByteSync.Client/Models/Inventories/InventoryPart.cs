using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Models.FileSystems;
using Newtonsoft.Json;

namespace ByteSync.Models.Inventories
{
    [JsonObject(IsReference = true)] 
    public class InventoryPart
    {
        public InventoryPart()
        {
            FileSystemDescriptions = new List<FileSystemDescription>();
        }

        public InventoryPart(Inventory inventory, string rootPath, FileSystemTypes inventoryPartType)
        {
            Inventory = inventory;
            RootPath = rootPath;
            InventoryPartType = inventoryPartType;

            FileSystemDescriptions = new List<FileSystemDescription>();
        }

        public Inventory Inventory { get; set; }
        
        public string RootPath { get; set; }

        public FileSystemTypes InventoryPartType { get; set; }

        public List<FileSystemDescription> FileSystemDescriptions { get; set; }

        public string Code { get; set; }

        public List<FileDescription> FileDescriptions
        {
            get
            {
                List<FileDescription> fileDescriptions = new List<FileDescription>();

                foreach (FileSystemDescription fileSystemDescription in FileSystemDescriptions)
                {
                    if (fileSystemDescription is FileDescription fileDescription)
                    {
                        fileDescriptions.Add(fileDescription);
                    }
                }

                return fileDescriptions;
            }
        }

        public List<DirectoryDescription> DirectoryDescriptions
        {
            get
            {
                List<DirectoryDescription> directoryDescriptions = new List<DirectoryDescription>();

                foreach (FileSystemDescription fileSystemDescription in FileSystemDescriptions)
                {
                    if (fileSystemDescription is DirectoryDescription directoryDescription)
                    {
                        directoryDescriptions.Add(directoryDescription);
                    }
                }

                return directoryDescriptions;
            }
        }

        public string RootName
        {
            get
            {
                string directorySeparatorChar;
                switch (Inventory.Endpoint.OSPlatform)
                {
                    case OSPlatforms.Windows:
                        directorySeparatorChar = "\\";
                        break;
                    case OSPlatforms.Linux:
                    case OSPlatforms.MacOs:
                        directorySeparatorChar = "/";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(directorySeparatorChar));
                }
                
                return RootPath.Substring(RootPath.LastIndexOf(directorySeparatorChar, StringComparison.Ordinal));
            }
        }

        protected bool Equals(InventoryPart other)
        {
            return Equals(Inventory, other.Inventory) && RootPath == other.RootPath && InventoryPartType == other.InventoryPartType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InventoryPart) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Inventory.GetHashCode();
                hashCode = (hashCode * 397) ^ RootPath.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) InventoryPartType;
                return hashCode;
            }
        }

        public override string ToString()
        {
#if DEBUG
            return $"InventoryPart {RootName} {RootPath}";
#endif

#pragma warning disable 162
            return base.ToString();
#pragma warning restore 162
        }
    }
}
