using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace ByteSync.Services.Synchronizations;

public static class LocalPathSafety
{
    public static StringComparison Comparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static void ValidateComponent(string? value)
    {
        ValidateRelativePath(value);
        if (string.IsNullOrEmpty(value) || value.Contains('/')) throw new UnauthorizedAccessException("Invalid storage component");
    }

    public static void ValidateRelativePath(string? path)
    {
        if (path == null || path.Contains('\\') || path.Contains(':') || path.Contains('\0') || path.StartsWith('/') ||
            Path.IsPathRooted(path)) throw new UnauthorizedAccessException("Invalid relative path");
        if (path == "") return;
        foreach (var component in path.Split('/'))
        {
            if (component is "" or "." or ".." || component.Any(char.IsControl)) throw new UnauthorizedAccessException("Ambiguous relative path");
            if (OperatingSystem.IsWindows())
            {
                var basename = component.Split('.')[0];
                if (component.EndsWith(' ') || component.EndsWith('.') || component.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(basename, StringComparer.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException("Ambiguous Windows path");
            }
        }
    }

    public static void AssertNoLinks(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)!;
        if (OperatingSystem.IsWindows() && root.StartsWith(@"\\")) throw new UnauthorizedAccessException("UNC sources are not supported");
        var current = root;
        foreach (var component in fullPath[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            try
            {
                var attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new UnauthorizedAccessException("Links and reparse points are not allowed");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
    public static IDisposable Acquire(params string[] paths) => AcquireDirectories(1, paths);

    private static IDisposable AcquireDirectories(uint sharing, params string[] paths)
    {
        var handles = new List<SafeFileHandle>();
        try
        {
            foreach (var path in paths.Distinct())
            {
                AssertNoLinks(path);
                if (!OperatingSystem.IsWindows()) continue;
                var full = Path.GetFullPath(path);
                var current = Path.GetPathRoot(full)!;
                foreach (var component in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, component);
                    if (current == full || !Directory.Exists(current)) break;
                    var handle = OpenWindows(current, 0x80000000, sharing, 0x02200000);
                    handles.Add(handle);
                }
            }
            return new DirectoryLease(handles);
        }
        catch
        {
            foreach (var handle in handles) handle.Dispose();
            throw;
        }
    }

    public static FileStream OpenRead(string path)
    {
        using var lease = Acquire(path);
        if (!OperatingSystem.IsWindows()) return OpenUnixRead(path);
        var handle = OpenWindows(path, 0x80000000, 1, 0x00200000);
        return new FileStream(handle, FileAccess.Read);
    }

    private static FileStream OpenUnixRead(string path)
    {
        var full = Path.GetFullPath(path);
        var noFollow = OperatingSystem.IsMacOS() ? 0x100 : 0x20000;
        var directory = OperatingSystem.IsMacOS() ? 0x100000 : 0x10000;
        var fd = OpenAt(OperatingSystem.IsMacOS() ? -2 : -100, "/", directory | noFollow, 0);
        if (fd < 0) throw new IOException("Cannot open root", new Win32Exception(Marshal.GetLastWin32Error()));
        var handle = new SafeFileHandle((IntPtr)fd, true);
        try
        {
            var components = full.TrimStart('/').Split('/');
            for (var i = 0; i < components.Length; i++)
            {
                var next = OpenAt(handle.DangerousGetHandle().ToInt32(), components[i], (OperatingSystem.IsMacOS() ? 4 : 0x800) | noFollow | (i < components.Length - 1 ? directory : 0), 0);
                if (next < 0) throw new IOException("Cannot open file without following links", new Win32Exception(Marshal.GetLastWin32Error()));
                handle.Dispose();
                handle = new SafeFileHandle((IntPtr)next, true);
            }
            AssertUnixRegularFile(handle);
            return new FileStream(handle, FileAccess.Read);
        }
        catch { handle.Dispose(); throw; }
    }


    public static FileStream OpenWrite(string path, bool createNew = false, bool append = false, bool readable = false)
    {
        using var lease = Acquire(path);
        SafeFileHandle handle;
        if (OperatingSystem.IsWindows())
            handle = OpenWindows(path, readable ? 0xC0000080u : 0x40000080u, 1, 0x00200000, createNew ? 1u : 4u);
        else
        {
            using var parent = OpenUnixParent(path, out var name);
            var flags = (readable ? 2 : 1) | (OperatingSystem.IsMacOS() ? 4 : 0x800) | (OperatingSystem.IsMacOS() ? 0x200 : 0x40) | (OperatingSystem.IsMacOS() ? 0x100 : 0x20000);
            if (createNew) flags |= OperatingSystem.IsMacOS() ? 0x800 : 0x80;
            var fd = OpenAt(parent.DangerousGetHandle().ToInt32(), name, flags, 0x180);
            if (fd < 0) throw new IOException("Cannot open destination", new Win32Exception(Marshal.GetLastWin32Error()));
            handle = new SafeFileHandle((IntPtr)fd, true);
        }
        AssertUnixRegularFile(handle);
        var stream = new FileStream(handle, readable ? FileAccess.ReadWrite : FileAccess.Write);
        if (append) stream.Seek(0, SeekOrigin.End);
        else stream.SetLength(0);
        return stream;
    }

    public static void CreateDirectory(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            using var directory = OpenUnixDirectory(path, true);
            return;
        }
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        var handles = new List<SafeFileHandle>();
        try
        {
            foreach (var component in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, component);
                Directory.CreateDirectory(current);
                handles.Add(OpenWindows(current, 0x80000000, 1, 0x02200000));
            }
        }
        finally { foreach (var handle in handles) handle.Dispose(); }
    }

    public static void Move(string source, string destination, bool overwrite = false)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!string.Equals(Path.GetDirectoryName(source), Path.GetDirectoryName(destination), StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Replacement must stay in the same directory");
            using var initialLease = Acquire(source, destination);
            using var file = OpenWindows(source, 0x80010080, 1, 0x00200000);
            using var renameLease = AcquireDirectories(3, source, destination);
            using var parent = OpenWindows(Path.GetDirectoryName(destination)!, 0x80000000, 3, 0x02200000);
            initialLease.Dispose();
            var name = Encoding.Unicode.GetBytes(Path.GetFileName(destination));
            var handleOffset = IntPtr.Size == 8 ? 8 : 4;
            var lengthOffset = handleOffset + IntPtr.Size;
            var nameOffset = lengthOffset + 4;
            var buffer = Marshal.AllocHGlobal(nameOffset + name.Length);
            try
            {
                for (var i = 0; i < nameOffset; i++) Marshal.WriteByte(buffer, i, 0);
                Marshal.WriteByte(buffer, 0, overwrite ? (byte)1 : (byte)0);
                Marshal.WriteIntPtr(buffer, handleOffset, parent.DangerousGetHandle());
                Marshal.WriteInt32(buffer, lengthOffset, name.Length);
                Marshal.Copy(name, 0, IntPtr.Add(buffer, nameOffset), name.Length);
                var status = NtSetInformationFile(file, out _, buffer, (uint)(nameOffset + name.Length), 10);
                if (status != 0) throw new IOException("Cannot replace destination", new Win32Exception((int)RtlNtStatusToDosError(status)));
            }
            finally { Marshal.FreeHGlobal(buffer); }
            return;
        }
        using var sourceParent = OpenUnixParent(source, out var sourceName);
        using var targetParent = OpenUnixParent(destination, out var targetName);
        var result = OperatingSystem.IsMacOS()
            ? RenameAtMac(sourceParent.DangerousGetHandle().ToInt32(), sourceName, targetParent.DangerousGetHandle().ToInt32(), targetName, overwrite ? 0u : 4u)
            : RenameAtLinux(sourceParent.DangerousGetHandle().ToInt32(), sourceName, targetParent.DangerousGetHandle().ToInt32(), targetName, overwrite ? 0u : 1u);
        if (result != 0)
            throw new IOException("Cannot replace destination", new Win32Exception(Marshal.GetLastWin32Error()));
    }

    public static void Delete(string path, bool directory = false)
    {
        using var lease = Acquire(path);
        if (OperatingSystem.IsWindows())
        {
            if (directory) Directory.Delete(path, false); else File.Delete(path);
            return;
        }
        using var parent = OpenUnixParent(path, out var name);
        if (UnlinkAt(parent.DangerousGetHandle().ToInt32(), name, directory ? (OperatingSystem.IsMacOS() ? 0x80 : 0x200) : 0) != 0 &&
            Marshal.GetLastWin32Error() != 2) throw new IOException("Cannot delete destination", new Win32Exception(Marshal.GetLastWin32Error()));
    }

    public static void SetDates(string path, DateTime creation, DateTime write)
    {
        using var lease = Acquire(path);
        if (OperatingSystem.IsWindows())
        {
            using var handle = OpenWindows(path, 0x80000100, 1, 0x00200000);
            File.SetCreationTimeUtc(handle, creation);
            File.SetLastWriteTimeUtc(handle, write);
        }
        else
        {
            using var stream = OpenUnixRead(path);
            File.SetCreationTimeUtc(stream.SafeFileHandle, creation);
            File.SetLastWriteTimeUtc(stream.SafeFileHandle, write);
        }
    }

    private static SafeFileHandle OpenUnixParent(string path, out string name)
    {
        var full = Path.GetFullPath(path);
        name = Path.GetFileName(full);
        if (name.Length == 0) throw new UnauthorizedAccessException("Cannot operate on a filesystem root");
        return OpenUnixDirectory(Path.GetDirectoryName(full)!, false);
    }

    private static SafeFileHandle OpenUnixDirectory(string path, bool create)
    {
        var flags = OperatingSystem.IsMacOS() ? 0x100100 : 0x30000;
        var fd = OpenAt(OperatingSystem.IsMacOS() ? -2 : -100, "/", flags, 0);
        if (fd < 0) throw new IOException("Cannot open directory root");
        var handle = new SafeFileHandle((IntPtr)fd, true);
        try
        {
            foreach (var component in Path.GetFullPath(path).Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (create && MkdirAt(handle.DangerousGetHandle().ToInt32(), component, 0x1C0) != 0 && Marshal.GetLastWin32Error() != 17)
                    throw new IOException("Cannot create directory", new Win32Exception(Marshal.GetLastWin32Error()));
                var next = OpenAt(handle.DangerousGetHandle().ToInt32(), component, flags, 0);
                if (next < 0) throw new IOException("Cannot access directory without following links", new Win32Exception(Marshal.GetLastWin32Error()));
                handle.Dispose();
                handle = new SafeFileHandle((IntPtr)next, true);
            }
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "mkdirat")]
    private static extern int MkdirAt(int directory, string path, uint mode);
    [DllImport("libc", SetLastError = true, EntryPoint = "renameat2")]
    private static extern int RenameAtLinux(int sourceDirectory, string source, int targetDirectory, string target, uint flags);
    [DllImport("libc", SetLastError = true, EntryPoint = "renameatx_np")]
    private static extern int RenameAtMac(int sourceDirectory, string source, int targetDirectory, string target, uint flags);
    [DllImport("libc", SetLastError = true, EntryPoint = "unlinkat")]
    private static extern int UnlinkAt(int directory, string path, int flags);

    private static SafeFileHandle OpenWindows(string path, uint access, uint share, uint flags, uint disposition = 3)
    {
        var handle = CreateFile(path, access, share, IntPtr.Zero, disposition, flags, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new IOException("Cannot acquire path access", new Win32Exception(Marshal.GetLastWin32Error())); }
        if (!GetFileInformationByHandleEx(handle, 9, out var info, 8) || (info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            handle.Dispose();
            throw new UnauthorizedAccessException("Links and reparse points are not allowed");
        }
        if ((info.Attributes & FileAttributes.Directory) == 0 &&
            (!GetFileInformationByHandle(handle, out var fileInfo) || fileInfo.LinkCount != 1))
        {
            handle.Dispose();
            throw new UnauthorizedAccessException("Hard-linked files are not allowed");
        }
        return handle;
    }

    private static void AssertUnixRegularFile(SafeFileHandle handle)
    {
        if (OperatingSystem.IsWindows()) return;
        UnixFileInformation info;
        var result = OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? FStatMac64(handle.DangerousGetHandle().ToInt32(), out info)
            : FStat(handle.DangerousGetHandle().ToInt32(), out info);
        if (result != 0)
        {
            handle.Dispose();
            throw new IOException("Cannot inspect opened file");
        }
        var mac = OperatingSystem.IsMacOS();
        var arm = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        if (!mac && RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64))
        {
            handle.Dispose();
            throw new PlatformNotSupportedException("Unsupported filesystem ABI");
        }
        var links = mac ? info.MacLinks : arm ? info.LinuxArmLinks : info.LinuxX64Links;
        var mode = mac ? info.MacMode : arm ? info.LinuxArmMode : info.LinuxX64Mode;
        if (links != 1 || (mode & 0xF000) != 0x8000)
        {
            handle.Dispose();
            throw new UnauthorizedAccessException("Only regular files with one link are allowed");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct UnixFileInformation
    {
        [FieldOffset(16)] public ulong LinuxX64Links;
        [FieldOffset(24)] public uint LinuxX64Mode;
        [FieldOffset(20)] public uint LinuxArmLinks;
        [FieldOffset(16)] public uint LinuxArmMode;
        [FieldOffset(6)] public ushort MacLinks;
        [FieldOffset(4)] public ushort MacMode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 52)]
    private struct WindowsFileInformation
    {
        [FieldOffset(40)] public uint LinkCount;
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "fstat")]
    private static extern int FStat(int descriptor, out UnixFileInformation information);
    [DllImport("libc", SetLastError = true, EntryPoint = "fstat$INODE64")]
    private static extern int FStatMac64(int descriptor, out UnixFileInformation information);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out WindowsFileInformation information);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock { public IntPtr Status; public UIntPtr Information; }

    [DllImport("ntdll.dll")]
    private static extern int NtSetInformationFile(SafeFileHandle handle, out IoStatusBlock status, IntPtr information, uint length, int kind);

    [DllImport("ntdll.dll")]
    private static extern uint RtlNtStatusToDosError(int status);

    private sealed class DirectoryLease(List<SafeFileHandle> handles) : IDisposable
    {
        public void Dispose() { foreach (var handle in handles) handle.Dispose(); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AttributeTagInfo { public FileAttributes Attributes; public uint Tag; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int kind, out AttributeTagInfo info, uint size);

    [DllImport("libc", SetLastError = true, EntryPoint = "openat")]
    private static extern int OpenAt(int directory, string path, int flags, uint mode);

}
