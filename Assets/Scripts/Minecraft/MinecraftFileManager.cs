using System.IO;
using System.IO.Compression;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Minecraft
{

    public class MinecraftFileManager : BaseManager
    {
        static MinecraftFileManager instance;

        Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
        HashSet<string> isTextureAnimated = new HashSet<string>();
        public Dictionary<string, string> jsonFiles = new Dictionary<string, string>();

        // readPreReadedFiles�� �ִ� ���ϵ��� �̸� �о��
        Dictionary<string, MinecraftModelData> importantModels = new Dictionary<string, MinecraftModelData>();

        //readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; // ���� ����
        //readonly string[] readTexturesFolders = 
        //    { "block", "item", "entity/bed", "entity/shulker", "entity/chest", "entity/conduit", 
        //    "entity/creeper", "entity/zombie/zombie", "entity/skeleton/", "entity/piglin", "entity/player/wide/steve", "entity/enderdragon/dragon"}; // textures�� ���� ����
        //readonly string[] readPreReadedFiles =
        //    {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // �̸� �ε��� ����

        readonly string[] hardcodeNames = { "head", "bed", "shulker_box", "chest", "conduit", "shield", "decorated_pot", "banner" };

        readonly string Appdata = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);

        const string minecraftPath = ".minecraft/versions";
        const string minecraftVersion = "1.21.4";

        [SerializeField]
        string filePath;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        private async void Start()
        {
            filePath = $"{Appdata}/{minecraftPath}/{minecraftVersion}/{minecraftVersion}.jar";

            CustomLog.Log($"Reading minecraft file: {minecraftVersion}");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            await ReadJarFile(filePath, $"assets/minecraft");

            CustomLog.Log("Finished reading JAR file");
            //CustomLog.Log("Textures: " + textureFiles.Count + ", JSON: " + jsonFiles.Count);

            sw.Stop();
            CustomLog.Log($"Reading JAR file took {sw.ElapsedMilliseconds}ms");

        }

        #region Static �Լ���

        public static JObject GetJSONData(string path)
        {
            if (path.Contains("bed") && !path.Contains("items"))
            {
                //CustomLog.Log("Bed: " + path);
                var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
                return JObject.Parse(bed.text);
            }

            if (instance.jsonFiles.ContainsKey(path))
            {
                return JObject.Parse(instance.jsonFiles[path]);
            }
            return null;
        }

        /// <summary>
        /// �� �����͸� �����ɴϴ�.
        /// ���� hardcodeNames�� �ִ� �̸��� Ȯ���ϰ�, �� ���� jsonFiles�� �ִ��� Ȯ���մϴ�.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MinecraftModelData GetModelData(string path)
        {
            //CustomLog.Log("Get Model Data: " + path);

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
                return JsonConvert.DeserializeObject<MinecraftModelData>(instance.jsonFiles[path]);
            }

            CustomLog.LogError("Model not found: " + path);
            return null;
        }

        public static Texture2D GetTextureFile(string path)
        {
            if (instance.textureFiles.ContainsKey(path))
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                //texture.alphaIsTransparency = true;
                texture.Apply();

                texture.LoadImage(instance.textureFiles[path]);

                return texture;
            }
            CustomLog.LogError("Texture not found: " + path);
            return null;
        }

        public static bool IsTextureAnimated(string path)
        {
            return instance.isTextureAnimated.Contains(path + ".mcmeta");
        }

        public static string RemoveNamespace(string path) => path.Replace("minecraft:", "");
        #endregion

        #region ���� �ε�
        async Task ReadJarFile(string path, string targetFolder)
        {
            string[] readTexturesFolders =
            {
                "textures/block", "textures/item", "textures/entity/bed", "textures/entity/shulker",
                "textures/entity/chest", "textures/entity/conduit", "textures/entity/creeper",
                "textures/entity/zombie/zombie", "textures/entity/skeleton/", "textures/entity/piglin",
                "textures/entity/player/wide/steve", "textures/entity/enderdragon/dragon",
                "textures/entity/shield", "textures/entity/conduit/base", "textures/entity/decorated_pot/decorated_pot",
                "textures/entity/banner_base"
            };

            string[] readFolder = { "models", "textures", "blockstates", "items" };

            string[] readPreReadedFiles =
            {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // �̸� �ε��� ����

            if (!File.Exists(path))
            {
                CustomLog.LogError("File not found: " + path);
                return;
            }

            using (ZipArchive jarArchive = ZipFile.OpenRead(path))
            {
                List<Task> tasks = new List<Task>(); // Store tasks for async processing

                foreach (var entry in jarArchive.Entries)
                {
                    if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                        continue;

                    string folderName = GetTopLevelFolder(entry.FullName, targetFolder);

                    byte[] fileData;
                    using (var stream = entry.Open()) // Read the file data first
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        fileData = memoryStream.ToArray();
                    }

                    // Process each file asynchronously
                    tasks.Add(Task.Run(() =>
                    {
                        if (folderName == "textures")
                        {
                            if (!IsReadFolder(entry.FullName, readTexturesFolders))
                                return;

                            if (entry.FullName.EndsWith(".png"))
                                SavePNGData(entry.FullName, fileData);
                            else if (entry.FullName.EndsWith(".mcmeta"))
                            {
                                lock (isTextureAnimated)
                                {
                                    isTextureAnimated.Add(entry.FullName.Replace("assets/minecraft/", ""));
                                }
                            }
                            return;
                        }

                        if (Array.IndexOf(readFolder, folderName) > -1)
                        {
                            if (entry.FullName.EndsWith(".json"))
                                SaveJsonData(entry.FullName, fileData);
                        }
                    }));
                }

                await Task.WhenAll(tasks); // Wait for all async tasks to finish
                
            }

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
        bool IsReadFolder(string fullPath, ReadOnlySpan<string> readTexturesFolders)
        {
            int cnt = readTexturesFolders.Length;
            for (int i = 0; i < cnt; i++)
            {
                if (fullPath.Contains(readTexturesFolders[i]))
                {
                    return true;
                }
            }
            return false;
        }

        void SaveJsonData(string path, byte[] fileData)
        {
            path = path.Replace("assets/minecraft/", "");

            string json;
            using (var memoryStream = new MemoryStream(fileData)) // Read from memory
            using (var reader = new StreamReader(memoryStream))
            {
                json = reader.ReadToEnd();
            }

            lock (jsonFiles) // Lock because jsonFiles is a shared resource
            {
                jsonFiles.Add(path, json);
            }

            // CustomLog.Log("JSON: " + path);
        }

        void SavePNGData(string path, byte[] fileData)
        {
            path = path.Replace("assets/minecraft/", "");

            lock (textureFiles) // Lock because textureFiles is a shared resource
            {
                textureFiles.Add(path, fileData);
            }

            // CustomLog.Log("PNG: " + path);
        }
        #endregion
    }
}