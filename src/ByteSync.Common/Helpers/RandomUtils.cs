using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ByteSync.Common.Helpers;

public static class RandomUtils
{
    
    /// <summary>
    /// Génère une chaîne de lettres alétoires.
    /// </summary>
    /// <param name="upperCase">True : majuscule, False : minuscule, null : majuscule ou minuscule</param>
    /// <returns></returns>
    public static string GetRandomLetters(int count, bool? uppercase)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < count; i++)
        {
            var c = GetRandomLetter(uppercase);
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Génère une lettre aléatoirement
    /// </summary>
    /// <param name="upperCase">True : majuscule, False : minuscule, null : majuscule ou minuscule</param>
    /// <returns></returns>
    public static char GetRandomLetter(bool? upperCase)
    {
        if (upperCase == null)
        {
            if (RandomNumberGenerator.GetInt32(2) == 0)
            {
                return (char) RandomNumberGenerator.GetInt32('A', 'Z');
            }
            else
            {
                return (char) RandomNumberGenerator.GetInt32('a', 'z');
            }
        }
        else
        {
            if (upperCase.Value)
            {
                return (char) RandomNumberGenerator.GetInt32('A', 'Z');
            }
            else
            {
                return (char) RandomNumberGenerator.GetInt32('a', 'z');
            }
        }
    }

    /// <summary>
    /// Génère un nombre entier sur un nombre donné de chiffres, PadLeft('0') sur la gauche
    /// </summary>
    /// <param name="digits"></param>
    /// <returns></returns>
    public static string GetRandomNumber(int digits)
    {
        // Ex, pour digits==3 :
        int max = (int) Math.Pow(10, digits); // max = 1000
        max = max - 1; // max = 999, OK :)

        var result = RandomNumberGenerator.GetInt32(1, max).ToString().PadLeft(digits, '0');

        return result;
    }

    public static T GetRandomElement<T>(ICollection<T> collection)
    {
        if (collection.Count == 0)
        {
            return default(T);
        }
        else
        {
            var r = RandomNumberGenerator.GetInt32(collection.Count);

            if (collection is IList<T>)
            {
                return ((IList<T>)collection)[r];
            }
            else
            {
                return collection.ToList()[r];
            }
        }
    }
}