using System.Collections;
using GameSystem;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace FileSystem
{
    public class ExportManager : MonoBehaviour
    {
        public GameObject exportPanel;

        public TextMeshProUGUI exportPathText;
        private string exportFolder = "result";
        public string ExportFolder
        {
            get => exportFolder;
            set
            {
                exportFolder = value;
                SetPathText(currentPath);
            }
        }
        private string currentPath;
        private string finalPath;

        private void Start()
        {
            SetPathText(Application.dataPath);
        }

        private void SetPathText(string path)
        {
            Debug.Log($"Export path: {path}");
            currentPath = path;
            finalPath = path + '/' + exportFolder;
            exportPathText.text = finalPath;
        }

        public void SetExportPanel(bool isShow)
        {
            exportPanel.SetActive(isShow);
            if (isShow)
            {
                //SetPathText(currentPath);
                UIManager.CurrentUIStatus |= UIManager.UIStatus.OnPopupPanel;
            }
            else
            {
                UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnPopupPanel;
            }
        }

        // public void ChangePath() => StartCoroutine(GetNewPathCoroutine());
        public void ChangePath() => GetNewPathCoroutine().Forget();

        private async UniTask GetNewPathCoroutine()
        {
            await FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, Application.dataPath).ToUniTask();

            if (FileBrowser.Success)
            {
                //Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetPathText(FileBrowser.Result[0]);
            }
        }

        public void OnExportButton()
        {
            ExportAnimation().Forget();
        }

        public async UniTask ExportAnimation()
        {
            await UniTask.Delay(500);
        }
    }
}