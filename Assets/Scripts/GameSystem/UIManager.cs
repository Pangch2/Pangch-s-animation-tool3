using JetBrains.Annotations;
using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using FileSystem;
using System;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Mainmenu;
using DG.Tweening;

namespace GameSystem
{
    public class UIManager : BaseManager
    {

        [Flags]
        public enum UIStatus
        {
            None = 0,
            OnAnimUIPanel = 1 << 0,
            OnSettingPanel = 1 << 1,
            OnExportPanel = 1 << 2,
            OnDraggingPanel = 1 << 3,
            OnMenuBarPanel = 1 << 4,
            OnPopupPanel = 1 << 5,
        }

        private static UIStatus _currentUIStatus = UIStatus.None;
        public static UIStatus CurrentUIStatus
        {
            get => _currentUIStatus;
            set
            {
                _currentUIStatus = value;
                GameManager.SetPlayerInput((_currentUIStatus | UIStatus.OnMenuBarPanel | UIStatus.OnExportPanel) != UIStatus.None);
                //Debug.Log($"CurrentUIStatus: {_currentUIStatus}");
            }
        }

        const string DefaultLoadingText = "Loading...";

        private FileLoadManager _fileManager;

        [FormerlySerializedAs("LoadingPanel")] public GameObject loadingPanel;
        public TextMeshProUGUI loadingText;

        private int _cursorID;

        public GameObject popupPanel;
        public TextMeshProUGUI popupText1;
        public TextMeshProUGUI popupText2;
        public Button popupApplyButton;
        public Button popupCancelButton;
        public Action<bool> onApplyOrCancel;

        public CanvasGroup canvasGroup;

        private void Start()
        {
            _fileManager = GameManager.GetManager<FileLoadManager>();

            _currentUIStatus = UIStatus.None;

            popupApplyButton.onClick.AddListener(() => OnPopupButton(true));
            popupCancelButton.onClick.AddListener(() => OnPopupButton(false));

            canvasGroup = GetComponent<CanvasGroup>();

            if (MainmenuManager.isFirstVisiting)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;

                canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.InQuart).OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                });
            }
        }

        public void SetLoadingPanel(bool isOn)
        {
            loadingPanel.SetActive(isOn);
            if (isOn)
            {
                _cursorID = CursorStack.Push(NTCursors.Busy);
            }
            else
            {
                CursorStack.Pop(_cursorID);
                loadingText.text = DefaultLoadingText;
            }
        }

        public void SetLoadingText(string text)
        {
            loadingText.text = text;
        }

        public void OnPressImportButton()
        {
            _fileManager.ImportFile();
        }

        public void SetPopupPanel(string text1, string text2, Action<bool> OnApplyOrCancel = null)
        {
            CurrentUIStatus |= UIStatus.OnPopupPanel;
            popupPanel.SetActive(true);
            popupText1.text = text1;
            popupText2.text = text2;

            onApplyOrCancel = OnApplyOrCancel;
        }

        void OnPopupButton(bool isApply)
        {
            CurrentUIStatus &= ~UIStatus.OnPopupPanel;
            popupPanel.SetActive(false);

            onApplyOrCancel?.Invoke(isApply);
        }

        public async UniTask<bool> SetPopupPanelAsync(string text1, string text2)
        {
            var tcs = new UniTaskCompletionSource<bool>();

            SetPopupPanel(text1, text2, isApply =>
            {
                tcs.TrySetResult(isApply);
            });

            return await tcs.Task;
        }

        void OnDestroy()
        {
            _currentUIStatus = UIStatus.None;
        }
    }
}
