using System.IO;
using System.IO.Abstractions;
using System.Xml.Serialization;
using ByteSync.Common.Interfaces;

namespace ByteSync.Common.Controls;

public class ConfigurationReader<T> : IConfigurationReader<T> where T : class
{
    public ConfigurationReader(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public IFileSystem FileSystem { get; }

    public T? GetConfiguration(string configurationPath)
    {
        if (!FileSystem.File.Exists(configurationPath))
        {
            return default;
        }
        else
        {
            using var fs = FileSystem.FileStream.New(configurationPath, FileMode.Open);
                
            XmlSerializer xs = new XmlSerializer(typeof(T));
            T? config = xs.Deserialize(fs) as T;

            return config;
        }
    }
}