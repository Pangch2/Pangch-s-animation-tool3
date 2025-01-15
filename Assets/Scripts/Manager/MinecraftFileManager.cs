using System.IO;
using System.IO.Compression;
using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

public class MinecraftFileManager : RootManager
{
    public Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
    public Dictionary<string, string> jsonFiles = new Dictionary<string, string>();

    string[] readFolder = { "models", "textures", "blockstates", "items" }; // ���� ����
    string[] readTexturesFolders = { "block", "item" };

    readonly string Appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
    readonly string minecraftPath = ".minecraft/versions";
    readonly string minecraftVersion = "1.21.4";

    [SerializeField]
    string filePath;

    private void Start()
    {
        filePath = $"{Appdata}/{minecraftPath}/{minecraftVersion}/{minecraftVersion}.jar";
        Thread thread = new Thread(() => ReadJarFile(filePath, "assets/minecraft"));
        thread.Start();
    }

    void ReadJarFile(string path, string targetFolder)
    {
        Debug.Log($"Reading JAR file: {path}");
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        using ZipArchive jarArchive = ZipFile.OpenRead(path);
        foreach (var entry in jarArchive.Entries)
        {
            if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                continue;

            // ���� ���͸�
            string folderName = GetTopLevelFolder(entry.FullName, targetFolder);

            if (folderName == "textures")
            {
                // textures ���� ó��
                if (!IsReadFolder(entry.FullName)) continue; // ������ ���� Ȯ��
                if (entry.FullName.EndsWith(".png"))
                {
                    //Debug.Log($"Found texture file: {entry.FullName}");
                    SavePNGFile(entry, entry.FullName);
                }
            }
            else if (readFolder.Contains(folderName))
            {
                // �ٸ� ���� ó��
                if (entry.FullName.EndsWith(".json"))
                {
                    SaveJson(entry, entry.FullName);
                    //Debug.Log($"Found JSON file: {entry.FullName}");
                }
            }
        }

        Debug.Log("Finished reading JAR file");
        Debug.Log("Textures: " + textureFiles.Count);
        Debug.Log("JSON: " + jsonFiles.Count);
    }

    // �ֻ��� ���� �̸� ����
    string GetTopLevelFolder(string fullPath, string targetFolder)
    {
        string relativePath = fullPath.Substring(targetFolder.Length + 1); // targetFolder ���� ���
        int firstSlashIndex = relativePath.IndexOf('/');
        return firstSlashIndex > -1 ? relativePath.Substring(0, firstSlashIndex) : relativePath;
    }

    // �о���� �������� Ȯ��
    bool IsReadFolder(string fullPath)
    {
        foreach (string readFolders in readTexturesFolders)
        {
            if (fullPath.Contains($"textures/{readFolders}/"))
            {
                return true;
            }
        }
        return false;
    }

    void SaveJson(ZipArchiveEntry entry, string path)
    {
        using Stream stream = entry.Open();
        using StreamReader reader = new StreamReader(stream);

        path = path.Replace("assets/minecraft/", "");

        string json = reader.ReadToEnd();
        jsonFiles.Add(path, json);
        //Debug.Log("JSON: " + path);
    }

    void SavePNGFile(ZipArchiveEntry entry, string path)
    {
        using Stream stream = entry.Open();
        using MemoryStream memoryStream = new MemoryStream();

        path = path.Replace("assets/minecraft/", "");

        stream.CopyTo(memoryStream);
        textureFiles.Add(path, memoryStream.ToArray());
        //Debug.Log("PNG: " + path);
    }
}