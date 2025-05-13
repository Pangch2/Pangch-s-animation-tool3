using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        // public RawImage fadeImg;
        public CanvasGroup menu;

        string backgroundColor = "303030";
        // public RectTransform previewPanel;

        public static bool isFirstVisiting = false;

        // public static readonly Regex VersionRegex = new Regex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);

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

            isFirstVisiting = true;
        }

        public async UniTask<bool> SetNewPath(string path)
        {
            const string versionPattern = @"(\d+)\.(\d+)\.(\d+)";

            string version = Regex.Match(path, versionPattern).Value;
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

        public void OnAnimatorButton()
        {
            var loadScene = SceneManager.LoadSceneAsync("Animation");

            loadScene.allowSceneActivation = false;
            menu.interactable = false;
            menu.transform.DOScale(0f, 1f).SetEase(Ease.InOutBack);
            menu.DOFade(0f, 1f).SetEase(Ease.InOutBack);

            if (ColorUtility.TryParseHtmlString("#" + backgroundColor, out Color color))
            {
                var cam = Camera.main;
                cam.DOColor(color, 1f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    loadScene.allowSceneActivation = true;
                });
            }
        }

        public void OnDisplayMakerButton()
        {

        }
    }
}