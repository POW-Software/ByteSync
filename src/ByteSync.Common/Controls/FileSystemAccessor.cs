using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ByteSync.Common.Interfaces;

namespace ByteSync.Common.Controls;

public class FileSystemAccessor : IFileSystemAccessor
{
    public async Task OpenFile(string filePath)
    {
        await Task.Run(() =>
        {
            // https://stackoverflow.com/questions/58846709/process-start-in-core-3-0-does-not-open-a-folder-just-by-its-name
            ProcessStartInfo startInfo = new (filePath) {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        });
            
        // System.Diagnostics.Process.Start(logFilePath);
    }

    public async Task OpenDirectory(string directoryPath)
    {
        await Task.Run(() =>
        {
            // https://stackoverflow.com/questions/58846709/process-start-in-core-3-0-does-not-open-a-folder-just-by-its-name
            ProcessStartInfo startInfo = new (directoryPath) {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        });
    }
        
    public async Task OpenDirectoryAndSelectFile(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                    string argument = "/select, \"" + filePath.Replace('/', '\\') + "\"";

                    Process.Start("explorer.exe", argument);
                }
                else
                {
                    // Linux : https://askubuntu.com/questions/1165908/how-to-open-file-browser-with-a-specific-file-selected-by-default

                    // Mac : https://stackoverflow.com/questions/39214539/opening-finder-from-terminal-with-file-selected
                }
            });
        }
        else
        {
            // todo: Implémenter traitement sur Linux et Mac
                
            FileInfo fileInfo = new FileInfo(filePath);

            await OpenDirectory(fileInfo.Directory.FullName);
        }
    }
}