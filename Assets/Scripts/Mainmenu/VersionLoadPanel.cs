using System;
using System.IO;
using Cysharp.Threading.Tasks;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace Mainmenu
{
    public class VersionLoadPanel : MonoBehaviour
    {
        MainmenuManager manager;
        public GameObject versionLoadPanel;

        public TMP_InputField inputField;

        const string helpURL = "https://potangaming.tistory.com/319";

        FileBrowser.Filter loadFilter = new FileBrowser.Filter("Files", ".jar");

        void Start()
        {
            manager = GetComponent<MainmenuManager>();
            SetupFileBrowser();
        }

        private void SetupFileBrowser()
        {
            FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");
            FileBrowser.AddQuickLink("Minecraft Folder", Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft"
            ));

            var download = Path.GetDirectoryName(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            );
            download = Path.Combine(download, "Downloads");
            FileBrowser.AddQuickLink("Downloads", download);
        }

        public void OnPanelButton()
        {
            versionLoadPanel.SetActive(true);
            inputField.text = PlayerPrefs.GetString("MinecraftPath", string.Empty);

        }

        public async void OnLoadByFileBrowserButton()
        {
            FileBrowser.SetFilters(false, loadFilter);
            await FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Files,
                false,
                string.IsNullOrEmpty(inputField.text) ? null : inputField.text,
                "Select Minecraft Jar File",
                "Select").ToUniTask();

            if (FileBrowser.Success)
            {
                inputField.text = FileBrowser.Result[0];
            }
        }

        public void OnHelpButton()
        {
            Application.OpenURL(helpURL);
        }

        public async void OnChangePathButton()
        {
            bool success = await manager.SetNewPath(inputField.text);
            if (success)
            {
                versionLoadPanel.SetActive(false);
            }
            else
            {
                inputField.text = string.Empty;
            }
        }

        public void OnPanelCloseButton()
        {
            versionLoadPanel.SetActive(false);
        }
    }
}

