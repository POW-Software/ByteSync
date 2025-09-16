namespace ByteSync.Services.Applications;

public class MsixPfnParser : IMsixPfnParser
{
    public bool TryParse(string containerDirectoryName, out string? packageFamilyName)
    {
        packageFamilyName = null;
        
        if (string.IsNullOrWhiteSpace(containerDirectoryName))
        {
            return false;
        }
        
        var idxUnderscore = containerDirectoryName.IndexOf('_');
        var idxDoubleUnderscore = containerDirectoryName.IndexOf("__", StringComparison.Ordinal);
        
        if (idxUnderscore > 0 && idxDoubleUnderscore > idxUnderscore)
        {
            var name = containerDirectoryName.Substring(0, idxUnderscore);
            var publisherId = containerDirectoryName.Substring(idxDoubleUnderscore + 2);
            
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(publisherId))
            {
                packageFamilyName = $"{name}_{publisherId}";
                
                return true;
            }
        }
        
        return false;
    }
}