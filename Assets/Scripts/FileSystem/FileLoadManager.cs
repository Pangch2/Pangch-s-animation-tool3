using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
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

        public TagUUIDAdder tagUUIDAdder;

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
                    CustomLog.LogError($"파일 임포트 중 오류 발생: {e}");
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

        #region 메인 임포트 로직

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
            // var mainAnimObject = await MakeDisplayAsync(filePaths[0], mainName);

            // BDObject 읽은 뒤 BDObjectManager에 추가, AnimObject 추가 
            var mainBdObject = await FileProcessingHelper.ProcessFileAsync(filePaths[0]);

            bool isCorrectTag = BdObjectHelper.HasVaildID(mainBdObject);
            if (!isCorrectTag)
            {
                mainBdObject = await AskAndApplyTagUUIDAdder(filePaths[0]);

                if (mainBdObject == null)
                {
                    CustomLog.Log("태그 추가 취소됨, 임포트 중단");
                    return;
                }

            }

            await bdObjManager.AddObject(mainBdObject, mainName);
            var mainAnimObject = animObjList.AddAnimObject(mainName);

            // 4) 나머지 파일 프레임 추가
            ui.SetLoadingText("Adding Frames...");
            for (int i = 1; i < filePaths.Count; i++)
            {

                // var bdObj = await FileProcessingHelper.ProcessFileAsync(filePaths[i]);

                // if (!isCorrectTag)
                // {
                //     // 첫번째 파일에 태그가 없다면 여기에도 똑같은 방식으로 태그 추가
                //     await tagUUIDAdder.ApplyTagOrUUID(bdObj);
                // }
                var bdObj = await GetBDObject(mainAnimObject, filePaths[i]);

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
                    var bdObject = await GetBDObject(target, filePath);

                    if (bdObject == null)
                    {
                        CustomLog.Log("프레임 불러오기 취소됨");
                        return;
                    }

                    target.AddFrame(
                        Path.GetFileNameWithoutExtension(filePath),
                        bdObject,
                        tick,
                        GameManager.Setting.defaultInterpolation
                    );
                }
                catch (Exception e)
                {
                    CustomLog.UnityLogErr($"프레임 임포트 오류: {e}");
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

        private async UniTask<BdObject> GetBDObject(AnimObject target, string filePath)
        {
            var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);

            // // 태그 있는지 없는지 감지
            // var isCorrectTag = BdObjectHelper.HasVaildID(bdObject, target.tagName, target.uuidNumber);
            // if (isCorrectTag == BdObjectHelper.IDValidationResult.Mismatch)
            // {
            //     const string ADDTAGDESC_MISMATCH = "해당 오브젝트에 시작 오브젝트의 태그가 일치하지 않습니다.\n태그를 자동으로 추가하거나 무시하고 넣을 수 있습니다.\n(UUID의 경우 대체됩니다)";
            //     bool checkApply = await GameManager.GetManager<UIManager>().SetPopupPanelAsync(
            //         ADDTAGDESC_MISMATCH,
            //         target.tagName + (target.uuidNumber > 0 ? ", uuid:" + target.uuidNumber.ToString() : "")
            //     );

            //     if (checkApply)
            //     {
            //         tagUUIDAdder.TagName = target.tagName;
            //         tagUUIDAdder.uuidStartNumber = target.uuidNumber;

            //         if (target.uuidNumber > 0)
            //         {
            //             tagUUIDAdder.AddType = TagUUIDAdder.ADDTYPE.UUID;
            //         }
            //         else
            //         {
            //             tagUUIDAdder.AddType = TagUUIDAdder.ADDTYPE.TAG;
            //         }

            //         tagUUIDAdder.IsReplacingTag = false;
            //         await tagUUIDAdder.ApplyTagOrUUID(bdObject, false);
            //     }
            //     else
            //     {
            //         CustomLog.Log("태그 추가 취소됨");
            //     }
            // }
            // else if (isCorrectTag == BdObjectHelper.IDValidationResult.NoID)
            // {
            //     const string ADDTAGDESC_NOID = "해당 오브젝트에 태그가 없습니다.\n태그를 자동으로 추가하거나 불러오기를 취소합니다.\n";
            //     bool checkApply = await GameManager.GetManager<UIManager>().SetPopupPanelAsync(
            //         ADDTAGDESC_NOID,
            //         target.tagName + (target.uuidNumber > 0 ? ", uuid:" + target.uuidNumber.ToString() : "")
            //     );

            //     if (checkApply)
            //     {
            //         tagUUIDAdder.TagName = target.tagName;
            //         tagUUIDAdder.uuidStartNumber = target.uuidNumber;

            //         if (target.uuidNumber > 0)
            //         {
            //             tagUUIDAdder.AddType = TagUUIDAdder.ADDTYPE.UUID;
            //         }
            //         else
            //         {
            //             tagUUIDAdder.AddType = TagUUIDAdder.ADDTYPE.TAG;
            //         }

            //         tagUUIDAdder.IsReplacingTag = false;
            //         await tagUUIDAdder.ApplyTagOrUUID(bdObject, false);
            //     }
            //     else
            //     {
            //         CustomLog.Log("프레임 불러오기 취소됨");
            //         return null;
            //     }
            // }
            return bdObject;
        }

        #endregion

        #region 태그/UUID 추가

        async UniTask<BdObject> AskAndApplyTagUUIDAdder(string path)
        {
            // 1) CompletionSource 생성
            var tcs = new UniTaskCompletionSource<BdObject>();

            // 2) 이벤트 핸들러 정의 (로컬 함수로 캡쳐)
            void Handler(BdObject obj)
            {
                // 결과 세팅
                tcs.TrySetResult(obj);
                // 메모리 누수 방지를 위해 구독 해제
                tagUUIDAdder.OnBDObjectEdited -= Handler;
            }

            // 3) 편집 완료 이벤트 구독
            tagUUIDAdder.OnBDObjectEdited += Handler;

            // 4) 패널 띄우기
            tagUUIDAdder.SetFilePath(path);
            tagUUIDAdder.SetPanelActive(true);

            async UniTaskVoid UserCancelDetection()
            {
                await UniTask.WaitUntil(() => tagUUIDAdder.gameObject.activeSelf == false);

                tcs.TrySetCanceled();
                tagUUIDAdder.OnBDObjectEdited -= Handler;
            }

            UserCancelDetection().Forget();

            // 5) 편집 완료 이벤트가 발생할 때까지 대기
            try
            {
                BdObject editedObject = await tcs.Task;
                tagUUIDAdder.SetPanelActive(false);
                return editedObject;
            }
            catch (OperationCanceledException)
            {
                tagUUIDAdder.SetPanelActive(false);
                return null;
            }
        }

        #endregion
    }
}
