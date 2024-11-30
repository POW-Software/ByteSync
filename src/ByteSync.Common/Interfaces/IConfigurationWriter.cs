namespace ByteSync.Common.Interfaces;

public interface IConfigurationWriter<T> where T : class
{
    public void SaveConfiguration(T config, string configurationPath);
}