using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Display;
using Cysharp.Threading.Tasks;
using GameSystem;
using SimpleFileBrowser;
using UnityEngine;

namespace FileSystem
{
    public class FileLoadManager : BaseManager
    {
        #region 필드 & 프로퍼티

        public BdObjectManager bdObjManager;    // BDObjectManager 참조
        public AnimObjList animObjList;         // AnimObjList (애니메이션 관련)

        /// <summary>
        /// HeadGenerator 동작이 끝날 때까지 대기할 때 활용
        /// </summary>
        public readonly HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

        /// <summary>
        /// frame.txt에서 읽은 정보 (f키 → (tick, interpolation))
        /// </summary>
        public readonly Dictionary<string, (int, int)> FrameInfo = new Dictionary<string, (int, int)>();

        private FileBrowser.Filter loadFilter = new FileBrowser.Filter("Files", ".bdengine", ".bdstudio");

        #endregion

        #region Unity 라이프사이클

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();

        }

        #endregion

        #region 메인 임포트 (UI 호출) - 파일/폴더 선택 후 async

        /// <summary>
        /// 여러 .bdengine(또는 폴더)을 임포트하는 UI 버튼. 
        /// 파일/폴더 선택 코루틴 → 비동기 임포트
        /// </summary>
        public void ImportFile()
        {
            // StartCoroutine(ShowLoadDialogCoroutine(OnFilesSelectedForMainImportAsync));
            ShowLoadDialogCoroutine().Forget();
        }

        /// <summary>
        /// FileBrowser로부터 여러 파일/폴더 선택
        /// </summary>
        private async UniTaskVoid ShowLoadDialogCoroutine()
        {
            FileBrowser.SetFilters(false, loadFilter);

            await FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.FilesAndFolders,
                allowMultiSelection: true,
                title: "Select Files",
                loadButtonText: "Load"
            ).ToUniTask();

            if (FileBrowser.Success)
            {
                var selectedPaths = FileBrowser.Result.ToList();
                // 코루틴에서 async 메서드 호출
                var ui = GameManager.GetManager<UIManager>();
                ui.SetLoadingPanel(true);
                ui.SetLoadingText("Reading and Sorting Files...");

                try
                {
                    await ImportFilesAsync(selectedPaths);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogError($"임포트 중 예외 발생: {e}");
#else
                    CustomLog.LogError("불러오던 중 에러가 발생했습니다.");
#endif
                }
                finally
                {
                    ui.SetLoadingPanel(false);
                }
            }
            else
            {
                CustomLog.Log("파일 선택 취소/실패");
            }
        }

        #endregion

        #region 메인 임포트 로직 (비동기)

        private async UniTask ImportFilesAsync(List<string> filePaths)
        {
            var ui = GameManager.GetManager<UIManager>();
            var settingManager = GameManager.Setting;

            // 1) frame.txt 파싱
            if (settingManager.UseFrameTxtFile)
            {
                await TryParseFrameTxtAsync(filePaths, FrameInfo, settingManager);
            }

            FileProcessingHelper.GetAllFileFromFolder(ref filePaths);

            // 2) f<number> 정렬
            if (settingManager.UseFrameTxtFile || settingManager.UseNameInfoExtract)
            {
                filePaths = FileProcessingHelper.SortFiles(filePaths);
            }

            if (filePaths.Count < 1)
            {
                CustomLog.Log("임포트할 파일이 없습니다.");
                return;
            }

            // 3) 첫 파일로 메인 디스플레이 생성
            ui.SetLoadingText("Making Main Display...");
            string mainName = Path.GetFileNameWithoutExtension(filePaths[0]);
            var mainAnimObject = await MakeDisplayAsync(filePaths[0], mainName);

            // 4) 나머지 파일 프레임 추가
            ui.SetLoadingText("Adding Frames...");
            for (int i = 1; i < filePaths.Count; i++)
            {
                var bdObj = await FileProcessingHelper.ProcessFileAsync(filePaths[i]);
                mainAnimObject.AddFrame(bdObj, Path.GetFileNameWithoutExtension(filePaths[i]));
            }

            // 5) HeadGenerator 대기
            ui.SetLoadingText("Waiting Head Textures...");
            try
            {
                await UniTask.WaitWhile(() => WorkingGenerators.Count > 0).Timeout(TimeSpan.FromSeconds(100));
            }
            catch (TimeoutException)
            {
                CustomLog.LogError("머리 텍스쳐 로딩 타임아웃");
            }

            mainAnimObject.UpdateAllFrameInterJump();

            FrameInfo.Clear();

            CustomLog.Log($"Import 완료! BDObject 개수: {bdObjManager.bdObjectCount}");
        }

        /// <summary>
        /// 파일 하나로 AnimObject 생성 (BdObjectManager에 등록 후 AnimObjList에 생성)
        /// </summary>
        private async UniTask<AnimObject> MakeDisplayAsync(string filePath, string fileName)
        {
            // 파일 → BDObject
            var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);

            // BdObjectManager 등록
            await bdObjManager.AddObject(bdObject, fileName);

            return animObjList.AddAnimObject(fileName);
        }

        public static async UniTask TryParseFrameTxtAsync(
            List<string> paths,
            Dictionary<string, (int, int)> frameInfo,
            SettingManager settingManager
        )
        {
            frameInfo.Clear();

            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    var frameFile = Directory.GetFiles(p, "frame.txt").FirstOrDefault();

                    if (!string.IsNullOrEmpty(frameFile))
                    {
                        // 1. 로그는 메인 스레드에서 출력
                        CustomLog.Log("Frame.txt Detected : " + frameFile);

                        // 2. 파싱은 백그라운드로
                        await UniTask.RunOnThreadPool(() =>
                        {
                            ParseFrameFile(frameFile, frameInfo, settingManager);
                        });
                    }
                }
            }
        }

        private static void ParseFrameFile(
            string frameFile,
            Dictionary<string, (int, int)> frameInfo,
            SettingManager settingManager
            )
        {
            var lines = File.ReadLines(frameFile);

            foreach (var line in lines)
            {
                var parts = line.Split(' ');

                string frameKey = null;
                int sValue = settingManager.defaultTickInterval;
                int iValue = settingManager.defaultInterpolation;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();

                    if (trimmed.StartsWith("f"))
                    {
                        frameKey = trimmed;
                    }
                    else if (trimmed.StartsWith("s") &&
                             int.TryParse(trimmed.Substring(1), out int s))
                    {
                        sValue = s;
                    }
                    else if (trimmed.StartsWith("i") &&
                             int.TryParse(trimmed.Substring(1), out int inter))
                    {
                        iValue = inter;
                    }
                }

                if (!string.IsNullOrEmpty(frameKey))
                {
                    frameInfo[frameKey] = (sValue, iValue);
                }
            }
        }

        #endregion

        #region 단일 프레임 임포트

        /// <summary>
        /// 기존 AnimObject에 프레임 하나 추가 (UI 버튼)
        /// </summary>
        public void ImportFrame(AnimObject target, int tick)
        {
            // StartCoroutine(ShowLoadDialogCoroutineForFrame(path =>
            //     OnFrameFileSelectedAsync(path, target, tick)
            // ));
            ShowLoadDialogCoroutineForFrame(target, tick).Forget();
        }

        private async UniTaskVoid ShowLoadDialogCoroutineForFrame(AnimObject target, int tick)
        {
            await FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.Files,
                allowMultiSelection: false
            ).ToUniTask();

            if (FileBrowser.Success)
            {
                var filePath = FileBrowser.Result[0];

                var ui = GameManager.GetManager<UIManager>();
                ui.SetLoadingPanel(true);

                try
                {
                    var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);
                    target.AddFrame(
                        Path.GetFileNameWithoutExtension(filePath),
                        bdObject,
                        tick,
                        GameManager.Setting.defaultInterpolation
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"프레임 임포트 오류: {e}");
                }
                finally
                {
                    ui.SetLoadingPanel(false);
                }
            }
            else
            {
                CustomLog.Log("프레임 추가용 파일 선택 취소/실패");
            }
        }

        #endregion
    }
}
