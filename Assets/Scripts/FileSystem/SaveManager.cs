using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Cysharp.Threading.Tasks;
using GameSystem;
using Newtonsoft.Json;
using SimpleFileBrowser;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace FileSystem
{
    public class SaveManager : BaseManager
    {
        public const string MDEFileExtension = ".mcdeanim";
        public string MDEFilePath = string.Empty;

        private FileBrowser.Filter saveFilter = new FileBrowser.Filter("Files", MDEFileExtension);
        public BdObjectManager bdObjManager;// BDObjectManager 참조
        FileLoadManager fileLoadManager; // FileLoadManager 참조
        public AnimObjList animObjList => fileLoadManager.animObjList; // AnimObjList (애니메이션 관련)
        public HashSet<HeadGenerator> WorkingGenerators => fileLoadManager.WorkingGenerators; // FileLoadManager에서 이동

        public MCDEANIMFile currentMDEFile;
        public bool IsNoneSaved { get; private set; } = true;
        public bool SavingProgressStatus = false;

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();
            fileLoadManager = GameManager.GetManager<FileLoadManager>();
        }

        public void MakeNewMDEFile(string name)
        {
            currentMDEFile = new MCDEANIMFile
            {
                name = name,
                version = Application.version,
            };
        }

        public void SetMCDEFilePath(string path)
        {
            MDEFilePath = path;
            IsNoneSaved = false;

            string fileName = Path.GetFileNameWithoutExtension(MDEFilePath);
            GameManager.GetManager<MenuBarManager>().SetCurrentFileText(fileName);
            currentMDEFile.name = fileName;
        }

        #region Save Logic
        // MDE 파일 업데이트하고 저장.
        public void SaveMCDEFile()
        {
            currentMDEFile.UpdateInfo(GameManager.GetManager<AnimObjList>().animObjects);
            // Serialize currentMDEFile to JSON and save to the specified path
            // string json = JsonConvert.SerializeObject(currentMDEFile, Formatting.Indented);
            // string fullPath = System.IO.Path.Combine(path, currentMDEFile.name + ".mde");
            // System.IO.File.WriteAllText(MDEFilePath, json);

            if (SavingProgressStatus) return; // 저장 중이면 추가 저장 불가

            SaveObjectCompressedAsync(currentMDEFile, MDEFilePath).Forget();

            // var json = JsonConvert.SerializeObject(currentMDEFile);
            // Debug.Log(json);
            // var bytes = Encoding.UTF8.GetBytes(json);

            // using (var fs = new FileStream(MDEFilePath, FileMode.Create))
            // using (var gzip = new GZipStream(fs, CompressionLevel.Optimal))
            // {
            //     gzip.Write(bytes, 0, bytes.Length);
            // }

            // CustomLog.Log($"Saved MDE file to: {MDEFilePath}");
        }


        /// <summary>
        /// 객체를 Brotli 압축된 JSON 파일로 비동기적으로 저장합니다 (Newtonsoft.Json 사용).
        /// 실제 작업은 백그라운드 스레드에서 수행됩니다.
        /// </summary>
        /// <param name="dataToSave">저장할 객체</param>
        /// <param name="filePath">저장할 파일 경로 (확장자 포함, 예: "saveData.json.br")</param>
        /// <param name="settings">JSON 직렬화 설정 (선택 사항)</param>
        /// <returns></returns>
        public async UniTaskVoid SaveObjectCompressedAsync(object dataToSave, string filePath, JsonSerializerSettings settings = null)
        {
            try
            {
                SavingProgressStatus = true;
#if UNITY_EDITOR
                string json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                Debug.Log($"Serialized JSON: {json}"); // 직렬화된 JSON 로그
#endif
                // 동기적인 파일 쓰기 및 직렬화 로직을 Task.Run으로 감싸서 비동기 실행
                await UniTask.RunOnThreadPool(() =>
                {
                    // 이 블록 안의 코드는 백그라운드 스레드에서 실행됩니다.
                    JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    using (BrotliStream brotliStream = new BrotliStream(fileStream, CompressionLevel.Optimal))
                    using (StreamWriter streamWriter = new StreamWriter(brotliStream, Encoding.UTF8))
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        if (settings?.Formatting == Formatting.Indented)
                        {
                            jsonWriter.Formatting = Formatting.Indented;
                        }
                        serializer.Serialize(jsonWriter, dataToSave);
                    } // using 블록 종료 시 자동으로 닫힘
                });
                // Task.Run 내부에서 예외가 발생하면 아래 catch 블록으로 전달됩니다.
                CustomLog.Log($"파일 저장 성공: {filePath}");
            }
            catch (Exception ex)
            {
                // Task.Run에서 발생한 예외 또는 그 외 예외 처리
                CustomLog.LogError($"파일 저장 실패: {filePath}\nError: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SavingProgressStatus = false;
            }
        }


        //public void SaveMDEFile() => SaveMDEFile(MDEFilePath);
        // public void SaveAsNewFile() => StartCoroutine(SaveAsNewFileCotoutine());
        public void SaveAsNewFile() => SaveAsNewFileCoroutine().Forget(); // UniTask로 변경
        private async UniTaskVoid SaveAsNewFileCoroutine()
        {
            FileBrowser.SetFilters(false, saveFilter);
            await FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, null, currentMDEFile.name).ToUniTask();

            if (FileBrowser.Success)
            {
                // Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetMCDEFilePath(FileBrowser.Result[0]);
                SaveMCDEFile();
            }
        }
        #endregion

        #region  Load Logic

        // public void LoadMCDEFile() => StartCoroutine(LoadMDEFileCoroutine());
        public void LoadMCDEFile()
        {
            if (SavingProgressStatus) return; // 저장/로드 중이면 추가 로드 불가
            LoadMDEFileCoroutine().Forget(); // UniTask로 변경
        }
        private async UniTaskVoid LoadMDEFileCoroutine()
        {
            SavingProgressStatus = true;
            FileBrowser.SetFilters(false, saveFilter); // MDE 파일 필터
            await FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, "Select MDE File").ToUniTask(); // 파일 선택 모드

            if (!FileBrowser.Success)
            {
                CustomLog.Log("MDE 파일 선택 취소/실패");
                return; // 파일 선택 취소 시
            }

            string filePath = FileBrowser.Result[0];
            CustomLog.Log($"선택된 MDE 파일: {filePath}");

            var ui = GameManager.GetManager<UIManager>();
            ui.SetLoadingPanel(true);
            ui.SetLoadingText("Loading MDE File...");

            // 1. 파일 로드 (비동기)
            // Task<MCDEANIMFile> loadTask = LoadObjectCompressedAsync<MCDEANIMFile>(filePath);
            // yield return new WaitUntil(() => loadTask.IsCompleted);
            currentMDEFile = await LoadObjectCompressedAsync<MCDEANIMFile>(filePath); // UniTask로 변경
            if (currentMDEFile == default)
            {
                CustomLog.LogError($"MDE 파일 로드 실패: {filePath}");
                ui.SetLoadingPanel(false);
                return;
            }

            SetMCDEFilePath(filePath); // MDE 파일 경로 설정

            // 2. 로드된 데이터 처리 (비동기)
            CustomLog.Log("MDE 파일 로드 완료. 데이터 처리 시작...");
            ui.SetLoadingText("Processing MDE Data..."); // 로딩 텍스트 변경
            try
            {
                await ProcessLoadedMDEFileAsync(currentMDEFile);
            }
            catch (Exception e) // ProcessLoadedMDEFileAsync 호출 자체에서 예외 발생 시
            {
                Debug.LogError($"파일 로드 중 예외: {e.Message}, {e.StackTrace}");
            }
            finally
            {
                // 3. 로딩 패널 닫기
                ui.SetLoadingPanel(false);
                SavingProgressStatus = false;
            }

        }

        /// <summary>
                /// 로드된 MCDEANIMFile 데이터를 기반으로 AnimObject와 Frame들을 생성합니다.
                /// </summary>
                /// <param name="mdeFile">파일로부터 로드된 MCDEANIMFile 객체</param>
        private async UniTask ProcessLoadedMDEFileAsync(MCDEANIMFile mdeFile)
        {
            if (mdeFile == null || mdeFile.animObjects == null || mdeFile.animObjects.Count == 0)
            {
                CustomLog.LogWarning("처리할 애니메이션 객체 데이터가 없습니다.");
                return;
            }

            var ui = GameManager.GetManager<UIManager>(); // UIManager 참조 가져오기
            ui.SetLoadingText("Processing Animation Objects...");

            // MCDEANIMFile 내의 모든 AnimObjectFile 순회
            foreach (var animObjectFile in mdeFile.animObjects)
            {
                if (animObjectFile.frameFiles == null || animObjectFile.frameFiles.Count == 0)
                {
                    CustomLog.LogWarning($"AnimObject '{animObjectFile.name}'에 프레임 데이터가 없습니다. 건너뜁니다.");
                    continue; // 다음 AnimObjectFile로
                }

                CustomLog.Log($"Processing AnimObject: {animObjectFile.name}");
                ui.SetLoadingText($"Processing: {animObjectFile.name}");

                // 1) 첫 번째 프레임으로 메인 AnimObject 생성
                var firstFrameFile = animObjectFile.frameFiles[0];
                BdObjectHelper.SetParent(null, firstFrameFile.bdObject);
                if (firstFrameFile.bdObject == null)
                {
                    CustomLog.LogError($"AnimObject '{animObjectFile.name}'의 첫 프레임 '{firstFrameFile.name}'에 BdObject 데이터가 없습니다. 건너뜁니다.");
                    continue;
                }

                // BdObjectManager에 등록하고 AnimObjList에서 AnimObject 생성
                // MakeDisplayAsync와 유사한 로직 수행
                await bdObjManager.AddObject(firstFrameFile.bdObject, animObjectFile.name);
                AnimObject currentRuntimeAnimObject = animObjList.AddAnimObject(animObjectFile.name);
                // 첫번째 프레임은 자동 추가 

                // 2) 나머지 프레임들 추가
                ui.SetLoadingText($"Adding Frames for {animObjectFile.name}...");
                for (int i = 1; i < animObjectFile.frameFiles.Count; i++)
                {
                    var currentFrameFile = animObjectFile.frameFiles[i];
                    BdObjectHelper.SetParent(null, currentFrameFile.bdObject);
                    if (currentFrameFile.bdObject == null)
                    {
                        CustomLog.LogWarning($"AnimObject '{animObjectFile.name}'의 프레임 '{currentFrameFile.name}'에 BdObject 데이터가 없습니다. 건너뜁니다.");
                        continue;
                    }

                    // AnimObject에 프레임 추가 (FrameFile에 저장된 tick, interpolation 사용)
                    currentRuntimeAnimObject.AddFrame(
                               currentFrameFile.name,
                               currentFrameFile.bdObject,
                               currentFrameFile.tick,
                               currentFrameFile.interpolation
                             );
                }

                // 3) 해당 AnimObject의 프레임 처리 완료 후 필요한 업데이트 호출
                currentRuntimeAnimObject.UpdateAllFrameInterJump(); // 각 AnimObject 처리가 끝날 때 호출

                CustomLog.Log($"Finished processing AnimObject: {animObjectFile.name}");
            }

            // 4) 모든 AnimObject 처리 후 HeadGenerator 대기 (필요한 경우)
            if (WorkingGenerators.Count > 0)
            {
                ui.SetLoadingText("Waiting Head Textures...");
                await UniTask.WaitUntil(() => WorkingGenerators.Count == 0).Timeout(TimeSpan.FromSeconds(100)); // 모든 HeadGenerator가 완료될 때까지 대기
                // while (WorkingGenerators.Count > 0)
                // {
                //     // Debug.Log($"Waiting for {WorkingGenerators.Count} head generators..."); // 디버그 로그
                //     await UniTask.Delay(500); // 0.5초 대기
                // }
            }

            // FrameInfo는 MDE 파일을 로드할 때는 사용하지 않으므로 비울 필요 없음 (이미 비어있거나 사용되지 않음)
            // FrameInfo.Clear();

            GameManager.Setting.LoadMCDEAnim(currentMDEFile);

            CustomLog.Log($"MDE File Processing 완료! BDObject 개수: {bdObjManager.bdObjectCount}");
        }

        /// <summary>
        /// Brotli 압축된 JSON 파일을 읽어 객체로 비동기적으로 역직렬화합니다 (Newtonsoft.Json 사용).
        /// 실제 작업은 Task.Run을 통해 백그라운드 스레드에서 수행됩니다.
        /// </summary>
        /// <typeparam name="T">역직렬화할 객체의 타입</typeparam>
        /// <param name="filePath">읽어올 파일 경로</param>
        /// <param name="settings">JSON 역직렬화 설정 (선택 사항)</param>
        /// <returns>역직렬화된 객체를 담은 Task<T>. 실패 시 default(T) 반환.</returns>
        public static async UniTask<T> LoadObjectCompressedAsync<T>(string filePath, JsonSerializerSettings settings = null)
        {
            // 파일 존재 여부는 동기적으로 먼저 확인 (IO 작업 전에 빠른 실패)
            if (!File.Exists(filePath))
            {
                CustomLog.LogError($"파일을 찾을 수 없습니다: {filePath}");
                return default;
            }

            try
            {
                // 동기적인 파일 읽기/압축해제/역직렬화 로직을 Task.Run으로 감싸서 비동기 실행
                T loadedObject = await UniTask.RunOnThreadPool(() =>
                {
                    // 이 블록 안의 코드는 백그라운드 스레드에서 실행됩니다.
                    JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (BrotliStream brotliStream = new BrotliStream(fileStream, CompressionMode.Decompress))
                    using (StreamReader streamReader = new StreamReader(brotliStream, Encoding.UTF8))
                    using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                    {
                        // 객체로 역직렬화
                        return serializer.Deserialize<T>(jsonReader);
                    } // using 블록 종료 시 자동으로 닫힘
                });

                CustomLog.Log($" 파일 로드 성공: {filePath}");
                return loadedObject;
            }
            catch (Exception ex)
            {
                // Task.Run에서 발생한 예외 또는 그 외 예외 처리
                CustomLog.LogError($"파일 로드 실패 : {filePath}\nError: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }
        #endregion
    }
}
