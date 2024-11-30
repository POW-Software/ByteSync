using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ByteSync.Common.Helpers;

public static class CollectionUtils
{
    public static void AddAll<T>(this ICollection<T> collection, IEnumerable<T> elementsToAdd)
    {
        foreach (T element in elementsToAdd)
        {
            collection.Add(element);
        }
    }

    public static HashSet<T> ToHashSet<T>(this ICollection<T> collection)
    {
        return new HashSet<T>(collection);
    }

    public static HashSet<T> Minus<T>(this HashSet<T> collection, ICollection<T> elementsToRemove)
    {
        var result = new HashSet<T>(collection);
        result.RemoveWhere(elementsToRemove.Contains);

        return result;
    }

    public static void RemoveAll<T>(this HashSet<T> collection, ICollection<T> elementsToRemove)
    {
        collection.RemoveWhere(elementsToRemove.Contains);
    }

    public static List<T> ToList<T>(this IList list)
    {
        List<T> result = new List<T>();

        foreach (var element in list)
        {
            result.Add((T)element);
        }

        return result;
    }

    public static List<T> ToList<T>(this T[] list)
    {
        List<T> result = new List<T>();

        foreach (var element in list)
        {
            result.Add((T)element);
        }

        return result;
    }
        
    public static bool ContainsAll<T>(this ICollection<T> container, ICollection<T> otherCollection)
    {
        if (otherCollection.Count == 0)
        {
            return false;
        }

        foreach (var element in otherCollection)
        {
            if (!container.Contains(element))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Indique si deux listes ont le meme contenu
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list1"></param>
    /// <param name="list2"></param>
    /// <returns></returns>
    public static bool HaveSameContent<T>(this IList<T> list1, IList<T> list2)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }

        for (int i = 0; i < list1.Count; i++)
        {
            if (! Object.Equals(list1[i], list2[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Indique si deux listes ont le mêmes éléments
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list1"></param>
    /// <param name="list2"></param>
    /// <returns></returns>
    public static bool HaveSameElements<T>(this ICollection<T> list1, ICollection<T> list2)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }

        foreach (T element in list1)
        {
            if (! list2.Contains(element))
            {
                return false;
            }
        }

        foreach (T element in list2)
        {
            if (!list1.Contains(element))
            {
                return false;
            }
        }

        return true;
    }
        
    /// <summary>
    /// https://stackoverflow.com/questions/5118513/removeall-for-observablecollections
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="T"></typeparam>
    public static void RemoveAll<T>(this ObservableCollection<T> collection,
        Func<T, bool> condition)
    {
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (condition(collection[i]))
            {
                collection.RemoveAt(i);
            }
        }
    }
}