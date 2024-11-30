using System;
using System.IO;

namespace ByteSync.Common.Helpers;

public static class IOUtils
{
    public static string ExtractRelativePath(string fileSystemInfoFullName, string baseDirectoryFullName)
    {
        string result = fileSystemInfoFullName.Substring(baseDirectoryFullName.Length);

        return result;
    }

    public static string Combine(this DirectoryInfo directoryInfo, params string[] pathParts)
    {
        return Combine(directoryInfo.FullName, pathParts);
    }

    /// <summary>
    /// Encapsule Combine en l'améliorant. Gestion du \ en début de path2 qui peut bloquer le processus dans Path.Combine
    /// </summary>
    /// <param name="startPath"></param>
    /// <param name="pathParts"></param>
    /// <returns></returns>
    public static string Combine(string startPath, params string[] pathParts)
    {
        string finalPath;
        if (Path.AltDirectorySeparatorChar == '/')
        {
            finalPath = startPath.Replace('\\', Path.DirectorySeparatorChar);
        }
        else if (Path.AltDirectorySeparatorChar == '\\')
        {
            finalPath = startPath.Replace('/', Path.AltDirectorySeparatorChar);
        }
        else
        {
            finalPath = startPath;
        }

        foreach (var pathPart in pathParts)
        {
            string preparedPart = pathPart
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
                
            finalPath = Path.Combine(finalPath, preparedPart);
        }

        return finalPath;
    }

    public static bool HasAncestor(this FileInfo fileInfo, Predicate<DirectoryInfo> predicate)
    {
        return FirstAncestor(fileInfo, predicate) != null;
    }
        
    public static bool HasAncestor(this DirectoryInfo directory, Predicate<DirectoryInfo> predicate)
    {
        return FirstAncestor(directory, predicate) != null;
    }
        
    public static DirectoryInfo FirstAncestor(this FileInfo fileInfo, Predicate<DirectoryInfo> predicate)
    {
        if (fileInfo.Directory == null)
        {
            return null;
        }
            
        if (predicate(fileInfo.Directory))
        {
            return fileInfo.Directory;
        }
        else
        {
            return FirstAncestor(fileInfo.Directory, predicate);
        }
    }
        
    public static DirectoryInfo FirstAncestor(this DirectoryInfo directory, Predicate<DirectoryInfo> predicate)
    {
        if (directory.Parent == null)
        {
            return null;
        }
            
        if (predicate(directory.Parent))
        {
            return directory.Parent;
        }
        else
        {
            return FirstAncestor(directory.Parent, predicate);
        }
    }
        
    /// <summary>
    /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
    /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
    /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
    /// https://stackoverflow.com/questions/5617320/given-full-path-check-if-path-is-subdirectory-of-some-other-path-or-otherwise
    /// </summary>
    public static bool IsSubPathOf(string path, string baseDirPath)
    {
        string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
            .WithEnding("\\"));

        string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
            .WithEnding("\\"));

        return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
    /// results in satisfying .EndsWith(ending).
    /// </summary>
    /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
    public static string WithEnding(this string? str, string ending)
    {
        if (str == null)
            return ending;

        string result = str;

        // Right() is 1-indexed, so include these cases
        // * Append no characters
        // * Append up to N characters, where N is ending length
        for (int i = 0; i <= ending.Length; i++)
        {
            string tmp = result + ending.Right(i);
            if (tmp.EndsWith(ending))
                return tmp;
        }

        return result;
    }
        
    /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
    /// <param name="value">The string to retrieve the substring from.</param>
    /// <param name="length">The number of characters to retrieve.</param>
    /// <returns>The substring.</returns>
    public static string Right(this string value, int length)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
        }

        return (length < value.Length) ? value.Substring(value.Length - length) : value;
    }
        
    /// <summary>
    /// https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}