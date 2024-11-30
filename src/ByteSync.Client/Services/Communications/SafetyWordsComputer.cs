using System.Text.RegularExpressions;
using ByteSync.Common.Helpers;

namespace ByteSync.Services.Communications;

/// <summary>
/// Cette classe permet de déterminer une liste de mots à partir d'un MD5.
/// Elle se base sur mnemonicode pour associer des mots au MD5.
/// Infos : https://github.com/singpolyma/mnemonicode // https://github.com/singpolyma/mnemonicode/blob/master/mn_wordlist.c
/// </summary>
public class SafetyWordsComputer
{
    public SafetyWordsComputer(string[] availableWords)
    {
        AvailableWords = availableWords;
    }

    public string[] AvailableWords { get; }

    public string[] Compute(string hexInput)
    {
        // On contrôle la valeur entrante
        if (hexInput.IsNullOrEmpty())
        {
            throw new ArgumentOutOfRangeException(nameof(hexInput), "input can not be empty");
        }
        if (!Regex.IsMatch(hexInput, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(hexInput), "wrong input format");
        }

        // On convertit hexInput en decimal
        // On détermine combien de mots seront nécessaires pour couvrir la 
        // Puis on le convertit en base 1633 / mnemonicode en effectuant une division
        // On complète éventuellement avec les mots manquants
        
        // Précédemment, on découpait la chaîne en groupes de 4 caractères, mais cela entraînait des sauts lors de la division
        // et diminuait la qualité de la conversion

        var result = new List<string>();
        
        // On calcule combien de valeurs possibles existent en fonction de la longueur de la chaîne en entrée
        var hexaInputPossibleValues = Math.Pow(16, hexInput.Length);
        var coverWordCount = 1;
        while (Math.Pow(AvailableWords.Length, coverWordCount) < hexaInputPossibleValues)
        {
            coverWordCount += 1;
        }

        var quotient = NumericUtils.ConvertHexToDouble(hexInput);
        string word;
        while (quotient > AvailableWords.Length)
        {
            // On procède à des divisions successives pour le changement de base
            // Le reste est ajouté à la liste, le quotient est ensuite redivisé
            // On continu tant que "quotient est divisable", c'est à dire tant que "quotient > AvailableWords.Length"
            
            var modulo = (int) (quotient % AvailableWords.Length);
            word = AvailableWords[modulo];
            result.Add(word);
            
            quotient = quotient / AvailableWords.Length;
        }
        
        // A la fin, on ajoute le dernier quotient à la liste
        word = AvailableWords[(int) quotient];
        result.Add(word);
        
        // Si le nombre en entrée est trop petit, on n'a pas atteint le nombre de mots attendu
        // On complète
        while (result.Count < coverWordCount)
        {
            result.Add(AvailableWords[0]);
        }

        // On a ajouté les mots à l'envers, on retourne la liste
        result.Reverse();

        return result.ToArray();
    }

    // private double ConvertHexToDouble(string hexa)
    // {
    //     // 05/11/2022 : pourrait être amélioré avec https://stackoverflow.com/questions/51445431/base64-encoding-of-a-sha256-string ???
    //     
    //     double result = 0;
    //     int pow = 0;
    //     for (int i = hexa.Length - 1; i >= 0; i--)
    //     {
    //         int hexNumber = int.Parse(hexa[i].ToString(), System.Globalization.NumberStyles.HexNumber);
    //         double value = hexNumber * Math.Pow(16, pow);
    //         result += value;
    //
    //         pow += 1;
    //     }
    //
    //     return result;
    // }
}