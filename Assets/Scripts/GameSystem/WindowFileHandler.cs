using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystem
{
    public class WindowFileHandler : MonoBehaviour
    {
        public UnityEvent<List<string>> OnFilesDropped;

        private const string DllName = "FileHook";

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate void FileDropCallback([MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void InitializeHook(System.IntPtr unityHWnd, FileDropCallback cb);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void ShutdownHook();

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        private static extern void ProcessClipboardFiles(FileDropCallback cb);

        private static FileDropCallback _dropCb;
        private static readonly List<string> _queue = new();
        private readonly List<string> _processed = new();

        // ────────────────────────────────────────────────
        private void OnEnable()
        {
            if (Application.isEditor || Application.platform != RuntimePlatform.WindowsPlayer)
                return;

            _dropCb = OnFileDroppedFromDll;
            // 윈도우 핸들이 유효한 시점이므로 바로 호출해도 안전합니다.
            InitializeHook(GetActiveWindow(), _dropCb);
        }

        private void OnDisable()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
                ShutdownHook();
        }

        private void Update()
        {
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    _processed.AddRange(_queue);
                    _queue.Clear();
                }
            }

            if (_processed.Count > 0)
            {
                OnFilesDropped?.Invoke(new List<string>(_processed));
                _processed.Clear();
            }
        }

        public void CheckForPastedFiles()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
                ProcessClipboardFiles(_dropCb);
        }

        [AOT.MonoPInvokeCallback(typeof(FileDropCallback))]
        private static void OnFileDroppedFromDll(string path)
        {
            lock (_queue) _queue.Add(path);
        }
    }
}
