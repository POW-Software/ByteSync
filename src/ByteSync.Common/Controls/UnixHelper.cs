using System;
using System.Runtime.InteropServices;
using ByteSync.Common.Helpers;

namespace ByteSync.Common.Controls;

public static class UnixHelper
{
    // https://stackoverflow.com/questions/592620/how-can-i-check-if-a-program-exists-from-a-bash-script
    public static bool CommandExists(string commandName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }
        
        bool? result = null;
        Action<string, string> handler = (standardOutput, standardError) =>
        {
            // Si la commande est trouvée, renvoie quelque chose de la forme :
            // /usr/bin/commandName
            
            result = standardOutput.IsNotEmpty() 
                     && standardError.IsEmpty() 
                     && standardOutput.GetLines().Count == 1
                     && standardOutput.Trim().EndsWith(commandName, StringComparison.InvariantCultureIgnoreCase);
        };

        CommandRunner commandRunner = new CommandRunner { OutputHandler = handler, NeedBash = true, LogLevel = CommandRunner.LogLevels.None};
        commandRunner.RunCommand("command", $"-v {commandName}");  
        
        // CommandRunner.RunCommand("command", $"-v {commandName}", null, handler, null, false, true);

        return result!.Value;
    }
}