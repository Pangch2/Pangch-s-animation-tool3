using System.IO;
using System.IO.Compression;
using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class MinecraftFileManager : RootManager
{
    static MinecraftFileManager instance;

    Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
    Dictionary<string, string> jsonFiles = new Dictionary<string, string>();

    // readPreReadedFiles�� �ִ� ���ϵ��� �̸� �о��
    Dictionary<string, MinecraftModelData> importantModels = new Dictionary<string, MinecraftModelData>();

    readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; // ���� ����
    readonly string[] readTexturesFolders = 
        { "block", "item", "entity/bed", "entity/shulker", "entity/chest", "entity/conduit" }; // textures�� ���� ����
    readonly string[] readPreReadedFiles =
        {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // �̸� �ε��� ����

    readonly string[] hardcodeNames = { "bed", "shulker_box", "chest", "conduit" };

    readonly string Appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
    const string minecraftPath = ".minecraft/versions";
    const string minecraftVersion = "1.21.4";

    [SerializeField]
    string filePath;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    private void Start()
    {
        filePath = $"{Appdata}/{minecraftPath}/{minecraftVersion}/{minecraftVersion}.jar";
        Thread thread = new Thread(() => ReadJarFile(filePath, "assets/minecraft"));
        thread.Start();
    }

    public static JObject GetJSONData(string path)
    {
        if (path.Contains("bed"))
        {
            Debug.Log("Bed: " + path);
            var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
            Debug.Log("Bed: " + bed.text);
            return JObject.Parse(bed.text);
        }

        if (instance.jsonFiles.ContainsKey(path))
        {
            return JObject.Parse(instance.jsonFiles[path]);
        }
        return null;
    }

    public static MinecraftModelData GetModelData(string path)
    {
        //Debug.Log("Get Model Data: " + path);

        if (instance.importantModels.ContainsKey(path))
        {
            return instance.importantModels[path];
        }

        for (int i = 0; i < instance.hardcodeNames.Length; i++)
        {
            if (path.Contains(instance.hardcodeNames[i]))
            {
                return JsonConvert.DeserializeObject<MinecraftModelData>(Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", "")).text);
            }
        }

        if (instance.jsonFiles.ContainsKey(path))
        {
            return  JsonConvert.DeserializeObject<MinecraftModelData>(instance.jsonFiles[path]);
        }
        return null;
    }

    public static Texture2D GetTextureFile(string path)
    {
        if (instance.textureFiles.ContainsKey(path))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.alphaIsTransparency = true;
            texture.Apply();

            texture.LoadImage(instance.textureFiles[path]);
            
            return texture;
        }
        return null;
    }

    public static string RemoveNamespace(string path)
    {
        return path.Replace("minecraft:", "");
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

        // readImportantModels();
        foreach (var read in readPreReadedFiles)
        {
            string readPath = $"models/{read}.json";
            if (instance.jsonFiles.ContainsKey(readPath))
            {
                importantModels.Add(read, GetModelData(jsonFiles[readPath]));
            }
        }
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