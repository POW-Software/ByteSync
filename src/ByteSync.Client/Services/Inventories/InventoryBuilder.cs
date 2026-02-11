using System.IO;
using System.Threading;
using ByteSync.Business;
using ByteSync.Business.Arguments;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;

namespace ByteSync.Services.Inventories;

public class InventoryBuilder : IInventoryBuilder
{
    private readonly ILogger<InventoryBuilder> _logger;
    
    public InventoryBuilder(SessionMember sessionMember, DataNode dataNode, SessionSettings sessionSettings,
        InventoryProcessData inventoryProcessData,
        OSPlatforms osPlatform, FingerprintModes fingerprintMode, ILogger<InventoryBuilder> logger,
        IInventoryFileAnalyzer inventoryFileAnalyzer,
        IInventorySaver inventorySaver,
        IInventoryIndexer inventoryIndexer,
        IFileSystemInspector? fileSystemInspector = null,
        IPosixFileTypeClassifier? posixFileTypeClassifier = null)
    {
        _logger = logger;
        
        SessionMember = sessionMember;
        DataNode = dataNode;
        SessionSettings = sessionSettings;
        InventoryProcessData = inventoryProcessData;
        FingerprintMode = fingerprintMode;
        OSPlatform = osPlatform;
        
        InventoryIndexer = inventoryIndexer;
        
        Inventory = InstantiateInventory();
        
        InventorySaver = inventorySaver;
        
        InventoryFileAnalyzer = inventoryFileAnalyzer;
        FileSystemInspector = fileSystemInspector ?? new FileSystemInspector(posixFileTypeClassifier);
    }
    
    private Inventory InstantiateInventory()
    {
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory();
        inventory.InventoryId = id;
        
        _logger.LogDebug("InventoryBuilder.AddInventoryPart: Creating inventory {InventoryId}", id);
        
        inventory.Endpoint = SessionMember.Endpoint;
        inventory.MachineName = SessionMember.MachineName;
        inventory.Code = InventoryCode;
        inventory.NodeId = DataNode.Id;
        
        return inventory;
    }
    
    private SessionMember SessionMember { get; }
    
    private DataNode DataNode { get; }
    
    public Inventory Inventory { get; }
    
    private InventoryProcessData InventoryProcessData { get; }
    
    public FingerprintModes FingerprintMode { get; }
    
    public SessionSettings? SessionSettings { get; }
    
    public string InventoryCode => DataNode.Code;
    
    private IInventoryFileAnalyzer InventoryFileAnalyzer { get; }
    
    private IInventorySaver InventorySaver { get; }
    
    public IInventoryIndexer InventoryIndexer { get; }
    
    private OSPlatforms OSPlatform { get; set; }
    
    private IFileSystemInspector FileSystemInspector { get; }
    
    private bool IgnoreHidden
    {
        get { return SessionSettings is { ExcludeHiddenFiles: true }; }
    }
    
    private bool IgnoreSystem
    {
        get { return SessionSettings is { ExcludeSystemFiles: true }; }
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
        
        if (ProtectedPaths.TryGetProtectedRoot(fullName, OSPlatform, out var protectedRoot))
        {
            _logger.LogWarning(
                "InventoryBuilder.AddInventoryPart: Path {Path} is under protected root {ProtectedRoot} and will be rejected",
                fullName, protectedRoot);
            
            throw new InvalidOperationException(
                $"Path '{fullName}' is under protected root '{protectedRoot}'");
        }
        
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
        _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory started", InventoryCode);
        
        _logger.LogInformation("Local Inventory parts:");
        foreach (var inventoryPart in Inventory.InventoryParts)
        {
            _logger.LogInformation(" - {@letter:l}: {@path} ({type})", inventoryPart.Code, inventoryPart.RootPath,
                inventoryPart.InventoryPartType);
        }
        
        try
        {
            InventoryFileAnalyzer.Start();
            InventorySaver.Start(inventoryFullName);
            
            Inventory.StartDateTime = DateTimeOffset.Now;
            
            var inventoryPartsToAnalyze = Inventory.InventoryParts.ToList();
            
            foreach (var inventoryPart in inventoryPartsToAnalyze)
            {
                _logger.LogInformation(
                    "InventoryBuilder {Letter:l}: Local Inventory - Files Identification started on part {Code:l} {Path}",
                    InventoryCode, inventoryPart.Code, inventoryPart.RootPath);
                
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
                    InventoryCode, inventoryPart.Code, inventoryPart.RootPath);
            }
            
            Inventory.EndDateTime = DateTimeOffset.Now;
            
            InventorySaver.WriteInventory(Inventory);
            
            _logger.LogInformation(
                "InventoryBuilder {Letter:l}: Local Inventory - Files Identification completed ({ItemsCount} files found)",
                InventoryCode, Inventory.InventoryParts.Sum(ip => ip.FileDescriptions.Count));
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
        _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis started", InventoryCode);
        
        try
        {
            using var _ = cancellationToken.Register(() => { InventoryFileAnalyzer.Stop(); });
            
            InventorySaver.Start(inventoryFullName);
            
            foreach (var item in items.Where(i => i.FileSystemDescription is FileDescription))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    InventoryFileAnalyzer.Stop();
                    
                    break;
                }
                
                InventoryFileAnalyzer.RegisterFile(
                    (item.FileSystemDescription as FileDescription)!,
                    (item.FileSystemInfo as FileInfo)!);
            }
            
            InventoryFileAnalyzer.IsAllIdentified = true;
            if (cancellationToken.IsCancellationRequested)
            {
                InventoryFileAnalyzer.Stop();
            }
            
            InventoryFileAnalyzer.HasFinished.WaitOne();
            
            InventorySaver.WriteInventory(Inventory);
            
            _logger.LogInformation("InventoryBuilder {Letter:l}: Local Inventory - Files Analysis completed ({ItemsCount} files analyzed)",
                InventoryCode, items.Count);
        }
        finally
        {
            InventorySaver.Stop();
        }
    }
    
    private void ProcessSubDirectories(InventoryPart inventoryPart, DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        foreach (var subDirectory in directoryInfo.EnumerateDirectories())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            
            try
            {
                DoAnalyze(inventoryPart, subDirectory, cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                AddInaccessibleDirectoryAndLog(inventoryPart, subDirectory, SkipReason.Inaccessible, ex,
                    "Directory {Directory} is inaccessible and will be skipped");
                
                continue;
            }
            catch (DirectoryNotFoundException ex)
            {
                AddInaccessibleDirectoryAndLog(inventoryPart, subDirectory, SkipReason.NotFound, ex,
                    "Directory {Directory} not found during enumeration and will be skipped");
                
                continue;
            }
            catch (IOException ex)
            {
                AddInaccessibleDirectoryAndLog(inventoryPart, subDirectory, SkipReason.IoError, ex,
                    "Directory {Directory} IO error and will be skipped");
                
                continue;
            }
        }
    }
    
    private void ProcessFiles(InventoryPart inventoryPart, DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        foreach (var subFile in directoryInfo.EnumerateFiles())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            
            DoAnalyze(inventoryPart, subFile, cancellationToken);
        }
    }
    
    private void AddInaccessibleDirectoryAndLog(InventoryPart inventoryPart, DirectoryInfo directoryInfo, SkipReason reason,
        Exception ex, string message)
    {
        inventoryPart.IsIncompleteDueToAccess = true;
        var subDirectoryDescription = IdentityBuilder.BuildDirectoryDescription(inventoryPart, directoryInfo);
        subDirectoryDescription.IsAccessible = false;
        AddFileSystemDescription(inventoryPart, subDirectoryDescription);
        RecordSkippedEntry(inventoryPart, directoryInfo, reason);
        _logger.LogWarning(ex, message, directoryInfo.FullName);
    }
    
    private bool IsRootPath(InventoryPart inventoryPart, FileSystemInfo fileSystemInfo)
    {
        var rootPath = NormalizePath(inventoryPart.RootPath);
        var currentPath = NormalizePath(fileSystemInfo.FullName);
        var comparison = OSPlatform == OSPlatforms.Windows
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        
        return string.Equals(rootPath, currentPath, comparison);
    }
    
    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
    
    private bool ShouldIgnoreHiddenDirectory(DirectoryInfo directoryInfo)
    {
        if (!IgnoreHidden)
        {
            return false;
        }
        
        if (FileSystemInspector.IsHidden(directoryInfo, OSPlatform))
        {
            _logger.LogInformation("Directory {Directory} is ignored because considered as hidden", directoryInfo.FullName);
            
            return true;
        }
        
        return false;
    }
    
    private void DoAnalyze(InventoryPart inventoryPart, FileInfo fileInfo, CancellationToken cancellationToken = new())
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
        
        try
        {
            var isRoot = IsRootPath(inventoryPart, fileInfo);
            
            if (TryHandleFileSkip(inventoryPart, fileInfo, isRoot))
            {
                return;
            }
            
            var fileDescription = IdentityBuilder.BuildFileDescription(inventoryPart, fileInfo);
            
            AddFileSystemDescription(inventoryPart, fileDescription);
            
            InventoryIndexer.Register(fileDescription, fileInfo);
        }
        catch (UnauthorizedAccessException ex)
        {
            AddInaccessibleFileAndLog(inventoryPart, fileInfo, SkipReason.Inaccessible, ex,
                "File {File} is inaccessible and will be skipped");
        }
        catch (DirectoryNotFoundException ex)
        {
            AddInaccessibleFileAndLog(inventoryPart, fileInfo, SkipReason.NotFound, ex,
                "File {File} parent directory not found and will be skipped");
        }
        catch (IOException ex)
        {
            AddInaccessibleFileAndLog(inventoryPart, fileInfo, SkipReason.IoError, ex,
                "File {File} IO error and will be skipped");
        }
    }
    
    private bool TryHandleFileSkip(InventoryPart inventoryPart, FileInfo fileInfo, bool isRoot)
    {
        var entryKind = FileSystemInspector.ClassifyEntry(fileInfo);
        if (entryKind == FileSystemEntryKind.Symlink)
        {
            RecordSkippedEntry(inventoryPart, fileInfo, SkipReason.Symlink, FileSystemEntryKind.Symlink);
            
            return true;
        }
        
        if (IsPosixSpecialFile(entryKind))
        {
            AddPosixSpecialFileAndLog(inventoryPart, fileInfo, entryKind);
            
            return true;
        }
        
        if (!isRoot && ShouldIgnoreHiddenFile(fileInfo))
        {
            RecordSkippedEntry(inventoryPart, fileInfo, SkipReason.Hidden, FileSystemEntryKind.RegularFile);
            
            return true;
        }
        
        if (!isRoot)
        {
            var systemSkipReason = GetSystemSkipReason(fileInfo);
            if (systemSkipReason.HasValue)
            {
                RecordSkippedEntry(inventoryPart, fileInfo, systemSkipReason.Value, FileSystemEntryKind.RegularFile);
                
                return true;
            }
        }
        
        if (!FileSystemInspector.Exists(fileInfo))
        {
            RecordSkippedEntry(inventoryPart, fileInfo, SkipReason.NotFound);
            
            return true;
        }
        
        if (FileSystemInspector.IsOffline(fileInfo) || IsRecallOnDataAccess(fileInfo))
        {
            RecordSkippedEntry(inventoryPart, fileInfo, SkipReason.Offline, FileSystemEntryKind.RegularFile);
            
            return true;
        }
        
        return false;
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
        
        var entryKind = FileSystemInspector.ClassifyEntry(directoryInfo);
        if (entryKind == FileSystemEntryKind.Symlink)
        {
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.Symlink, FileSystemEntryKind.Symlink);
            
            return;
        }
        
        if (IsPosixSpecialFile(entryKind))
        {
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.SpecialPosixFile, entryKind);
            _logger.LogWarning("Directory {Directory} is a POSIX special file ({EntryKind}) and will be skipped",
                directoryInfo.FullName, entryKind);
            
            return;
        }
        
        if (!IsRootPath(inventoryPart, directoryInfo) && ShouldIgnoreHiddenDirectory(directoryInfo))
        {
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.Hidden, FileSystemEntryKind.Directory);
            
            return;
        }
        
        if (!IsRootPath(inventoryPart, directoryInfo) && ShouldIgnoreNoiseDirectory(directoryInfo))
        {
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.NoiseEntry, FileSystemEntryKind.Directory);
            
            return;
        }
        
        var directoryDescription = IdentityBuilder.BuildDirectoryDescription(inventoryPart, directoryInfo);
        
        AddFileSystemDescription(inventoryPart, directoryDescription);
        
        InventoryIndexer.Register(directoryDescription, directoryInfo);
        
        try
        {
            ProcessSubDirectories(inventoryPart, directoryInfo, cancellationToken);
            ProcessFiles(inventoryPart, directoryInfo, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            directoryDescription.IsAccessible = false;
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.Inaccessible);
            _logger.LogWarning(ex, "Directory {Directory} is inaccessible and will be skipped", directoryInfo.FullName);
        }
        catch (DirectoryNotFoundException ex)
        {
            directoryDescription.IsAccessible = false;
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.NotFound);
            _logger.LogWarning(ex, "Directory {Directory} not found during enumeration and will be skipped", directoryInfo.FullName);
        }
        catch (IOException ex)
        {
            directoryDescription.IsAccessible = false;
            RecordSkippedEntry(inventoryPart, directoryInfo, SkipReason.IoError);
            _logger.LogWarning(ex, "Directory {Directory} IO error and will be skipped", directoryInfo.FullName);
        }
    }
    
    private bool ShouldIgnoreHiddenFile(FileInfo fileInfo)
    {
        if (!IgnoreHidden)
        {
            return false;
        }
        
        if (FileSystemInspector.IsHidden(fileInfo, OSPlatform))
        {
            _logger.LogInformation("File {File} is ignored because considered as hidden", fileInfo.FullName);
            
            return true;
        }
        
        return false;
    }
    
    private bool ShouldIgnoreNoiseDirectory(DirectoryInfo directoryInfo)
    {
        if (!IgnoreSystem)
        {
            return false;
        }
        
        if (FileSystemInspector.IsNoiseEntryName(directoryInfo.Name, OSPlatform))
        {
            _logger.LogInformation("Directory {Directory} is ignored because considered as noise", directoryInfo.FullName);
            
            return true;
        }
        
        return false;
    }
    
    private SkipReason? GetSystemSkipReason(FileInfo fileInfo)
    {
        if (!IgnoreSystem)
        {
            return null;
        }
        
        if (FileSystemInspector.IsNoiseEntryName(fileInfo.Name, OSPlatform))
        {
            _logger.LogInformation("File {File} is ignored because considered as noise", fileInfo.FullName);
            
            return SkipReason.NoiseEntry;
        }
        
        if (FileSystemInspector.IsSystemAttribute(fileInfo))
        {
            _logger.LogInformation("File {File} is ignored because considered as system", fileInfo.FullName);
            
            return SkipReason.SystemAttribute;
        }
        
        return null;
    }
    
    private bool IsRecallOnDataAccess(FileInfo fileInfo)
    {
        return FileSystemInspector.IsRecallOnDataAccess(fileInfo);
    }
    
    private void AddInaccessibleFileAndLog(InventoryPart inventoryPart, FileInfo fileInfo, SkipReason reason,
        Exception ex, string message)
    {
        inventoryPart.IsIncompleteDueToAccess = true;
        var relativePath = BuildRelativePath(inventoryPart, fileInfo);
        var fileDescription = new FileDescription(inventoryPart, relativePath)
        {
            IsAccessible = false
        };
        AddFileSystemDescription(inventoryPart, fileDescription);
        RecordSkippedEntry(inventoryPart, fileInfo, reason);
        _logger.LogWarning(ex, message, fileInfo.FullName);
    }
    
    private void AddPosixSpecialFileAndLog(InventoryPart inventoryPart, FileInfo fileInfo, FileSystemEntryKind entryKind)
    {
        RecordSkippedEntry(inventoryPart, fileInfo, SkipReason.SpecialPosixFile, entryKind);
        _logger.LogWarning("File {File} is a POSIX special file ({EntryKind}) and will be skipped", fileInfo.FullName, entryKind);
    }
    
    private static bool IsPosixSpecialFile(FileSystemEntryKind entryKind)
    {
        return entryKind is
            FileSystemEntryKind.BlockDevice or
            FileSystemEntryKind.CharacterDevice or
            FileSystemEntryKind.Fifo or
            FileSystemEntryKind.Socket;
    }
    
    private string BuildRelativePath(InventoryPart inventoryPart, FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo switch
        {
            FileInfo fileInfo => BuildRelativePath(inventoryPart, fileInfo),
            DirectoryInfo directoryInfo => BuildRelativePath(inventoryPart, directoryInfo),
            _ => string.Empty
        };
    }
    
    private string BuildRelativePath(InventoryPart inventoryPart, FileInfo fileInfo)
    {
        if (inventoryPart.InventoryPartType != FileSystemTypes.Directory)
        {
            return "/" + fileInfo.Name;
        }
        
        var rawRelativePath = IOUtils.ExtractRelativePath(fileInfo.FullName, inventoryPart.RootPath);
        var normalizedPath = OSPlatform == OSPlatforms.Windows
            ? rawRelativePath.Replace(Path.DirectorySeparatorChar, IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR)
            : rawRelativePath;
        
        if (!normalizedPath.StartsWith(IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR))
        {
            normalizedPath = IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR + normalizedPath;
        }
        
        return normalizedPath;
    }
    
    private string BuildRelativePath(InventoryPart inventoryPart, DirectoryInfo directoryInfo)
    {
        if (inventoryPart.InventoryPartType != FileSystemTypes.Directory)
        {
            return "/" + directoryInfo.Name;
        }
        
        var rawRelativePath = IOUtils.ExtractRelativePath(directoryInfo.FullName, inventoryPart.RootPath);
        var normalizedPath = OSPlatform == OSPlatforms.Windows
            ? rawRelativePath.Replace(Path.DirectorySeparatorChar, IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR)
            : rawRelativePath;
        
        if (!normalizedPath.StartsWith(IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR))
        {
            normalizedPath = IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR + normalizedPath;
        }
        
        return normalizedPath;
    }
    
    private void RecordSkippedEntry(InventoryPart inventoryPart, FileSystemInfo fileSystemInfo, SkipReason reason,
        FileSystemEntryKind? detectedKind = null)
    {
        string relativePath;
        try
        {
            relativePath = BuildRelativePath(inventoryPart, fileSystemInfo);
        }
        catch (Exception)
        {
            relativePath = string.Empty;
        }
        
        var entry = new SkippedEntry
        {
            FullPath = fileSystemInfo.FullName,
            RelativePath = relativePath,
            Name = fileSystemInfo.Name,
            Reason = reason,
            DetectedKind = detectedKind
        };
        
        InventoryProcessData.RecordSkippedEntry(entry);
    }
    
    private void AddFileSystemDescription(InventoryPart inventoryPart, FileSystemDescription fileSystemDescription)
    {
        if (fileSystemDescription.RelativePath.IsNotEmpty()
            && !fileSystemDescription.RelativePath.Equals(IdentityBuilder.GLOBAL_DIRECTORY_SEPARATOR.ToString()))
        {
            inventoryPart.AddFileSystemDescription(fileSystemDescription);
            
            if (fileSystemDescription is FileDescription fileDescription)
            {
                InventoryProcessData.UpdateMonitorData(imd =>
                {
                    if (fileDescription.IsAccessible)
                    {
                        imd.IdentifiedVolume += fileDescription.Size;
                    }
                    else
                    {
                        imd.IdentificationErrors += 1;
                    }
                    
                    imd.IdentifiedFiles += 1;
                });
            }
            else
            {
                InventoryProcessData.UpdateMonitorData(imd =>
                {
                    if (!fileSystemDescription.IsAccessible)
                    {
                        imd.IdentificationErrors += 1;
                    }
                    
                    imd.IdentifiedDirectories += 1;
                });
            }
        }
    }
}
