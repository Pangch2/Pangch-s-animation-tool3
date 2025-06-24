using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystem
{
    public class WindowFileHandler : MonoBehaviour
    {
        public UnityEvent<List<string>> OnFilesDropped;

        private const string DllName = "FileHookDLL";

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        // --- C++ 콜백과 연결될 델리게이트 정의 ---
        // C++의 wchar_t*를 C#의 string으로 받도록 MarshalAs 지정
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate void FileDropCallback([MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void InitializeHook(IntPtr unityHWnd, FileDropCallback callback);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void ShutdownHook();

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void ProcessClipboardFiles(FileDropCallback callback);


        private static FileDropCallback _fileDropCallbackInstance;
        private static readonly List<string> _queuedFiles = new List<string>();
        private readonly List<string> _processedFiles = new List<string>();

        void OnEnable()
        {
            // 에디터가 아닌 윈도우 플레이어에서만 실행
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                _fileDropCallbackInstance = OnFileDroppedFromDll;
                InitializeHook(GetActiveWindow(), _fileDropCallbackInstance);
            }
        }

        void OnDisable()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                ShutdownHook();
            }
        }

        void Update()
        {
            lock (_queuedFiles)
            {
                if (_queuedFiles.Count > 0)
                {
                    _processedFiles.AddRange(_queuedFiles);
                    _queuedFiles.Clear();
                }
            }

            if (_processedFiles.Count > 0)
            {
                OnFilesDropped.Invoke(new List<string>(_processedFiles));
                _processedFiles.Clear();
            }
        }

        public void CheckForPastedFiles()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                ProcessClipboardFiles(_fileDropCallbackInstance);
            }
        }

        [AOT.MonoPInvokeCallback(typeof(FileDropCallback))]
        private static void OnFileDroppedFromDll(string filePath)
        {
            lock (_queuedFiles)
            {
                _queuedFiles.Add(filePath);
            }
        }
    }
}