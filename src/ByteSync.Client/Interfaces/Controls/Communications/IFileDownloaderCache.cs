using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileDownloaderCache
{
    Task<IFileDownloader> GetFileDownloader(SharedFileDefinition sharedFileDefinition);
    
    /// <summary>
    /// Enregistre le fileDownloader en l'associant au sharedFileDefinition.
    /// Comme on est en temps réel, il est possible qu'un autre file Downloader ait été associé entre temps.
    /// C'est pour ça qu'on fonctionne en "ref", pour mettre à jour si besoin
    /// </summary>
    /// <param name="fileDownloader"></param>
    /// <param name="sharedFileDefinition"></param>
    /// <returns></returns>
    //bool RegisterFileDownloader(ref IFileDownloader fileDownloader, SharedFileDefinition sharedFileDefinition);
    
    Task RemoveFileDownloader(IFileDownloader fileDownloader);

    // object GetSyncRoot(SharedFileDefinition sharedFileDefinition);
}