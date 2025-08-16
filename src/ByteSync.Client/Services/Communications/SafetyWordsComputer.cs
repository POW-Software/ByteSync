using System.Text.RegularExpressions;
using ByteSync.Common.Helpers;

namespace ByteSync.Services.Communications;

/// <summary>
/// This class allows you to determine a list of words from an MD5.
/// It is based on mnemonicode to associate words with MD5.
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
        // The incoming value is checked.
        if (hexInput.IsNullOrEmpty())
        {
            throw new ArgumentOutOfRangeException(nameof(hexInput), "input can not be empty");
        }
        var safeRegex = new Regex("^[0-9a-f]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        if (!safeRegex.IsMatch(hexInput))
        {
            throw new ArgumentOutOfRangeException(nameof(hexInput), "wrong input format");
        }

        // Convert hexInput to decimal
        // We determine how many words will be needed to cover the 
        // Then we convert it to base 1633 / mnemonicode by performing a division
        // We fill in any missing words

        // Previously, we split the string into groups of 4 characters, but this caused jumps during division
        // and reduced the quality of the conversion

        var result = new List<string>();
        
        // We calculate how many possible values exist based on the length of the input string
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
            // Successive divisions are performed to change the base
            // The remainder is added to the list, and the quotient is then divided again
            // This is continued as long as “quotient is divisible,” i.e., as long as “quotient > AvailableWords.Length”
            
            var modulo = (int) (quotient % AvailableWords.Length);
            word = AvailableWords[modulo];
            result.Add(word);
            
            quotient = quotient / AvailableWords.Length;
        }
        
        // At the end, add the last quotient to the list
        word = AvailableWords[(int) quotient];
        result.Add(word);
        
        // If the number entered is too small, the expected number of words has not been reached
        // Complete
        while (result.Count < coverWordCount)
        {
            result.Add(AvailableWords[0]);
        }

        // We added the words backwards, then reversed the list
        result.Reverse();

        return result.ToArray();
    }
}