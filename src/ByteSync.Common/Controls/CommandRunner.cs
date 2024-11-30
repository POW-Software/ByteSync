using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ByteSync.Common.Helpers;
using Serilog;

namespace ByteSync.Common.Controls;

public class CommandRunner
{
    public enum LogLevels
    {
        All, Protected, Minimal, None
    }
    
    public CommandRunner()
    {
        WorkingDirectory = null;
        OutputHandler = null;
        NeedBash = false;
        LogLevel = LogLevels.All;
    }
    
    public string? WorkingDirectory { get; set; }
    
    public Action<string, string>? OutputHandler { get; set; }
    
    public bool NeedBash { get; set; }
    
    public LogLevels LogLevel { get; set; }
    
    public static Task<int> RunCommandAsync(string command, string arguments, 
        int? expectedCommandResult = null, bool needBash = false, [CallerMemberName] string? callerMemberName = null)
    {
        return Task.Run(() => RunCommand(command, arguments, expectedCommandResult, needBash, callerMemberName));
    }
    
    // public static Task<int> RunCommandAsync(string command, string arguments, string? workingDirectory = null,
    //     Action<string, string>? outputHandler = null, int? expectedCommandResult = null,
    //     bool protectLog = false, bool needBash = false, [CallerMemberName] string? callerMemberName = null)
    // {
    //     return Task.Run(() => RunCommand(command, arguments, workingDirectory, 
    //         outputHandler, expectedCommandResult, protectLog, needBash, callerMemberName));
    // }

    public static int RunCommand(string command, string arguments, int? expectedCommandResult = null, 
        bool needBash = false, [CallerMemberName] string? callerMemberName = null)
    {
        CommandRunner commandRunner = new CommandRunner();
        commandRunner.NeedBash = needBash;

        return commandRunner.RunCommand(command, arguments, expectedCommandResult, callerMemberName);
    }

    public Task<int> RunCommandAsync(string command, string arguments, int? expectedCommandResult = null,
        [CallerMemberName] string? callerMemberName = null)
    {
        return Task.Run(() => RunCommand(command, arguments, expectedCommandResult, callerMemberName));
    }

    public int RunCommand(string command, string arguments, int? expectedCommandResult = null, 
        [CallerMemberName] string? callerMemberName = null)
    {
        ProcessStartInfo procStartInfo = new ProcessStartInfo();

        if (NeedBash)
        {
            procStartInfo.FileName = "/bin/bash";

            string preparedArgs = command + " " + arguments;
            preparedArgs = preparedArgs.Replace("\"", "\\\"");

            procStartInfo.Arguments = $"-c \"{preparedArgs}\"";
        }
        else
        {
            procStartInfo.FileName = command;
            procStartInfo.Arguments = arguments;
        }
        

        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = false;
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.RedirectStandardError = true;

        if (WorkingDirectory != null)
        {
            procStartInfo.WorkingDirectory = WorkingDirectory;
        }

        if (LogLevel.In(LogLevels.Minimal, LogLevels.All))
        {
            Log.Information("Running command {command} with arguments {arguments}", 
                command, arguments);
        }
        else if (LogLevel.In(LogLevels.Protected))
        {
            Log.Information("Running command {command} with arguments ***PROTECTED***", 
                command);
        }
        
        
        Process process = new Process();
        process.StartInfo = procStartInfo;
        process.EnableRaisingEvents = true;
        process.Start();
        
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        bool hasExited = process.WaitForExit(120*1000);
        var exitCode = process.ExitCode;

        if (LogLevel.In(LogLevels.All, LogLevels.Protected))
        {
            if (standardOutput.IsNotEmpty(true))
            {
                Log.Information("{Caller}.{Command} - Standard output: {output}", callerMemberName, command, Environment.NewLine + standardOutput);
            }
            else
            {
                Log.Information("{Caller}.{Command} - Standard output empty", callerMemberName, command);
            }

            if (standardError.IsNotEmpty(true))
            {
                Log.Error("{Caller}.{Command} - Error output: {output}", callerMemberName, command, Environment.NewLine + standardError);
            }
            else
            {
                Log.Information("{Caller}.{Command} - Error output empty", callerMemberName, command);
            }
        }

        if (!hasExited)
        {
            throw new Exception("process has not exited after 120 seconds");
        }

        if (LogLevel.In(LogLevels.All, LogLevels.Protected))
        {
            Log.Information("{Caller}.{Command} - ExitCode: {code}", callerMemberName, command, exitCode);
        }

        if (OutputHandler != null)
        {
            OutputHandler.Invoke(standardOutput, standardError);
        }

        if (expectedCommandResult != null)
        {
            if (expectedCommandResult != exitCode)
            {
                throw new Exception("Unexpected command result. ExpectedCommandResult: " + expectedCommandResult + ". exitCode: " + exitCode);
            }
        }
        
        return exitCode;
    }
}