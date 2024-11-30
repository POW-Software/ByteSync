using System.IO;
using System.Threading.Tasks;
using ByteSync.Common.Helpers;
using PowSoftware.Common.Business.Versions;
using Serilog;

namespace ByteSync.Services.Updates;

public class UpdateReplacer
{
    public const string BAK_EXTENSION = "pow_upd_bak";
    public const string MACOS_BYTESYNC_APP_DIRECTORY = "ByteSync.app";
        
    public UpdateReplacer(SoftwareVersionFile softwareVersionFile)
    {
        SoftwareVersionFile = softwareVersionFile;

        RenamedFiles = new List<Tuple<string, string>>();
            
        MovedFiles = new List<Tuple<string, string>>();

        RenamedDirectories = new List<Tuple<string, string>>();
            
        MovedDirectories = new List<Tuple<string, string>>();
    }

    private SoftwareVersionFile SoftwareVersionFile { get; }
        
    public List<Tuple<string, string>> RenamedFiles { get; }
        
    public List<Tuple<string, string>> MovedFiles { get; }
        
    public List<Tuple<string, string>> RenamedDirectories { get; }

    public List<Tuple<string, string>> MovedDirectories { get; }
        
    public async Task ReplaceFilesAsync(string unzipLocation, string assemblyLocation)
    {
        await Task.Run(() => ReplaceFiles(unzipLocation, assemblyLocation));
    }

    private void ReplaceFiles(string unzipLocation, string assemblyLocation)
    {
        DirectoryInfo unzipDirectory; 
        DirectoryInfo assemblyDirectory; 

        GetDirectories(unzipLocation, assemblyLocation, out unzipDirectory, out assemblyDirectory);

        // if (isMacAppUpdate)
        // {
        //     ApplyMacUpdate(unzipDirectory, assemblyDirectory);
        // }
        // else
        // {
        //     ApplyGenericUpdate(unzipDirectory, assemblyDirectory);
        // }
            
        ApplyGenericUpdate(unzipDirectory, assemblyDirectory);
    }

    // private void ApplyMacUpdate(DirectoryInfo unzipDirectory, DirectoryInfo assemblyDirectory)
    // {
    //     DirectoryInfo currentByteSyncAppDirectory = assemblyDirectory;
    //     while (!currentByteSyncAppDirectory.Name.Equals("ByteSync.app"))
    //     {
    //         currentByteSyncAppDirectory = currentByteSyncAppDirectory.Parent!;
    //     }
    //
    //     string currentByteSyncAppDirectoryFullName = currentByteSyncAppDirectory.FullName;
    //     
    //     int cpt = 0;
    //     string renamedDestination = $"{currentByteSyncAppDirectoryFullName}.{BAK_EXTENSION}{cpt}";
    //
    //     while (Directory.Exists(renamedDestination))
    //     {
    //         cpt += 1;
    //         renamedDestination = $"{currentByteSyncAppDirectoryFullName}.{BAK_EXTENSION}{cpt}";
    //     }
    //
    //     currentByteSyncAppDirectory.MoveTo(renamedDestination);
    //     RenamedDirectories.Add(new Tuple<string, string>(currentByteSyncAppDirectoryFullName, renamedDestination));
    //
    //     var newByteSyncAppDirectory = currentByteSyncAppDirectory.Parent!.CreateSubdirectory(MACOS_BYTESYNC_APP_DIRECTORY);
    //     
    //     IOUtils.CopyFilesRecursively(unzipDirectory.GetDirectories()[0].FullName, newByteSyncAppDirectory.FullName);
    // }

    private void ApplyGenericUpdate(DirectoryInfo unzipDirectory, DirectoryInfo assemblyDirectory)
    {
        if (!unzipDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"unzipDirectory {unzipDirectory.FullName} not found");
        }

        if (!assemblyDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"assemblyDirectory {assemblyDirectory.FullName} not found");
        }



        Log.Information("UpdateReplacer: replacing files from source {UnzipRootDirectoryInfo}",
            unzipDirectory.FullName);

        DoReplaceFiles(unzipDirectory, assemblyDirectory);
        DoReplaceDirectories(unzipDirectory, assemblyDirectory);
    }

    private void GetDirectories(string unzipLocation, string assemblyLocation, out DirectoryInfo unzipDirectory, out DirectoryInfo assemblyDirectory)
    {
        unzipDirectory = new DirectoryInfo(unzipLocation);
        assemblyDirectory = new DirectoryInfo(assemblyLocation);
            
        bool isMacAppUpdate = false;
        if (SoftwareVersionFile.Platform == Platform.Osx)
        {
            DirectoryInfo? macOsDir = null;
            if (assemblyDirectory.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
            {
                macOsDir = assemblyDirectory;
            }
            else if (assemblyDirectory.Parent != null
                     && assemblyDirectory.Parent.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
            {
                macOsDir = assemblyDirectory.Parent;
            }

            if (macOsDir?.Parent != null &&
                macOsDir.Parent.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
                macOsDir.Parent.Parent != null &&
                macOsDir.Parent.Parent.Name.Equals(MACOS_BYTESYNC_APP_DIRECTORY, StringComparison.InvariantCultureIgnoreCase))
            {
                if (unzipDirectory.GetFiles().Length == 0 && unzipDirectory.GetDirectories().Length == 1
                                                          && unzipDirectory.GetDirectories()[0].Name.Equals(MACOS_BYTESYNC_APP_DIRECTORY, StringComparison.InvariantCultureIgnoreCase))
                {
                    isMacAppUpdate = true;
                        
                    assemblyDirectory = macOsDir.Parent.Parent;
                    unzipDirectory = unzipDirectory.GetDirectories()[0];
                }
            }
        }

        if (!isMacAppUpdate)
        {
            // On obtient le vrai répertoire racine qui contient les données de la version
            unzipDirectory = FindUnzipRoot(unzipDirectory);
        }
    }

    private void DoReplaceFiles(DirectoryInfo unzipRootDirectoryInfo, DirectoryInfo assemblyDirectory)
    {
        foreach (var fileInfo in unzipRootDirectoryInfo.GetFiles())
        {
            var destination = new FileInfo(IOUtils.Combine(assemblyDirectory.FullName, fileInfo.Name));

            string previousFullName = fileInfo.FullName;
            string nextFullName = destination.FullName;
                
            if (destination.Exists)
            {
                int cpt = 0;
                string renamedDestination = $"{nextFullName}.{BAK_EXTENSION}{cpt}";

                while (File.Exists(renamedDestination))
                {
                    cpt += 1;
                    renamedDestination = $"{nextFullName}.{BAK_EXTENSION}{cpt}";
                }

                // On renomme, et on garde en mémoire
                destination.MoveTo(renamedDestination);

                RenamedFiles.Add(new Tuple<string, string>(nextFullName, renamedDestination));
            }

            fileInfo.MoveTo(nextFullName);

            MovedFiles.Add(new Tuple<string, string>(previousFullName, nextFullName));
        }
    }
        
    private void DoReplaceDirectories(DirectoryInfo unzipRootDirectoryInfo, DirectoryInfo assemblyDirectory)
    {
        foreach (var directoryInfo in unzipRootDirectoryInfo.GetDirectories())
        {
            var destination = new DirectoryInfo(IOUtils.Combine(assemblyDirectory.FullName, directoryInfo.Name));

            string previousFullName = directoryInfo.FullName;
            string nextFullName = destination.FullName;
                
            if (destination.Exists)
            {
                int cpt = 0;
                string renamedDestination = $"{nextFullName}.{BAK_EXTENSION}{cpt}";

                while (Directory.Exists(renamedDestination))
                {
                    cpt += 1;
                    renamedDestination = $"{nextFullName}.{BAK_EXTENSION}{cpt}";
                }

                // On renomme, et on garde en mémoire
                destination.MoveTo(renamedDestination);

                RenamedDirectories.Add(new Tuple<string, string>(nextFullName, renamedDestination));
            }

            directoryInfo.MoveTo(nextFullName);

            MovedDirectories.Add(new Tuple<string, string>(previousFullName, nextFullName));
        }
    }

    /// <summary>
    /// Trouve le premier répertoire qui contient l'exécutable du logiciel.
    /// Cette méthode est faite pour descendre le long de l'archive, si celle-ci contenait plusieurs répertoires à la racine.
    /// </summary>
    /// <param name="anUnzipDirectory"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private DirectoryInfo FindUnzipRoot(DirectoryInfo anUnzipDirectory)
    {
        var subDirectories = anUnzipDirectory.GetDirectories();
        var subFiles = anUnzipDirectory.GetFiles();

        if (subFiles.Length > 0)
        {
            if (subFiles.Any(f => f.Name.Equals(SoftwareVersionFile.ExecutableFileName,
                    StringComparison.InvariantCultureIgnoreCase)))
            {
                return anUnzipDirectory;
            }
            else
            {
                // Premier répertoire où on trouve des fichiers, mais ExecutableFileName non trouvé, on lève une exception
                throw new Exception("Unable to find a file named " + SoftwareVersionFile.ExecutableFileName);
            }
        }
        else
        {
            if (subDirectories.Length == 0)
            {
                // Le répertoire est vide, on ne peut donc pas parcourir ses enfants...
                // On s'arrête là, sans avoir trouvé de fichier
                throw new Exception("Unable to find any file");
            }
            else if (subDirectories.Length >= 2)
            {
                // Il y a plusieurs répertoires, impossible de déterminer le chemin à suivre pour continuer
                throw new Exception("Unable to parse data structure");
            }
            else
            {
                // Il y a un répertoire, on continue avec ce répertoire
                return FindUnzipRoot(subDirectories[0]);
            }
        }
    }

    public async Task DeleteBackupData()
    {
        await Task.Run(() =>
        {
            foreach (var renamedFile in RenamedFiles)
            {
                try
                {
                    File.Delete(renamedFile.Item2);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot delete {BackupData}", renamedFile.Item2);
                }
            }

            foreach (var renamedDirectory in RenamedDirectories)
            {
                try
                {
                    Directory.Delete(renamedDirectory.Item2, true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot delete {BackupData}", renamedDirectory.Item2);
                }
            }
        });
    }
}