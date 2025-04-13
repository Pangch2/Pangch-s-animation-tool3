using System;
using System.Collections;
using System.Text;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using BDObjectSystem;
using FileSystem;
using Cysharp.Threading.Tasks;

namespace BDObjectSystem.Display
{
    public class HeadGenerator : BlockModelGenerator
    {
        public enum HeadType
        {
            Player,
            Piglin,
            Dragon,
            Zombie,
            Skull,
            Witherskull,
            Creeper,
            None
        }

        private const string DefaultTexturePath = "entity/";

        public HeadType headType;
        public Texture2D headTexture;
        public string downloadUrl;

        public void GenerateHead(string name)
        {
            modelName = "head";

            headType = name switch
            {
                "player" => HeadType.Player,
                "piglin" => HeadType.Piglin,
                "dragon" => HeadType.Dragon,
                "zombie" => HeadType.Zombie,
                "skeleton" => HeadType.Skull,
                "wither_skeleton" => HeadType.Witherskull,
                "creeper" => HeadType.Creeper,
                _ => HeadType.None
            };

            if (headType == HeadType.None)
            {
                CustomLog.LogError("Head Type Error.");
                return;
            }

            // StartCoroutine(GenerateHeadCoroutine());
            GenerateHeadCoroutine().Forget();
        }

        private async UniTaskVoid GenerateHeadCoroutine()
        {
            GameManager.GetManager<FileLoadManager>().WorkingGenerators.Add(this);

            try
            {
                headTexture = headType switch
                {
                    HeadType.Player => await SetPlayerTexture().Timeout(TimeSpan.FromSeconds(200)),
                    HeadType.Piglin => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "piglin/piglin.png"),
                    HeadType.Dragon => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "enderdragon/dragon.png"),
                    HeadType.Zombie => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "zombie/zombie.png"),
                    HeadType.Skull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/skeleton.png"),
                    HeadType.Witherskull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/wither_skeleton.png"),
                    HeadType.Creeper => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "creeper/creeper.png"),
                    _ => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png")
                };

            }
            catch (Exception e)
            {
                CustomLog.LogError("Head Texture Error: " + e.Message);
                headTexture = MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");
            }
            finally
            {
                GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }

            switch (headType)
            {
                case HeadType.Player:
                    SetModel("item/player_head");
                    break;
                case HeadType.Zombie:
                    SetModel("item/zombie_head");
                    break;
                case HeadType.Witherskull:
                case HeadType.Skull:
                case HeadType.Creeper:
                    SetModel("item/creeper_head");
                    break;
                case HeadType.Piglin:
                    SetModel("item/piglin_head");
                    break;
                case HeadType.Dragon:
                    SetModel("item/dragon_head");
                    break;
                case HeadType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override Texture2D CreateTexture(string path)
        {
            return headTexture;
        }

        private async UniTask<Texture2D> SetPlayerTexture()
        {
            // Get Playter Texture
            var data = transform.parent.parent.GetComponent<BdObjectContainer>().BdObject;

            if (!data.ExtraData.TryGetValue("defaultTextureValue", out var value))
                return MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");

            var jsonDataBytes = Convert.FromBase64String(value.ToString());
            var jsonString = Encoding.UTF8.GetString(jsonDataBytes);

            var jsonObject = JObject.Parse(jsonString);

            downloadUrl = jsonObject["textures"]?["SKIN"]?["url"]?.ToString().Replace("http://", "https://");

            using var request = UnityWebRequestTexture.GetTexture(downloadUrl);

            while (true)
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
#if UNITY_EDITOR
                    CustomLog.LogError("Error: " + request.error);
#else
            CustomLog.LogError("Download Fail! Try Again");
#endif
                }
                else
                {
                    var downloadedTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                    downloadedTexture.filterMode = FilterMode.Point;
                    downloadedTexture.wrapMode = TextureWrapMode.Clamp;
                    downloadedTexture.Apply();

                    //SetPlayerSkin(downloadedTexture);
                    //downloadedTexture.Apply();

                    return downloadedTexture;

                }
            }


        }

    }
}
