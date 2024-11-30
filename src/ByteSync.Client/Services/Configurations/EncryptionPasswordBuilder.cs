using System.Runtime.InteropServices;
using ByteSync.Common.Controls;
using ByteSync.Common.Helpers;
using Microsoft.Win32;
using Serilog;

namespace ByteSync.Services.Configurations;

public sealed class EncryptionPasswordBuilder
{
    public bool HasTriedToGenerateEnvironmentEncryptionKey { get; private set; }
    
    public bool? IsEnvironmentEncryptionKeySuccess { get; private set; }

    public bool IsEnvironmentEncryptionKeyError { get; private set; }
    
    public bool HasTriedToLoadMachineId { get; private set; }
    
    public bool? IsMachineIdLoadingSuccess { get; private set; }

    public bool IsMachineIdLoadingError { get; private set; }
    
    public string Build()
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction
        
        var ep = "gEfeWW9uWnyl6RCA";
        ep += '\ud801';
        ep += '\udcfb';
        ep += '\ud83d';
        ep += '\udc01';
        ep += "vD7mUWapOH_".Substring(2);
        ep += '\ud801';
        ep += '\udccf';
        ep += '\ud801';
        ep += '\udcd8';
        ep += '\ud83d';
        ep += '\udc02';
        ep += '\u20AC';

        string? additonalData;
        try
        {
            additonalData = LoadOrInitializeEnvironmentEncryptionKey();
            
            IsEnvironmentEncryptionKeySuccess = !additonalData.IsNullOrEmpty();
        }
        catch (Exception ex)
        {
            IsEnvironmentEncryptionKeyError = true;
            
            Log.Error(ex, "LoadOrInitializeEnvironmentEncryptionKey");
            additonalData = null;
        }
        
        if (additonalData.IsNullOrEmpty())
        {
            try
            {
                additonalData = GetMachineId();

                IsMachineIdLoadingSuccess = !additonalData.IsNullOrEmpty();
            }
            catch (Exception ex)
            {
                IsMachineIdLoadingError = true;

                Log.Error(ex, "LoadOrInitializeEnvironmentEncryptionKey");
                additonalData = null;
            }
        }

        if (additonalData.IsNullOrEmpty())
        {
            additonalData = "9Dksi78iSOçf0lsei";
        }

        // 07/12/2022
        // Voici ce qui est retenu pour la version 2022.3.
        // Le but du EncryptionPassword est de chiffrer les clés RSA pour rendre difficile leur accès sur la machine, et encore plus difficile (voir 
        //      très difficile depuis une autre machine), sans gêner l'utilisateur de la machine. Par exemple, on n'intègre plus le machineName 
        //      pour éviter qu'un renommage de la machine n'invalide les paramètres
        // Le EncryptionPassword est une chaîne composée de :
        //  - Une partie en dur
        //  - Une partie additionnelle, propre à la machine, et censée être stable
        //      Si possible : Variable d'environnement. Or les variables d'environnement sont compliquées à définir depuis le code sur Linux/MacOS
        //      Sinon : Machine Id
        //
        //
        // Pourquoi pas le MachineName ?
        //  - Avantage : si on déplace le fichier sur un autre machine (ayant un autre nom), le fichier n'est pas lisible
        //  - Inconvénient : Pas assez protecteur, il suffit de connaître le nom de la machine et de décompiler le programme pour reconstruire l'EncryptionKey
        //  - Inconvénient : Gênant, en cas de renommage de la machine, les clés sont perdues
        //
        // Pourquoi pas un PassPhrase ?
        //  - Avantage : très protecteur
        //  - Inconvénient : Gênant, il faut le saisir à chaque lancement (donc pas de lancement non surveillé)

        var pre = ep + additonalData;

        var final = Caesar(pre, 2020);

        return final;
    }

    private string? LoadOrInitializeEnvironmentEncryptionKey()
    {
        string? environnementEncryptionKey;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            environnementEncryptionKey = Environment.GetEnvironmentVariable("BYTESYNC_SYMMETRIC_KEY", EnvironmentVariableTarget.User);

            if (environnementEncryptionKey.IsNullOrEmpty())
            {
                HasTriedToGenerateEnvironmentEncryptionKey = true;
                
                var guid = Guid.NewGuid().ToString("D");

                Environment.SetEnvironmentVariable("BYTESYNC_SYMMETRIC_KEY", guid, EnvironmentVariableTarget.User);

                environnementEncryptionKey = Environment.GetEnvironmentVariable("BYTESYNC_SYMMETRIC_KEY", EnvironmentVariableTarget.User);

                if (!Equals(environnementEncryptionKey, guid))
                {
                    environnementEncryptionKey = null;
                }
            }
        }
        else
        {
            environnementEncryptionKey = Environment.GetEnvironmentVariable("BYTESYNC_SYMMETRIC_KEY");
        }

        return environnementEncryptionKey;
    }
    
    private string? GetMachineId()
    {
        HasTriedToLoadMachineId = true;
        
        string? machineId = null;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // https://stackoverflow.com/questions/6304275/c-sharp-reading-the-registry-productid-returns-null-in-x86-targeted-app-any-c
            
            var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var windowsNtKey = localMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            var productId = windowsNtKey?.GetValue("ProductId");

            machineId = productId as string;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // https://unix.stackexchange.com/questions/395331/is-machine-id-a-uuid
            
            Action<string, string> handler = (standardOutput, _) =>
            {
                if (standardOutput.IsNotEmpty())
                {
                    machineId = standardOutput;
                }
            };
            var commandRunner = new CommandRunner { OutputHandler = handler, NeedBash = true, LogLevel = CommandRunner.LogLevels.None};
            commandRunner.RunCommand("cat", "/etc/machine-id");  
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // https://stackoverflow.com/questions/48116377/how-to-get-a-unique-id-of-a-mac-machine-in-2018
            
            Action<string, string> handler = (standardOutput, _) =>
            {
                if (standardOutput.IsNotEmpty())
                {
                    machineId = standardOutput;
                }
            };
            var commandRunner = new CommandRunner { OutputHandler = handler, NeedBash = true, LogLevel = CommandRunner.LogLevels.None};
            commandRunner.RunCommand("system_profiler", "SPHardwareDataType | awk '/UUID/ {print $3}'"); 
        }
        
        return machineId;
    }

    private static string Caesar(string source, Int16 shift)
    {
        // https://stackoverflow.com/questions/13025949/simple-obfuscation-of-string-in-net
        
        var maxChar = Convert.ToInt32(char.MaxValue);
        var minChar = Convert.ToInt32(char.MinValue);

        var buffer = source.ToCharArray();

        for (var i = 0; i < buffer.Length; i++)
        {
            var shifted = Convert.ToInt32(buffer[i]) + shift;

            if (shifted > maxChar)
            {
                shifted -= maxChar;
            }
            else if (shifted < minChar)
            {
                shifted += maxChar;
            }

            buffer[i] = Convert.ToChar(shifted);
        }

        return new string(buffer);
    }

}