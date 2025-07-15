using System.Reactive.Linq;
using System.Threading;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers;

public class FileDownloaderCache : IFileDownloaderCache
{
    private readonly ISessionService _sessionService;
    private readonly IFileDownloaderFactory _fileDownloaderFactory;
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Action<SharedFileDefinition, IDownloadPartsCoordinator>? OnPartsCoordinatorCreated { get; set; }

    public FileDownloaderCache(ISessionService sessionService, IFileDownloaderFactory fileDownloaderFactory)
    {
        _sessionService = sessionService;
        _fileDownloaderFactory = fileDownloaderFactory;

        FileDownloadersDictionary = new Dictionary<string, IFileDownloader>();
        
        // SyncRootHandlers = new Dictionary<SharedFileDefinition, Object>();

        _sessionService.SessionStatusObservable
            .DistinctUntilChanged()
            .Where(s => s == SessionStatus.Preparation)
            .SelectMany(_ => Observable.FromAsync(Reset))
            .Subscribe();
        
        _sessionService.SessionObservable
            .DistinctUntilChanged()
            .Where(s => s == null)
            .SelectMany(_ => Observable.FromAsync(Reset))
            .Subscribe();
        
        // _cloudSessionEventsHub.SessionResetted += (_, _) => Reset();
        // _cloudSessionEventsHub.CloudSessionQuitted += (_, _) => Reset();
    }

    // private object SyncRoot { get; }

    private Dictionary<string, IFileDownloader> FileDownloadersDictionary { get; }
    
    // private Dictionary<SharedFileDefinition, object> SyncRootHandlers { get; set; }
    
    public async Task<IFileDownloader> GetFileDownloader(SharedFileDefinition sharedFileDefinition)
    {
        // Add logging for downloader creation/reuse
        Console.WriteLine($"GetFileDownloader: checking for file Id: {sharedFileDefinition.Id}");
        await _semaphore.WaitAsync();
        try
        {
            // return _tokens?.Clone() as AuthenticationTokens;
            // IFileDownloader fileDownloader;
            if (!FileDownloadersDictionary.TryGetValue(sharedFileDefinition.Id, out var fileDownloader))
            {
                Console.WriteLine($"GetFileDownloader: creating new downloader for file Id: {sharedFileDefinition.Id}");
                fileDownloader = _fileDownloaderFactory.Build(sharedFileDefinition);
                FileDownloadersDictionary.Add(sharedFileDefinition.Id, fileDownloader);
                OnPartsCoordinatorCreated?.Invoke(sharedFileDefinition, fileDownloader.PartsCoordinator);
                
                // fileDownloader.Initialize(sharedFileDefinition, DownloadSemaphore);
            }
            else
            {
                Console.WriteLine($"GetFileDownloader: reusing existing downloader for file Id: {sharedFileDefinition.Id}");
            }

            return fileDownloader;

            // if (!FileDownloadersDictionary.ContainsKey(sharedFileDefinition))
            // {
            //     fileDownloader.Initialize(sharedFileDefinition, DownloadSemaphore);
            //     
            //     var fileDownloader = FileDownloadersDictionary[sharedFileDefinition];
            //     return fileDownloader;
            // }
        }
        finally
        {
            _semaphore.Release();
        }
        //
        //
        // var fileDownloader = _fileDownloaderCache.GetFileDownloader(sharedFileDefinition);
        //
        // if (fileDownloader == null)
        // {
        //     // if (sharedFileDefinition.IsSynchronization)
        //     // {
        //     //     if (_synchronizationActionsService.GetActionsGroupIds(sharedFileDefinition) == null)
        //     //     {
        //     //         var actionsGroupsIds =
        //     //             _connectionManager.ApiWrapper.GetActionsGroupsIds(sharedFileDefinition.SessionId, sharedFileDefinition)
        //     //                 .GetAwaiter().GetResult();
        //     //         _synchronizationActionsService.SetActionsGroupIds(sharedFileDefinition, actionsGroupsIds);
        //     //     }
        //     // }
        //         
        //     fileDownloader = Locator.Current.GetService<IFileDownloader>()!; 
        //     fileDownloader.Initialize(sharedFileDefinition, DownloadSemaphore);
        //
        //     _fileDownloaderCache.RegisterFileDownloader(ref fileDownloader, sharedFileDefinition);
        // }
        //
        // return fileDownloader;
        //
        //
        // lock (SyncRoot)
        // {
        //     if (FileDownloadersDictionary.ContainsKey(sharedFileDefinition))
        //     {
        //         var fileDownloader = FileDownloadersDictionary[sharedFileDefinition];
        //         return fileDownloader;
        //     }
        //
        //     return null;
        // }
    }

    // /// <summary>
    // /// Enregistre le fileDownloader en l'associant au sharedFileDefinition.
    // /// Comme on est en temps réel, il est possible qu'un autre file Downloader ait été associé entre temps.
    // /// C'est pour ça qu'on fonctionne en "ref", pour mettre à jour si besoin
    // /// </summary>
    // /// <param name="fileDownloader"></param>
    // /// <param name="sharedFileDefinition"></param>
    // /// <returns>True si on a ajoué, False si on a réutilisé</returns>
    // public bool RegisterFileDownloader(ref IFileDownloader fileDownloader, SharedFileDefinition sharedFileDefinition)
    // {
    //     lock (SyncRoot)
    //     {
    //         if (FileDownloadersDictionary.ContainsKey(sharedFileDefinition))
    //         {
    //             fileDownloader = FileDownloadersDictionary[sharedFileDefinition];
    //             // On a réutilisé un existant, on n'a pas ajouté
    //             return false;
    //         }
    //         else
    //         {
    //             FileDownloadersDictionary.Add(sharedFileDefinition, fileDownloader);
    //             // on n'a pas ajouté
    //             return true;
    //         }
    //     }
    // }

    public async Task RemoveFileDownloader(IFileDownloader fileDownloader)
    {
        Console.WriteLine($"RemoveFileDownloader: removing downloader for file Id: {fileDownloader.SharedFileDefinition.Id}");
        await _semaphore.WaitAsync();
        try
        {
            if (fileDownloader is FileDownloader concreteDownloader)
            {
                concreteDownloader.CleanupResources();
            }
            FileDownloadersDictionary.Remove(fileDownloader.SharedFileDefinition.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task Reset()
    {
        await _semaphore.WaitAsync();
        try
        {
            FileDownloadersDictionary.Clear();
            
            // SyncRootHandlers.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}