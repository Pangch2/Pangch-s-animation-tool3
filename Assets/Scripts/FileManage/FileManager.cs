using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

public class FileManager
{
    public static Setting LoadSettingFromFile(string filePath)
    {
        string jsonData = File.ReadAllText(filePath);
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
            var serializer = new DataContractJsonSerializer(typeof(Setting));
            return (Setting)serializer.ReadObject(stream);
        }
        catch
        {
            return new Setting();
        }
    }
}
