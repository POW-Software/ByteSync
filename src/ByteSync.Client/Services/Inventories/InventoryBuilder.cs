using System.IO;
using System.Threading;
using ByteSync.Business;
using ByteSync.Business.Arguments;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class InventoryBuilder : IInventoryBuilder
{
    private readonly ILogger<InventoryBuilder> _logger;
    
    private const int FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 4194304; 
        
    public InventoryBuilder(SessionMember sessionMember, SessionSettings sessionSettings, InventoryProcessData inventoryProcessData, 
        OSPlatforms osPlatform, FingerprintModes fingerprintMode, ILogger<InventoryBuilder> logger)
    {
        _logger = logger;
        
        SessionMember = sessionMember;
        SessionSettings = sessionSettings;
        InventoryProcessData = inventoryProcessData;
        FingerprintMode = fingerprintMode;
        OSPlatform = osPlatform;
        
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
                    
        _logger.LogDebug("InventoryBuilder.AddInventoryPart: Creating inventory {InventoryId}", id);
            
        inventory.Endpoint = SessionMember.Endpoint;
        inventory.MachineName = SessionMember.MachineName;
        inventory.Letter = InventoryLetter!;

        return inventory;
    }
    
    private SessionMember SessionMember { get; }

    public Inventory Inventory { get; }
        
    private InventoryProcessData InventoryProcessData { get; }

    public FingerprintModes FingerprintMode { get; }
        
    public SessionSettings? SessionSettings { get; }

    public string InventoryLetter => SessionMember.GetLetter();

    private InventoryFileAnalyzer InventoryFileAnalyzer { get; }

    internal InventorySaver InventorySaver { get; }
        
    public InventoryIndexer Indexer { get; }
    
    private OSPlatforms OSPlatform { get; set; }

    private bool IgnoreHidden
    {
        get
        {
            return SessionSettings is { ExcludeHiddenFiles: true };
        }
    }
        
    private bool IgnoreSystem
    {
        get
        {
            return SessionSettings is { ExcludeSystemFiles: true };
        }
    }

    public InventoryPart AddInventoryPart(DataSource dataSource)
    {
        var inventoryPart = AddInventoryPart(dataSource.Path);

        inventoryPart.Code = dataSource.Code;

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
        _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory started", InventoryLetter);
        
        _logger.LogInformation("Local Inventory parts:");
        foreach (var inventoryPart in Inventory.InventoryParts)
        {
            _logger.LogInformation(" - {@letter:l}: {@path} ({type})", inventoryPart.Code, inventoryPart.RootPath, inventoryPart.InventoryPartType);
        }

        try
        {
            InventoryFileAnalyzer.Start();
            InventorySaver.Start(inventoryFullName);

            Inventory.StartDateTime = DateTimeOffset.Now;

            List<InventoryPart> inventoryPartsToAnalyze = Inventory.InventoryParts.ToList();
                
            foreach (var inventoryPart in inventoryPartsToAnalyze)
            {
                _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Identification started on part {Code:l} {Path}", 
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
                    
                _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Identification completed on {Code:l} {Path}", 
                    InventoryLetter, inventoryPart.Code, inventoryPart.RootPath);
            }
            
            Inventory.EndDateTime = DateTimeOffset.Now;

            InventorySaver.WriteInventory();
                
            _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Identification completed ({ItemsCount} files found)", 
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
        _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis started", InventoryLetter);
            
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

            _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis completed ({ItemsCount} files analyzed)",
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

        if (IgnoreHidden)
        {
            if (directoryInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                (OSPlatform == OSPlatforms.Linux && directoryInfo.Name.StartsWith(".")))
            {
                _logger.LogInformation("Directory {Directory} is ignored because considered as hidden", directoryInfo.FullName);
                return;
            }
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
                _logger.LogWarning("Directory {Directory} is ignored because it has flag 'ReparsePoint'", subDirectory.FullName);
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

        if (IgnoreHidden)
        {
            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) || 
                (OSPlatform == OSPlatforms.Linux && fileInfo.Name.StartsWith(".")))
            {
                _logger.LogInformation("File {File} is ignored because considered as hidden", fileInfo.FullName);
                return;
            }
        }


        if (IgnoreSystem)
        {
            if (fileInfo.Name.In("desktop.ini", "thumbs.db", ".desktop.ini", ".thumbs.db", ".DS_Store")
                || fileInfo.Attributes.HasFlag(FileAttributes.System))
            {
                _logger.LogInformation("File {File} is ignored because considered as system", fileInfo.FullName);
                return;
            }
        }
            
        // https://stackoverflow.com/questions/1485155/check-if-a-file-is-real-or-a-symbolic-link
        // Exemple pour créer un symlink :
        //  - Windows: New-Item -ItemType SymbolicLink -Path  C:\Users\paulf\Desktop\testVide\SL -Target C:\Users\paulf\Desktop\testA_
        if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            _logger.LogWarning("File {File} is ignored because it has flag 'ReparsePoint'. It might be a symolic link", fileInfo.FullName);
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
            
        // Non-Local OneDrive Files (not GoogleDrive)
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