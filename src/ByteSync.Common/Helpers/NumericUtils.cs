using System;

namespace ByteSync.Common.Helpers;

public static class NumericUtils
{
    public static double ConvertHexToDouble(string hexa)
    {
        // 05/11/2022 : pourrait être amélioré avec https://stackoverflow.com/questions/51445431/base64-encoding-of-a-sha256-string ???
        
        double result = 0;
        int pow = 0;
        for (int i = hexa.Length - 1; i >= 0; i--)
        {
            int hexNumber = int.Parse(hexa[i].ToString(), System.Globalization.NumberStyles.HexNumber);
            double value = hexNumber * Math.Pow(16, pow);
            result += value;

            pow += 1;
        }

        return result;
    }
    
    public static int GetDecimals(decimal d, int i = 0)
    {
        decimal multiplied = (decimal)((double)d * Math.Pow(10, i));
        if (Math.Round(multiplied) == multiplied)
            return i;
        return GetDecimals(d, i+1);
    }
}