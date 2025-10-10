using System.IO;
using System.Threading;
using ByteSync.Business;
using ByteSync.Business.Arguments;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.FileSystems;
using FastRsync.Signature;

namespace ByteSync.Services.Inventories;

public class InventoryFileAnalyzer : IInventoryFileAnalyzer
{
    private bool _isAllIdentified;
    
    private readonly FingerprintModes _fingerprintMode;
    private readonly InventoryProcessData _processData;
    
    private readonly IInventorySaver _saver;
    private readonly ILogger<InventoryFileAnalyzer> _logger;
    
    public InventoryFileAnalyzer(FingerprintModes fingerprintMode, InventoryProcessData processData, IInventorySaver saver,
        ILogger<InventoryFileAnalyzer> logger)
    {
        _fingerprintMode = fingerprintMode;
        _processData = processData;
        
        _saver = saver;
        _logger = logger;
        
        ManualResetSyncEvents = new ManualResetSyncEvents();
        SyncRoot = new object();
        FilesToAnalyze = new List<Tuple<FileDescription, FileInfo>>();
        HasFinished = new ManualResetEvent(false);
    }
    
    private List<Tuple<FileDescription, FileInfo>> FilesToAnalyze { get; }
    
    private object SyncRoot { get; }
    
    private ManualResetSyncEvents ManualResetSyncEvents { get; }
    
    public ManualResetEvent HasFinished { get; }
    
    public bool IsAllIdentified
    {
        get
        {
            lock (SyncRoot)
            {
                return _isAllIdentified;
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _isAllIdentified = value;
                
                if (FilesToAnalyze.Count == 0 && _isAllIdentified)
                {
                    ManualResetSyncEvents.SetEnd();
                }
            }
        }
    }
    
    public void Start()
    {
        HasFinished.Reset();
        
        Task.Run(HandleFilesAnalysis);
    }
    
    public void Stop()
    {
        lock (SyncRoot)
        {
            ManualResetSyncEvents.ResetEvent();
            ManualResetSyncEvents.SetEnd();
        }
    }
    
    public void RegisterFile(FileDescription fileDescription, FileInfo fileInfo)
    {
        lock (SyncRoot)
        {
            FilesToAnalyze.Add(new Tuple<FileDescription, FileInfo>(fileDescription, fileInfo));
            
            ManualResetSyncEvents.SetEvent();
        }
    }
    
    private void HandleFilesAnalysis()
    {
        while (ManualResetSyncEvents.WaitForEvent())
        {
        #if DEBUG
            if (DebugArguments.ForceSlow)
            {
                DebugUtils.DebugSleep(2);
            }
        #endif
            
            Tuple<FileDescription, FileInfo> tuple;
            lock (SyncRoot)
            {
                tuple = FilesToAnalyze[0];
                FilesToAnalyze.RemoveAt(0);
                
                if (FilesToAnalyze.Count == 0)
                {
                    ManualResetSyncEvents.ResetEvent();
                    
                    if (IsAllIdentified)
                    {
                        ManualResetSyncEvents.SetEnd();
                    }
                }
            }
            
            tuple.Item1.FingerprintMode = _fingerprintMode;
            
            try
            {
                ComputeFingerPrint(tuple);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InventoryFileAnalyzer.HandleFilesAnalysis");
                
                tuple.Item1.AnalysisErrorType = ex.GetType().Name;
                tuple.Item1.AnalysisErrorDescription = ex.Message;
                
                _processData.UpdateMonitorData(imd => imd.AnalyzeErrors += 1);
            }
            
            _processData.UpdateMonitorData(inventoryMonitorData =>
            {
                inventoryMonitorData.AnalyzedFiles += 1;
                inventoryMonitorData.ProcessedVolume += tuple.Item1.Size;
            });
        }
        
        HasFinished.Set();
    }
    
    private void ComputeFingerPrint(Tuple<FileDescription, FileInfo> tuple)
    {
        if (tuple.Item1.FingerprintMode == FingerprintModes.Sha256)
        {
            using var basisStream = new FileStream(tuple.Item2.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var sha256 = CryptographyUtils.ComputeSHA256(basisStream);
            
            tuple.Item1.Sha256 = sha256;
        }
        else
        {
            _logger.LogInformation("Analyzing file {FullName}", tuple.Item2.FullName);
            
            using var basisStream = new FileStream(tuple.Item2.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var memoryStream = new MemoryStream();
            var signatureBuilder = new SignatureBuilder();
            signatureBuilder.Build(basisStream, new SignatureWriter(memoryStream));
            
            var guid = Guid.NewGuid().ToString();
            
            _saver.AddSignature(guid, memoryStream);
            
            tuple.Item1.SignatureGuid = guid;
        }
    }
}