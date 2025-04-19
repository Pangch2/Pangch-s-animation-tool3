using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Minecraft;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mainmenu
{
    public class MainmenuManager : MonoBehaviour
    {
        MinecraftFileManager minecraftFileManager;
        public TextMeshProUGUI versionText;

        public bool isInstalled = false;

        public Button[] buttons;

        public RawImage fadeImg;

        void Start()
        {
            minecraftFileManager = MinecraftFileManager.Instance;

            string path = PlayerPrefs.GetString("MinecraftPath", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".minecraft/versions",
                    MinecraftFileManager.MinecraftVersion,
                    MinecraftFileManager.MinecraftVersion + ".jar"
                    );
            }
            // Debug.Log(path);
            SetNewPath(path).Forget();
        }

        public async UniTask<bool> SetNewPath(string path)
        {
            string version = RegexPatterns.VersionRegex.Match(path).Value;
            if (string.IsNullOrEmpty(version))
            {
                versionText.text = "Version not found";
                buttons[0].interactable = false;
                buttons[1].interactable = false;
                return false;
            }

            isInstalled = await minecraftFileManager.ReadMinecraftFile(path, version);

            buttons[0].interactable = isInstalled;
            buttons[1].interactable = isInstalled;

            if (!isInstalled)
            {
                versionText.text = "Version not found";
                return false;
            }

            versionText.text = "Version: " + version;
            PlayerPrefs.SetString("MinecraftPath", path);
            return true;
        }

        public async void OnAnimatorButton()
        {
            var loadScene = SceneManager.LoadSceneAsync("Animation");
            loadScene.allowSceneActivation = false;
            await FadeOut();
            await UniTask.Delay(1000);
            loadScene.allowSceneActivation = true;
        }

        public void OnDisplayMakerButton()
        {

        }

        public void OnAddTagButton()
        {

        }

        async UniTask FadeOut()
        {
            fadeImg.gameObject.SetActive(true);
            fadeImg.color = new Color(0, 0, 0, 0);
            float time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime;
                fadeImg.color = new Color(0, 0, 0, time);
                await UniTask.Yield();
            }
        }
    }
}