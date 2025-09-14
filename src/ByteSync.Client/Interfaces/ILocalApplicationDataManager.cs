namespace ByteSync.Interfaces;

public interface ILocalApplicationDataManager
{
    string ApplicationDataPath { get; }

    string? LogFilePath { get; }

    string? DebugLogFilePath { get; }

    string ShellApplicationDataPath { get; }

    string GetShellPath(string path);
}