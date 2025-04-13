using System.Collections;
using System.Linq;
using GameSystem;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.IO;
using System;
using Animation.AnimFrame;
using System.Collections.Generic;
using System.Text;

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

        SettingManager settingManager;

        private void Start()
        {
            SetPathText(Application.dataPath);
            settingManager = GameManager.Setting;
        }

        private void SetPathText(string path)
        {
            Debug.Log($"Export path: {path}");
            currentPath = path.Replace("\\", "/");
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
            ExportFile().Forget(e => CustomLog.LogError($"Export failed: {e.Message}"));
        }

        #region Export Logic

       public async UniTask ExportFile()
        {
            // 1. Export path 설정: 폴더가 없으면 생성
            if (!Directory.Exists(finalPath))
            {
                Directory.CreateDirectory(finalPath);
            }
            
            // 2. 폴더 내 기존 파일 삭제
            DeleteFrameAndFnumberFiles(finalPath);

            // 3. AnimObject의 모든 ExportFrame 데이터 가져오기
            SortedList<int, ExportFrame> allFrames = GameManager.GetManager<AnimObjList>().GetAllFrames();
            
            // 4. 각 ExportFrame별로 mcfunction 명령어 생성 및 저장
            foreach (var frame in allFrames.Values)
            {
                
            }

            // 5. 모든 ExportFrame의 tick 값을 모아, scoreboard 흐름을 위한 frame.mcfunction 생성
            List<string> scoreLines = new List<string>();
            int startScore = settingManager.startTick; // 시작 스코어
            string nameSpace = settingManager.packNamespace; // 네임스페이스
            if (!nameSpace.Contains(":"))
            {
                nameSpace += ':';
            }
            else if (!nameSpace.EndsWith("/"))
            {
                nameSpace += "/";
            }

            // 모든 ExportFrame의 tick은 sorted되어 있다고 가정
            var sortedTicks = allFrames.Keys;
            int cnt = 0;
            foreach (var tick in sortedTicks)
            {
                string scoreLine = $"execute if score {settingManager.fakePlayer} {settingManager.scoreboardName} matches {startScore + tick} run function {nameSpace}f{cnt++}";
                scoreLines.Add(scoreLine);
            }

            string scoreFile = Path.Combine(finalPath, 
                string.IsNullOrWhiteSpace(settingManager.frameFileName) ? "frame.mcfunction" : $"{settingManager.frameFileName}.mcfunction");
            await File.WriteAllLinesAsync(scoreFile, scoreLines, Encoding.UTF8);

            // 약간의 딜레이 후 Export 완료 로그
            CustomLog.Log($"Export is Done! Export path: {finalPath}");
        }

        public void DeleteFrameAndFnumberFiles(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.mcfunction");
            
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                // Check if the file name matches the frame file name or is f{number} pattern
                if (fileName.Equals(settingManager.frameFileName + ".mcfunction", StringComparison.OrdinalIgnoreCase) || RegexPatterns.FNumberRegex.IsMatch(fileName))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        CustomLog.LogError($"삭제 실패: {fileName} - {ex.Message}");
                    }
                }
            }
        }

        // Helper: Matrix를 문자열로 포맷한다.
        private string FormatTransformation(in Matrix4x4 matrix, int interpolation)
        {
            float[] elements = new float[16];
            // Row-major 순서로 추출 (예: m00, m01, m02, m03, m10, m11, ... )
            elements[0] = matrix.m00; elements[1] = matrix.m01; elements[2] = matrix.m02; elements[3] = matrix.m03;
            elements[4] = matrix.m10; elements[5] = matrix.m11; elements[6] = matrix.m12; elements[7] = matrix.m13;
            elements[8] = matrix.m20; elements[9] = matrix.m21; elements[10] = matrix.m22; elements[11] = matrix.m23;
            elements[12] = matrix.m30; elements[13] = matrix.m31; elements[14] = matrix.m32; elements[15] = matrix.m33;
            // 각 원소를 소수점 4자리로 반올림하여 "f" 접미사를 붙임
            string joined = string.Join(",", elements.Select(x => Math.Round(x, 4).ToString() + "f"));
            return $"{{interpolation_duration: {interpolation}, transformation:[{joined}]}}";
        }
        #endregion
    }
}