using System;
using UnityEngine;
using FileSystem;
using TMPro;
using System.Collections;
using SimpleFileBrowser;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace GameSystem
{

    public class MenuBarManager : BaseManager
    {
        private const string githubURL = "https://github.com/Potan7/Potan-Minecraft-Display-Engine";
        private ExportManager _exportManager;
        private SaveManager _saveManager;

        public TextMeshProUGUI currentFileText;

        public GameObject FilePanelButtons;

        public Button[] saveButtons;

        void Start()
        {
            _exportManager = GetComponent<ExportManager>();
            _saveManager = GameManager.GetManager<SaveManager>();
        }

        public void SetCurrentFileText(string fileName)
        {
            currentFileText.text = fileName;
        }

        private void UpdateSaveButtonInteraction()
        {
            foreach (var button in saveButtons)
            {
                button.interactable = _saveManager.IsNoneSaved;
            }
        }

        #region  Button Events

        public void OnSaveButton()
        {
            if (string.IsNullOrEmpty(_saveManager.MDEFilePath))
            {
                // Save As 실행하기
                OnSaveAsButton();
            }
            else
            {
                _saveManager.SaveMCDEFile();
            }
        }

        public void OnSaveKeyInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnSaveButton();
            }
        }

        public void OnSaveAsButton()
        {
            _saveManager.SaveAsNewFile();
        }

        public void OnLoadButton()
        {
            _saveManager.LoadMCDEFile().Forget();
        }

        public void OnExportButton()
        {
            _exportManager.SetExportPanel(true);
        }

        public void OnGitHubButton()
        {
            Application.OpenURL(githubURL);
        }

        public void OnBacktoMainMenuButton()
        {
            GameManager.GetManager<UIManager>().canvasGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuart).OnComplete(() =>
            {
                GameManager.GetManager<UIManager>().canvasGroup.interactable = false;
                SceneManager.LoadScene("Mainmenu");
            });
            
        }
        #endregion

        #region Mouse Events

        public void OnMenuBarMouseEnter()
        {
            UpdateSaveButtonInteraction();
            UIManager.CurrentUIStatus |= UIManager.UIStatus.OnMenuBarPanel;
        }

        public void OnMenuBarMouseExit()
        {
            if (FilePanelButtons.activeSelf) return;
            UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnMenuBarPanel;
        }

        public void OnFilesButtonMouseEnter()
        {
            FilePanelButtons.SetActive(true);
        }

        public void OnFilesButtonMouseExit()
        {
            FilePanelButtons.SetActive(false);
            UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnMenuBarPanel;
        }
        

        #endregion
    }
}
