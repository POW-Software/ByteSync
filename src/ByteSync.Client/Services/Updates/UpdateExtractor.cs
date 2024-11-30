using System.IO;
using System.Text;
using System.Threading.Tasks;
using ByteSync.Common.Controls;
using ByteSync.Common.Helpers;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using PowSoftware.Common.Business.Versions;
using Serilog;

namespace ByteSync.Services.Updates;

public class UpdateExtractor
{
    public async Task ExtractAsync(SoftwareVersionFile softwareVersionFile, string downloadLocation, string unzipLocation)
    {
        await Task.Run(() => Extract(softwareVersionFile, downloadLocation, unzipLocation));
    }

    public void Extract(SoftwareVersionFile softwareVersionFile, string downloadLocation, string unzipLocation)
    {
        bool canExtract = false;
            
        // windows
        if (softwareVersionFile.Platform.In(Platform.Windows)
            && softwareVersionFile.FileName.ToLower().EndsWith(".zip"))
        {
            canExtract = ExtractWindows(downloadLocation, unzipLocation);
        }
            
        // macos
        if (softwareVersionFile.Platform.In(Platform.Osx)
            && softwareVersionFile.FileName.ToLower().EndsWith(".zip"))
        {
            canExtract = ExtractMacOs(downloadLocation, unzipLocation);
        }

        // linux
        if (softwareVersionFile.Platform.In(Platform.Linux)
            && softwareVersionFile.FileName.ToLower().EndsWith(".tar.gz"))
        {
            canExtract = ExtractLinux(downloadLocation, unzipLocation);
        }

        if (!canExtract)
        {
            throw new Exception("Cannot extract archive with provided parameters");
        }
    }

    private static bool ExtractWindows(string downloadLocation, string unzipLocation)
    {
        bool canExtract;
        canExtract = true;

        Log.Information("UpdateExtractor: Extracting {DownloadLocation} to {UnzipLocation}", downloadLocation,
            unzipLocation);

        FastZip fastZip = new FastZip();
        fastZip.ExtractZip(downloadLocation, unzipLocation, null);
            
        return canExtract;
    }

    private bool ExtractMacOs(string downloadLocation, string unzipLocation)
    {
        bool canExtract;
        // Pour MacOS, on utilise le fichier ByteSync.app.zip.
        // On le décompresse dans ByteSync.app.
        // Problème, ByteSync.app.zip contient à la racine le répertoire ByteSync.app. 
        // => On doit donc décompresser, puis déplacer

        canExtract = true;

        Log.Information("UpdateExtractor: Extracting {DownloadLocation} to {UnzipLocation}", downloadLocation,
            unzipLocation);

        // FastZip fastZip = new FastZip();
        // fastZip.ExtractZip(downloadLocation, unzipLocation, null);
            
        DirectoryInfo unzipDirectoryInfo = new DirectoryInfo(unzipLocation);

        // Pour dézipper, il y a habituellement la commande unzip qui est disponible
        // unzip a l'avantage de garder les permissions des fichiers
        // Si unzip n'est pas disponible, on utilise FastZip et on restaure la permission sur ByteSync
        if (UnixHelper.CommandExists("unzip"))
        {
            // Extraction avec unzip
            GetCommandRunner().RunCommand("unzip", $"-qq \"{downloadLocation}\" -d \"{unzipLocation}\"");
        }
        else
        {
            // Extraction avec FastZip
            FastZip fastZip = new FastZip();
            fastZip.ExtractZip(downloadLocation, unzipLocation, null);
                
            // restauration de la permission
            ApplyPermission(unzipLocation);
        }
            
        var byteSyncAppSubDir = unzipDirectoryInfo
            .GetDirectories().Single(d => d.Name.Equals("ByteSync.app"));

        // var expectedFilesSystemsCount = byteSyncAppSubDir.GetFileSystemInfos("*", SearchOption.AllDirectories).Length - 1;

        var contentsDirectory = byteSyncAppSubDir
            .GetDirectories().Single(d => d.Name.Equals("Contents"));

        var destination = IOUtils.Combine(unzipLocation, contentsDirectory.Name);
        contentsDirectory.MoveTo(destination);

        // MoveTo peut prendre plus ou moins de temps, on doit attendre que les fichiers soient présents 
        // dans la destination
        // Log.Information("UpdateExtractor: Waiting for update content moved to {destination}", destination);
        // Thread.Sleep(300); // On commence par une attente initiale de 300 ms
        // var destinationDirectoryInfo = new DirectoryInfo(destination);
        // var filesSystemCount = destinationDirectoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories).Length;
        // int cpt = 0;
        // while (filesSystemCount < expectedFilesSystemsCount && cpt < 15)
        // {
        //     cpt += 1;
        //     Log.Information("UpdateExtractor: Waiting 1 second");
        //     Thread.Sleep(1000); // On attend que le déplacement se poursuive : 1 sec
        //
        //     destinationDirectoryInfo = new DirectoryInfo(destination);
        //     filesSystemCount = destinationDirectoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories).Length;
        // }
        //
        // if (filesSystemCount < expectedFilesSystemsCount)
        // {
        //     // Echec
        //     throw new ApplicationException("UpdateExtractor: Unable to extract data");
        // }
        // else
        // {
        //     //  Thread.Sleep(50);
        // }

        byteSyncAppSubDir.Delete();
            
        return canExtract;
    }

    private bool ExtractLinux(string downloadLocation, string unzipLocation)
    {
        bool canExtract;
        canExtract = true;

        Log.Information("UpdateExtractor: Untarring {DownloadLocation} to '{UnzipLocation}", downloadLocation,
            unzipLocation);

        // https://stackoverflow.com/questions/38120651/unzip-tar-gz-with-sharpziplib
        using var inStream = File.OpenRead(downloadLocation);
        using var gzipStream = new GZipInputStream(inStream);

        using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
        tarArchive.ExtractContents(unzipLocation);

        ApplyPermission(unzipLocation);

        return canExtract;
    }
        
    private void ApplyPermission(string unzipLocation)
    {
        var unzipDirectory = new DirectoryInfo(unzipLocation);

        if (UnixHelper.CommandExists("chmod"))
        {
            //var byteSyncFileBeforeMove2 = unzipDirectory.GetFiles("ByteSync", SearchOption.AllDirectories).Where(f => f.Name.Equals("ByteSync"));

            var byteSyncFileBeforeMove = unzipDirectory.GetFiles("ByteSync", SearchOption.AllDirectories)
                .Where(f => f.Name.Equals("ByteSync"))
                .MaxBy(f => f.LastWriteTime);
            if (byteSyncFileBeforeMove != null)
            {
                Log.Information("UpdateExtractor: Executing chmod u+x on {path}", byteSyncFileBeforeMove.FullName);
                GetCommandRunner().RunCommand("chmod", $"+x \"{byteSyncFileBeforeMove.FullName}\"");
            }
            else
            {
                Log.Warning("UpdateExtractor: ByteSync file not found, can not update permissions");
            }
        }
        else
        {
            Log.Warning("UpdateExtractor: Can not update permissions because chmod command is missing");
        }
    }

    // private static bool CommandExists(string commandName)
    // {
    //     bool? result = null;
    //     Action<string, string> handler = (standardOutput, standardError) =>
    //     {
    //         result = standardOutput.IsNotEmpty() && standardError.IsNotEmpty();
    //     };
    //     
    //     CommandRunner.RunCommand("command", $"\"-v {commandName}\"", null, handler);
    //
    //     return result!.Value;
    // }
        
    private CommandRunner GetCommandRunner()
    {
        CommandRunner commandRunner = new CommandRunner { LogLevel = CommandRunner.LogLevels.Minimal, NeedBash = true };

        return commandRunner;
    }

    public void Bump()
    {
        var _ = new FastZip();
    }
}