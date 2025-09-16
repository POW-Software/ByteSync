namespace ByteSync.Services.Applications;

public interface IMsixPfnParser
{
    bool TryParse(string containerDirectoryName, out string? packageFamilyName);
}