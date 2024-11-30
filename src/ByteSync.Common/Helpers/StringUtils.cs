using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ByteSync.Common.Helpers;

public static class StringUtils
{
    /// <summary>
    /// Indique si une chaine est vide
    /// </summary>
    /// <param name="str">chaine à analyser</param>
    /// <param name="trimBefore">Indique si la chaine est trimmée avant d'etre analysée</param>
    /// <returns></returns>
    public static bool IsEmpty(this string str, bool trimBefore = false)
    {
        if (trimBefore)
        {
            return str.Trim().Equals("");
        }
        else
        {
            return str.Equals("");
        }
    }

    /// <summary>
    /// Indique si une chaine est vide
    /// </summary>
    /// <param name="str">chaine à analyser</param>
    /// <param name="trimBefore">Indique si la chaine est trimmée avant d'etre analysée</param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(this string? str, bool trimBefore = false)
    {
        if (str == null)
        {
            return true;
        }
        else
        {
            return IsEmpty(str, trimBefore);
        }
    }

    /// <summary>
    /// Indique si une chaine n'est ni null, ni vide
    /// </summary>
    /// <param name="str">chaine à analyser</param>
    /// <param name="trimBefore">Indique si la chaine est trimmée avant d'etre analysée</param>
    /// <returns></returns>
    public static bool IsNotEmpty(this string? str, bool trimBefore = false)
    {
        return !IsNullOrEmpty(str, trimBefore);
    }

    public static string[] Split(this string str, char separator, StringSplitOptions options)
    {
        return str.Split(new char[] {separator}, options);
    }

    public static string[] Split(this string str, string separator, StringSplitOptions options)
    {
        return str.Split(new string[] {separator}, options);
    }

    public static string JoinToString(this ICollection<string> collection, string separator)
    {
        return JoinToString(collection, separator, null, null);
    }

    public static string JoinToString(this ICollection<string> collection, string separator, Func<string, string>? actionOnElements)
    {
        return JoinToString(collection, separator, null, actionOnElements);
    }

    public static string JoinToString(this ICollection<string>? collection,
        string separator, string? lastSeparator,
        Func<string, string>? actionOnElements)
    {
        if (collection == null)
        {
            return "";
        }
        if (collection.Count == 0)
        {
            return "";
        }

        List<string> collectionToJoin;
        if (actionOnElements == null)
        {
            collectionToJoin = new List<string>(collection);
        }
        else
        {
            collectionToJoin = new List<string>();
            foreach (string element in collection)
            {
                var modifiedElement = actionOnElements.Invoke(element);
                collectionToJoin.Add(modifiedElement);
            }
        }

        string result;
        if (lastSeparator == null || collection.Count == 1)
        {
            result = string.Join(separator, collectionToJoin);
        }
        else
        {
            result = string.Join(separator, collectionToJoin, 0, collectionToJoin.Count - 1) + lastSeparator + collectionToJoin.LastOrDefault();
        }

        return result;
    }

    public static string UppercaseFirst(this string s)
    {
        // Check for empty string.
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }
        // Return char and concat substring.
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    public static string LowercaseFirst(this string s)
    {
        // Check for empty string.
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }
        // Return char and concat substring.
        return char.ToLower(s[0]) + s.Substring(1);
    }
        
    // https://stackoverflow.com/questions/6724840/how-can-i-truncate-my-strings-with-a-if-they-are-too-long
    public static string Truncate(this string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
    }
        
    // https://stackoverflow.com/questions/24769701/trim-whitespace-from-the-end-of-a-stringbuilder-without-calling-tostring-trim
    public static StringBuilder TrimEnd(this StringBuilder sb)
    {
        if (sb == null || sb.Length == 0) return sb;

        int i = sb.Length - 1;

        for (; i >= 0; i--)
            if (!char.IsWhiteSpace(sb[i]))
                break;

        if (i < sb.Length - 1)
            sb.Length = i + 1;

        return sb;
    }

    public static List<string> GetLines(this string input)
    {
        var lines = input.Split(new string[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var result = lines.ToList();

        return result;
    }
        
    // https://stackoverflow.com/questions/9484509/string-insert-multiple-values-is-this-possible
    public static string MultiInsert(this string str, string insertChar, params int[] positions)
    {
        StringBuilder sb = new StringBuilder(str.Length + (positions.Length*insertChar.Length));
        var posLookup = new HashSet<int>(positions);
        for(int i=0;i<str.Length;i++)
        {
            sb.Append(str[i]);
            if(posLookup.Contains(i))
                sb.Append(insertChar);

        }
        return sb.ToString();
    }
}