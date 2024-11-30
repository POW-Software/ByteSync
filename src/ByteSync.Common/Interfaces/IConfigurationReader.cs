namespace ByteSync.Common.Interfaces;

public interface IConfigurationReader<T> where T : class
{
    public T? GetConfiguration(string configurationPath);
}