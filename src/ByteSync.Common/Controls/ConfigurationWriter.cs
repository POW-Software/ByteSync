using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ByteSync.Common.Interfaces;

namespace ByteSync.Common.Controls;

public class ConfigurationWriter<T> : IConfigurationWriter<T> where T : class
{
    public ConfigurationWriter(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public IFileSystem FileSystem { get; }
    
    public void SaveConfiguration(T config, string configurationPath)
    {
        var configurationPathTemp = configurationPath + ".tmp";
        
        using (var fileStream = FileSystem.FileStream.New(configurationPathTemp, FileMode.Create))
        {
            using var streamWriter = XmlWriter.Create(fileStream, new()
            {
                Encoding = Encoding.UTF8,
                Indent = true
            });

            //Create our own namespaces for the output
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            //Add an empty namespace and empty value
            ns.Add("", "");
            
            XmlSerializer xs = new XmlSerializer(typeof(T));
            xs.Serialize(streamWriter, config, ns);
        }

        FileSystem.File.Delete(configurationPath);

        FileSystem.File.Move(configurationPathTemp, configurationPath);
    }
}