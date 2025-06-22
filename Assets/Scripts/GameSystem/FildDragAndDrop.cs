
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystem
{
    public class FildDragAndDrop : MonoBehaviour
    {
        // 파일이 드롭되었을 때 호출될 이벤트를 정의합니다.
        public UnityEvent<List<string>> OnFilesDropped;

        // --- Win32 API 함수 및 상수 정의 ---
        // C#에서 Windows API를 사용하기 위해 DllImport를 사용합니다.

        // 윈도우 핸들을 가져오는 함수
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        // 파일 드롭을 허용하도록 창 속성을 설정하는 함수
        [DllImport("shell32.dll")]
        private static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);

        // 드롭된 파일의 정보를 가져오는 함수
        [DllImport("shell32.dll")]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder lpszFile, uint cch);

        // 드롭된 파일 처리가 끝났음을 알리는 함수
        [DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        // 창의 프로시저(메시지 처리 함수)를 변경하기 위한 함수
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // 원래 창 프로시저를 호출하기 위한 함수
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4; // 창 프로시저를 설정하거나 가져오기 위한 인덱스
        private const uint WM_DROPFILES = 0x233; // 파일이 드롭되었을 때 발생하는 윈도우 메시지

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate wndProc;
        private IntPtr originalWndProcPtr;
        private IntPtr unityWindowHandle;

        void OnEnable()
        {
            // 윈도우 빌드에서만 작동하도록 처리
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // 델리게이트와 윈도우 핸들 초기화
                wndProc = WndProc;
                unityWindowHandle = GetActiveWindow();

                // 유니티 창이 파일 드롭을 허용하도록 설정
                DragAcceptFiles(unityWindowHandle, true);

                // 기존 윈도우 프로시저를 보존하고, 우리의 커스텀 프로시저로 교체
                originalWndProcPtr = SetWindowLongPtr(unityWindowHandle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
            }
        }

        void OnDisable()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // 창 프로시저를 원래대로 복원
                SetWindowLongPtr(unityWindowHandle, GWLP_WNDPROC, originalWndProcPtr);

                // 파일 드롭 허용 해제
                DragAcceptFiles(unityWindowHandle, false);
            }
        }

        // 커스텀 윈도우 메시지 처리 함수
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // 파일 드롭 메시지(WM_DROPFILES)가 발생했는지 확인
            if (msg == WM_DROPFILES)
            {
                ProcessDroppedFiles(wParam); // wParam에 드롭된 파일 정보(HDROP)가 담겨 있음
                return IntPtr.Zero;
            }

            // 다른 모든 메시지는 유니티의 원래 프로시저에게 전달
            return CallWindowProc(originalWndProcPtr, hWnd, msg, wParam, lParam);
        }

        private void ProcessDroppedFiles(IntPtr hDrop)
        {
            // 드롭된 파일의 총 개수를 가져옴
            uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            var droppedFiles = new List<string>();

            // 각 파일의 경로를 가져옴
            for (uint i = 0; i < fileCount; i++)
            {
                uint pathLength = DragQueryFile(hDrop, i, null, 0) + 1;
                var sb = new System.Text.StringBuilder((int)pathLength);
                DragQueryFile(hDrop, i, sb, pathLength);
                droppedFiles.Add(sb.ToString());
            }

            // 파일 처리가 끝났음을 시스템에 알림
            DragFinish(hDrop);

            // 메인 스레드에서 UnityEvent를 호출하여 파일 리스트를 전달
            // (Windows 메시지는 다른 스레드에서 올 수 있으므로 메인 스레드 처리가 안전)
            // 여기서는 간단하게 바로 호출하지만, 복잡한 작업 시 Loom 같은 메인 스레드 디스패처 사용 권장
            OnFilesDropped.Invoke(droppedFiles);
        }
    }
}