using ByteSync.Common.Helpers;

namespace ByteSync.Services.Converters.BaseConverters;

public abstract class AbstractBaseConverter
{
    public string ConvertTo(string hexInput)
    {
        // On calcule combien de valeurs possibles existent en fonction de la longueur de la chaîne en entrée
        var hexaInputPossibleValues = Math.Pow(16, hexInput.Length);
        var neededFiguresToCover = 1;
        while (Math.Pow(FiguresCount, neededFiguresToCover) < hexaInputPossibleValues)
        {
            neededFiguresToCover += 1;
        }
        
        var quotient = NumericUtils.ConvertHexToDouble(hexInput);

        return ConvertTo(quotient, neededFiguresToCover);
    }

    public string ConvertTo(double quotient, int figuresCount)
    {
        var result = new List<char>();

        char word;
        while (quotient > FiguresCount)
        {
            // On procède à des divisions successives pour le changement de base
            // Le reste est ajouté à la liste, le quotient est ensuite redivisé
            // On continu tant que "quotient est divisable", c'est à dire tant que "quotient > AvailableWords.Length"
            
            var modulo = (int) (quotient % FiguresCount);
            word = BaseFigures[modulo];
            result.Add(word);
            
            quotient = quotient / FiguresCount;
        }
        
        // A la fin, on ajoute le dernier quotient à la liste
        word = BaseFigures[(int) quotient];
        result.Add(word);
        
        // Si le nombre en entrée est trop petit, on n'a pas atteint le nombre de mots attendu
        // On complète
        while (result.Count < figuresCount)
        {
            result.Add(BaseFigures[0]);
        }

        // On a ajouté les mots à l'envers, on retourne la liste
        result.Reverse();

        var finalResult = new string(result.ToArray());

        return finalResult;
    }
    
    public abstract string BaseFigures { get; }

    public double FiguresCount
    {
        get
        {
            return BaseFigures.Length;
        }
    }
}