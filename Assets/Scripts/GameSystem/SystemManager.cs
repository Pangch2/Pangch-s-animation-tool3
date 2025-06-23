//using B83.Win32;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDObjectSystem;
using Minecraft;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace GameSystem
{
    public class SystemManager : BaseManager
    {
        public List<string> filesDropped;

        private WindowFileHandler _fileDragAndDrop; // FildDragAndDrop 참조 저장

        private float _deltaTime;

        [SerializeField] private int size = 15;
        [SerializeField] private Color color = Color.white;

        private GUIStyle _style;

        protected override void Awake()
        {
            base.Awake();

            // QualitySettings.vSyncCount = 1;
        }

        private void Start()
        {
            _style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = size,
                normal =
                {
                    textColor = color
                }
            };

            if (MinecraftFileManager.Instance.IsReadedFiles == false)
            {
                SceneManager.LoadScene("Mainmenu");
            }

            _fileDragAndDrop = GetComponent<WindowFileHandler>(); // 컴포넌트 참조 가져오기
            _fileDragAndDrop.OnFilesDropped.AddListener(OnFilesDropped);

        }

        private void OnGUI()
        {
            var rect = new Rect(Screen.width - 200, 100, Screen.width, Screen.height);

            var ms = _deltaTime * 1000f;
            var fps = 1.0f / _deltaTime;
            var text = $"{fps:0.} FPS ({ms:0.0} ms)";

            var versionRect = new Rect(Screen.width - 200, 70, Screen.width, Screen.height);
            var version = string.Format("Version: {0}", Application.version);

            GUI.Label(rect, text, _style);
            GUI.Label(versionRect, version, _style);
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnFilesDropped(List<string> files)
        {
            filesDropped = files;

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    CustomLog.Log($"File dropped: {file}");
                    // 여기에 파일 처리 로직을 추가할 수 있습니다.
                }
                else
                {
                    Debug.LogWarning($"File not found: {file}");
                }
            }
        }

        public void OnCopyKey(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CustomLog.Log("Copy key pressed");
                // 여기에 복사 로직을 추가할 수 있습니다.
            }
        }

        public void OnPasteKey(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                // 1. 클립보드에서 파일 경로 목록을 가져옵니다.
                // var pastedFiles = _fileDragAndDrop.GetCopiedFilePaths();
                _fileDragAndDrop.CheckForPastedFiles(); 
                // Debug.Log($"Pasted files count: {pastedFiles}");

                // if (pastedFiles != null && pastedFiles.Count > 0)
                // {
                //     // 파일이 붙여넣기 된 경우, OnFilesDropped를 호출하여 동일한 로직으로 처리
                //     CustomLog.Log($"Pasted files detected via clipboard: {pastedFiles.Count} file(s).");
                //     OnFilesDropped(pastedFiles);
                // }
                // else
                // {
                //     // 파일이 아닌 경우, 기존의 텍스트 클립보드 로직 수행
                //     var clipboardText = GUIUtility.systemCopyBuffer;
                //     if (!string.IsNullOrEmpty(clipboardText))
                //     {
                //         CustomLog.Log($"Paste key pressed. Clipboard text: {clipboardText}");
                //     }
                //     else
                //     {
                //         CustomLog.Log("Paste key pressed, but clipboard is empty or contains no files.");
                //     }
                // }
            }
        }
        
    }
}
