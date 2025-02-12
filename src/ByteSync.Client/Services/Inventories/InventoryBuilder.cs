using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Arguments;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using Serilog;

namespace ByteSync.Services.Inventories;

public class InventoryBuilder
{
    private const int FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 4194304; 
        
    public InventoryBuilder(string inventoryLetter, SessionSettings sessionSettings,
        InventoryProcessData inventoryProcessData, ByteSyncEndpoint byteSyncEndpoint,
        string machineName, FingerprintModes fingerprintMode = FingerprintModes.Rsync)
    {
        InventoryLetter = inventoryLetter;
        SessionSettings = sessionSettings;
        InventoryProcessData = inventoryProcessData;
        Endpoint = byteSyncEndpoint;
        FingerprintMode = fingerprintMode;
        MachineName = machineName;

        InventoryMonitorData = new InventoryMonitorData();

            
        InventoryFileAnalyzer = new InventoryFileAnalyzer(this, RaiseFileAnalyzed, RaiseFileAnalyzeError);
        InventorySaver = new InventorySaver(this);

        Indexer = new InventoryIndexer();

        Inventory = InstantiateInventory();
    }

    private Inventory InstantiateInventory()
    {
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory();
        inventory.InventoryId = id;
                    
        Log.Debug("InventoryBuilder.AddInventoryPart: Creating inventory {InventoryId}", id);
            
        inventory.Endpoint = Endpoint!;
        inventory.MachineName = MachineName;
        inventory.Letter = InventoryLetter!;

        return inventory;
    }

    public Inventory Inventory { get; }
        
    private InventoryProcessData InventoryProcessData { get; }
    
    private InventoryMonitorData InventoryMonitorData { get; }

    public FingerprintModes FingerprintMode { get; }
        
    public SessionSettings? SessionSettings { get; }

    public string InventoryLetter { get; }

    private InventoryFileAnalyzer InventoryFileAnalyzer { get; }

    internal InventorySaver InventorySaver { get; }
        
    public InventoryIndexer Indexer { get; }
        
    public ByteSyncEndpoint Endpoint { get; }
    
    public string MachineName { get; set; }

    public bool IgnoreHidden
    {
        get
        {
            return SessionSettings is { ExcludeHiddenFiles: true };
        }
    }
        
    public bool IgnoreSystem
    {
        get
        {
            return SessionSettings is { ExcludeSystemFiles: true };
        }
    }

    public InventoryPart AddInventoryPart(PathItem pathItem)
    {
        var inventoryPart = AddInventoryPart(pathItem.Path);

        inventoryPart.Code = pathItem.Code;

        return inventoryPart;
    }

    public InventoryPart AddInventoryPart(string fullName)
    {
        InventoryPart inventoryPart;

        if (Directory.Exists(fullName))
        {
            inventoryPart = new InventoryPart(Inventory, fullName, FileSystemTypes.Directory);
            Inventory.InventoryParts.Add(inventoryPart);
        }
        else if (File.Exists(fullName))
        {
            inventoryPart = new InventoryPart(Inventory, fullName, FileSystemTypes.File);
            Inventory.InventoryParts.Add(inventoryPart);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(fullName),
                $"Directory or file '{fullName}' does not exist");
        }

        return inventoryPart;
    }

    public async Task BuildBaseInventoryAsync(string inventoryFullName, CancellationToken cancellationToken = new())
    {
        await Task.Run(() => BuildBaseInventory(inventoryFullName, cancellationToken), cancellationToken);
    }

    private void BuildBaseInventory(string inventoryFullName, CancellationToken cancellationToken)
    {
        Log.Information("InventoryBuilder {Letter:l}: Local Inventory started", InventoryLetter);

        try
        {
            InventoryFileAnalyzer.Start();
            InventorySaver.Start(inventoryFullName);

            Inventory.StartDateTime = DateTimeOffset.Now;

            List<InventoryPart> inventoryPartsToAnalyze = Inventory.InventoryParts.ToList();
                
            foreach (var inventoryPart in inventoryPartsToAnalyze)
            {
                Log.Information("InventoryBuilder {Letter:l}: Local Inventory - Files Identification started on part {Code:l} {Path}", 
                    InventoryLetter, inventoryPart.Code, inventoryPart.RootPath);
                    
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (inventoryPart.InventoryPartType == FileSystemTypes.Directory)
                {
                    var directoryInfo = new DirectoryInfo(inventoryPart.RootPath);
                    DoAnalyze(inventoryPart, directoryInfo, cancellationToken);
                }
                else
                {
                    var fileInfo = new FileInfo(inventoryPart.RootPath);
                    DoAnalyze(inventoryPart, fileInfo, cancellationToken);
                }
                    
                Log.Information("InventoryBuilder {Letter:l}: Local Inventory - Files Identification completed on {Code:l} {Path}", 
                    InventoryLetter, inventoryPart.Code, inventoryPart.RootPath);
            }
            
            Inventory.EndDateTime = DateTimeOffset.Now;

            InventorySaver.WriteInventory();
                
            Log.Information("InventoryBuilder {Letter:l}: Local Inventory - Files Identification completed ({ItemsCount} files found)", 
                InventoryLetter, Inventory.InventoryParts.Sum(ip => ip.FileDescriptions.Count));
        }
        finally
        {
            InventorySaver.Stop();
        }
    }

    public async Task RunAnalysisAsync(string inventoryFullName, HashSet<IndexedItem> items, CancellationToken cancellationToken)
    {
        await Task.Run(() => RunAnalysis(inventoryFullName, items, cancellationToken), cancellationToken);
    }

    internal void RunAnalysis(string inventoryFullName, HashSet<IndexedItem> items, CancellationToken cancellationToken)
    {
        Log.Information("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis started", InventoryLetter);
            
        try
        {
            InventorySaver.Start(inventoryFullName);
                
            foreach (var item in items.Where(i => i.FileSystemDescription is FileDescription))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                    
                InventoryFileAnalyzer.RegisterFile(
                    (item.FileSystemDescription as FileDescription)!, 
                    (item.FileSystemInfo as FileInfo)!);
            }
                
            InventoryFileAnalyzer.IsAllIdentified = true;
                
            InventoryFileAnalyzer.HasFinished.WaitOne();
                
            InventorySaver.WriteInventory();

            Log.Information("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis completed ({ItemsCount} files analyzed)",
                InventoryLetter, items.Count);
        }
        finally
        {
            InventorySaver.Stop();
        }
    }

    private void DoAnalyze(InventoryPart inventoryPart, DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
    #if DEBUG
        if (DebugArguments.ForceSlow)
        {
            DebugUtils.DebugSleep(0.1d);
        }
    #endif
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (directoryInfo.Attributes.HasFlag(FileAttributes.Hidden) && IgnoreHidden)
        {
            Log.Information("Directory {Directory} is ignored because considered as hidden", directoryInfo.FullName);
            return;
        }

        var directoryDescription = IdentityBuilder.BuildDirectoryDescription(inventoryPart, directoryInfo);

        AddFileSystemDescription(inventoryPart, directoryDescription);
            
        Indexer.Register(directoryDescription, directoryInfo);

        foreach (var subDirectory in directoryInfo.GetDirectories())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
                
            // https://stackoverflow.com/questions/1485155/check-if-a-file-is-real-or-a-symbolic-link
            // Exemple pour créer un symlink :
            //  - Windows: New-Item -ItemType SymbolicLink -Path  C:\Users\paulf\Desktop\testVide\SL -Target C:\Users\paulf\Desktop\testA_
            if (subDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                Log.Warning("Directory {Directory} is ignored because it has flag 'ReparsePoint'", subDirectory.FullName);
                continue;
            }
                
            DoAnalyze(inventoryPart, subDirectory, cancellationToken);
        }

        foreach (var subFile in directoryInfo.GetFiles())    
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
                
            DoAnalyze(inventoryPart, subFile, cancellationToken);
        }
    }

    private void DoAnalyze(InventoryPart inventoryPart, FileInfo fileInfo, CancellationToken cancellationToken = new ())
    {
    #if DEBUG
        if (DebugArguments.ForceSlow)
        {
            DebugUtils.DebugSleep(0.1d);
        }
    #endif
            
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
            
        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) && IgnoreHidden)
        {
            Log.Information("File {File} is ignored because considered as hidden", fileInfo.FullName);
            return;
        }

        if (IgnoreSystem)
        {
            if (fileInfo.Name.In("desktop.ini", "thumbs.db", ".DS_Store"))
            {
                Log.Information("File {File} is ignored because considered as system", fileInfo.FullName);
                return;
            }
        }
            
        // https://stackoverflow.com/questions/1485155/check-if-a-file-is-real-or-a-symbolic-link
        // Exemple pour créer un symlink :
        //  - Windows: New-Item -ItemType SymbolicLink -Path  C:\Users\paulf\Desktop\testVide\SL -Target C:\Users\paulf\Desktop\testA_
        if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            Log.Warning("File {File} is ignored because it has flag 'ReparsePoint'. It might be a symolic link", fileInfo.FullName);
            return;
        }

        if (!fileInfo.Exists)
        {
            return;
        }
            
        if (fileInfo.Attributes.HasFlag(FileAttributes.Offline))
        {
            return;
        }
            
        // Fichiers OneDrive (GoogleDrive ?) non présents
        // https://docs.microsoft.com/en-gb/windows/win32/fileio/file-attribute-constants?redirectedfrom=MSDN
        // https://stackoverflow.com/questions/49301958/how-to-detect-onedrive-online-only-files
        // https://stackoverflow.com/questions/54560454/getting-full-file-attributes-for-files-managed-by-microsoft-onedrive
        if (((int)fileInfo.Attributes & FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) == FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS)
        {
            return;
        }

        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryPart, fileInfo);

        AddFileSystemDescription(inventoryPart, fileDescription);

        Indexer.Register(fileDescription, fileInfo);
    }

    private void AddFileSystemDescription(InventoryPart inventoryPart, FileSystemDescription fileSystemDescription)
    {
        if (fileSystemDescription.RelativePath.IsNotEmpty())
        {
            inventoryPart.AddFileSystemDescription(fileSystemDescription);

            if (fileSystemDescription is FileDescription fileDescription)
            {
                InventoryProcessData.UpdateMonitorData(imd =>
                {
                    imd.IdentifiedSize += fileDescription.Size;
                    imd.IdentifiedFiles += 1;
                });
            }
            else
            {
                InventoryProcessData.UpdateMonitorData(imd => imd.IdentifiedDirectories += 1);
            }
        }
    }

    private void RaiseFileAnalyzed(FileDescription fileDescription)
    {
        InventoryProcessData.UpdateMonitorData(inventoryMonitorData =>
        {
            inventoryMonitorData.AnalyzedFiles += 1;
            inventoryMonitorData.ProcessedSize += fileDescription.Size;
        });
    }
        
    private void RaiseFileAnalyzeError(FileDescription fileDescription)
    {
        InventoryProcessData.UpdateMonitorData(imd => imd.AnalyzeErrors += 1);
    }
}