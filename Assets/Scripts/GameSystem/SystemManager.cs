//using B83.Win32;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDObjectSystem;
using Minecraft;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace GameSystem
{
    public class SystemManager : BaseManager
    {
        public List<string> filesDropped;


        private float _deltaTime;

        [SerializeField] private int size = 15;
        [SerializeField] private Color color = Color.white;

        private GUIStyle _style;

        protected override void Awake()
        {
            base.Awake();

            Application.targetFrameRate = 165;
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

            GetComponent<FildDragAndDrop>().OnFilesDropped.AddListener(OnFilesDropped);

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

    }
}
