// using System.Diagnostics;
// using Serilog;
//
// namespace PowCommon.Controls
// {
//     public class LinuxCommandRunner
//     {
//         public void Exec(string cmd)
//         {
//             // https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
//                 
//             // https://www.howtoforge.com/tutorial/linux-chmod-command/
//             
//             var escapedArgs = cmd.Replace("\"", "\\\"");
//         
//             using var process = new Process
//             {
//                 StartInfo = new ProcessStartInfo
//                 {
//                     RedirectStandardOutput = true,
//                     UseShellExecute = false,
//                     CreateNoWindow = true,
//                     WindowStyle = ProcessWindowStyle.Hidden,
//                     FileName = "/bin/bash",
//                     Arguments = $"-c \"{escapedArgs}\""
//                 }
//             };
//
//             process.Start();
//             process.WaitForExit();
//
//             int exitCode = process.ExitCode;
//             Log.Information("UnixCommandRunner: ExitCode {exitCode}", exitCode);
//         }
//     }
// }