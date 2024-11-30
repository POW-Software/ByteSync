using System.Runtime.CompilerServices;
using Serilog;

namespace ByteSync.Common.Controls.Serilog;

public static class LoggerExtensions
{
    // https://stackoverflow.com/questions/29470863/serilog-output-enrich-all-messages-with-methodname-from-which-log-entry-was-ca
    
    public static ILogger Here(this ILogger logger,
        [CallerMemberName] string memberName = "")
    {
    #if DEBUG
        return logger
            .ForContext("MemberName", memberName);
    #else
        return logger;
    #endif
    }
    
    // public static ILogger Here(this ILogger logger,
    //     [CallerMemberName] string memberName = "",
    //     [CallerFilePath] string sourceFilePath = "",
    //     [CallerLineNumber] int sourceLineNumber = 0) {
    //     return logger
    //         .ForContext("MemberName", memberName)
    //         .ForContext("FilePath", sourceFilePath)
    //         .ForContext("LineNumber", sourceLineNumber);
    // }
}