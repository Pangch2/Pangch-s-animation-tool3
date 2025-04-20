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
using BDObjectSystem;
using Minecraft;

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
        public string currentPath;
        private string finalPath;

        SettingManager settingManager;

        private void Start()
        {
            SetPathText(Directory.GetParent(Application.dataPath).FullName);
            settingManager = GameManager.Setting;
        }

        public void SetPathText(string path)
        {
            Debug.Log($"Export path: {path}");
            currentPath = path.Replace("\\", "/");
            if (currentPath.EndsWith(exportFolder))
            {
                finalPath = currentPath;
            }
            else
            {
                finalPath = path + '/' + exportFolder;
            }
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
            // 1. Export path 설정
            if (!Directory.Exists(finalPath))
                Directory.CreateDirectory(finalPath);

            // 2. 폴더 내 기존 파일 삭제
            DeleteFrameAndFnumberFiles(finalPath);

            // 3. 트랙별 데이터 가져오기
            List<SortedList<int, ExportFrame>> allTracks =
                GameManager.GetManager<AnimObjList>().GetAllFrames();

            // 결과 누적: tick → 명령어 리스트
            var result = new SortedList<int, List<string>>();
            var utf8NoBom = new UTF8Encoding(false);

            foreach (var track in allTracks)
            {
                if (track.Count == 0)
                    continue;

                // ── 3‑1. 마지막 프레임으로 시드 세팅 ──
                var keys = track.Keys.ToList();
                int lastTick = keys[^1];
                var lastFrame = track[lastTick];

                // 이전 상태 버퍼 초기화
                var prevTransforms = new Dictionary<string, Matrix4x4>();
                var prevHeadTex = new Dictionary<string, string>();
                var prevEntities = new HashSet<string>();

                // 마지막 프레임을 ‘이전 상태’로 복사
                foreach (var kv in lastFrame.NodeDict)
                {
                    prevEntities.Add(kv.Key);
                    prevTransforms[kv.Key] = kv.Value.Item2;
                    string tex = ExtractTextureValue(kv.Value.Item1);
                    if (!string.IsNullOrEmpty(tex))
                        prevHeadTex[kv.Key] = tex;
                }

                // ── 3‑2. 오름차순 한번만 순회 ──
                foreach (int tick in keys)
                {
                    var frame = track[tick];

                    // 현재 상태용 새 컬렉션
                    var currTransforms = new Dictionary<string, Matrix4x4>();
                    var currHeadTex = new Dictionary<string, string>();
                    var currEntities = new HashSet<string>();
                    var cmds = new List<string>();

                    // Diff & Command 생성
                    foreach (var kv in frame.NodeDict)
                    {
                        string id = kv.Key;
                        BdObject obj = kv.Value.Item1;
                        Matrix4x4 xf = kv.Value.Item2;
                        string tex = ExtractTextureValue(obj);

                        currEntities.Add(id);
                        currTransforms[id] = xf;
                        if (!string.IsNullOrEmpty(tex))
                            currHeadTex[id] = tex;

                        bool existed = prevEntities.Contains(id);
                        bool tChanged = true, texChanged = true;

                        if (existed)
                        {
                            if (prevTransforms.TryGetValue(id, out var pxf))
                                tChanged = !MatricesAreEqual(xf, pxf);
                            prevHeadTex.TryGetValue(id, out var ptex);
                            texChanged = tex != ptex;
                        }

                        if (!tChanged && !texChanged)
                            continue;

                        // NBT 파트 조합
                        var parts = new List<string>();
                        if (tChanged)
                        {
                            string p = FormatTransformation(xf, kv.Value.Item3)
                                       .Trim('{', '}');
                            parts.Add(p);
                        }
                        if (texChanged && !string.IsNullOrEmpty(tex))
                        {
                            string itemNbt = FormatItemNbt(tex);
                            if (!string.IsNullOrEmpty(itemNbt))
                                parts.Add("item:" + itemNbt.Substring("item:".Length));
                        }

                        if (parts.Count > 0)
                        {
                            string combined = "{" + string.Join(",", parts) + "}";
                            string cmd = GenerateCommand(id, obj, combined, settingManager.useFindMode);
                            if (!string.IsNullOrEmpty(cmd))
                                cmds.Add(cmd);
                        }
                    }

                    if (cmds.Count == 0)
                        cmds.Add("# No changes in this frame");

                    // 결과 집계
                    if (!result.ContainsKey(tick))
                        result[tick] = new List<string>();
                    result[tick].AddRange(cmds);

                    // 이전 상태 업데이트 (복사본)
                    prevTransforms = new Dictionary<string, Matrix4x4>(currTransforms);
                    prevHeadTex = new Dictionary<string, string>(currHeadTex);
                    prevEntities = new HashSet<string>(currEntities);
                }
            }

            // 4. 개별 fN.mcfunction 파일 생성
            int idx = 1;
            foreach (var kv in result)
            {
                string path = Path.Combine(finalPath, $"f{idx++}.mcfunction");
                await File.WriteAllLinesAsync(path, kv.Value, utf8NoBom);
            }

            // 5. frame.mcfunction (scoreboard) 생성
            var scoreLines = new List<string>();
            string ns = settingManager.packNamespace;
            if (!string.IsNullOrWhiteSpace(ns) && !ns.Contains(":"))
                ns += ":";
            for (int i = 0; i < result.Count; i++)
            {
                int scoreVal = settingManager.startTick + result.Keys[i];
                scoreLines.Add(
                    $"execute if score {settingManager.fakePlayer} {settingManager.scoreboardName} matches {scoreVal} run function {ns}f{i + 1}"
                );
            }

            string scoreFile = string.IsNullOrWhiteSpace(settingManager.frameFileName)
                                 ? "frame.mcfunction"
                                 : $"{settingManager.frameFileName}.mcfunction";
            await File.WriteAllLinesAsync(
                Path.Combine(finalPath, scoreFile), scoreLines, utf8NoBom);

            CustomLog.Log($"Export is Done! Export path: {finalPath}");
        }


        /*
                public async UniTask ExportFile()
                {
                    // 1. Export path 설정: 폴더가 없으면 생성
                    if (!Directory.Exists(finalPath))
                    {
                        Directory.CreateDirectory(finalPath);
                    }

                    // 2. 폴더 내 기존 파일 삭제
                    DeleteFrameAndFnumberFiles(finalPath);

                    // 3. AnimObject의 모든 ExportFrame 데이터 가져오기 (SortedList는 Key로 자동 정렬됨)
                    List<SortedList<int, ExportFrame>> allFrames = GameManager.GetManager<AnimObjList>().GetAllFrames();

                    // 상태 초기화
                    previousTransforms.Clear();
                    previousHeadTextures.Clear();
                    entitiesInPreviousFrame.Clear();

                    bool isFirstFrame = true; // 첫 프레임 여부 확인

                    // 4. 각 ExportFrame별로 mcfunction 명령어 생성 및 저장

                    var result = new SortedList<int, List<string>>();

                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    foreach (var track in allFrames)
                        for (int i = -1; i < track.Count; i++)
                        {
                            int tick;
                            if (i == -1) tick = track.Keys[^1];
                            else tick = track.Keys[i]; // 현재 프레임의 Tick 값

                            ExportFrame frame = track[tick];

                            // 현재 프레임 상태 저장용
                            Dictionary<string, Matrix4x4> currentTransforms = new Dictionary<string, Matrix4x4>();
                            Dictionary<string, string> currentHeadTextures = new Dictionary<string, string>();
                            HashSet<string> entitiesInCurrentFrame = new HashSet<string>();

                            List<string> commandsForThisFrame = new List<string>();

                            foreach (var kvp in frame.NodeDict) // kvp: KeyValuePair<string, (BdObject, Matrix4x4)>
                            {
                                string entityId = kvp.Key; // entityId는 BdObject의 고유 식별자여야 함 (예: UUID 또는 커스텀 태그 조합)
                                BdObject entity = kvp.Value.Item1;
                                Matrix4x4 worldTransform = kvp.Value.Item2;

                                // 현재 프레임에 존재하는 엔티티 기록
                                entitiesInCurrentFrame.Add(entityId);

                                // 현재 상태 저장
                                currentTransforms[entityId] = worldTransform;
                                string currentTexture = ExtractTextureValue(entity); // 텍스처 값 추출
                                if (!string.IsNullOrEmpty(currentTexture)) // 텍스처가 있을 경우에만 저장
                                {
                                    currentHeadTextures[entityId] = currentTexture;
                                }

                                // --- 프레임 비교 (Differencing) 로직 ---
                                bool existsPreviously = entitiesInPreviousFrame.Contains(entityId);
                                bool transformChanged = true;
                                bool textureChanged = true;

                                if (existsPreviously)
                                {
                                    // Transformation 비교
                                    if (previousTransforms.TryGetValue(entityId, out Matrix4x4 prevTransform))
                                    {
                                        transformChanged = !MatricesAreEqual(worldTransform, prevTransform);
                                    }
                                    else // 이전 프레임엔 있었지만 transform 정보가 없었던 경우 (거의 없을 상황)
                                    {
                                        transformChanged = true;
                                    }

                                    // Texture 비교
                                    previousHeadTextures.TryGetValue(entityId, out string prevTexture); // 없으면 null 반환
                                    textureChanged = currentTexture != prevTexture; // null과 비교해도 안전
                                }

                                // 첫 프레임은 무조건 모든 데이터 포함 (Python 로직과 유사하게)
                                if (!isFirstFrame && !transformChanged && !textureChanged)
                                {
                                    // 변경 사항 없으면 이 엔티티는 이번 프레임 파일에 추가 안함
                                    continue;
                                }
                                // --- 프레임 비교 끝 ---


                                // --- 명령어 생성 ---
                                List<string> nbtParts = new List<string>();

                                // Transformation NBT 추가 (변경되었거나 첫 프레임일 경우)
                                if (transformChanged || isFirstFrame)
                                {
                                    // Matrix4x4와 interpolation 값을 전달
                                    string transformationNbt = FormatTransformation(worldTransform, kvp.Value.Item3);
                                    // transformationNbt는 이제 "{interpolation_duration:...,transformation:...}" 형태
                                    // 중괄호 제거하고 내용만 추출
                                    transformationNbt = transformationNbt.Trim('{', '}');
                                    nbtParts.Add(transformationNbt); // "interpolation_duration:..., transformation:..."
                                }

                                // Item (Head Texture) NBT 추가 (변경되었거나 첫 프레임일 경우, 그리고 텍스처가 존재할 경우)
                                if ((textureChanged || isFirstFrame) && !string.IsNullOrEmpty(currentTexture))
                                {
                                    string itemNbt = FormatItemNbt(currentTexture); // itemNbt는 "item:{...}" 형태
                                    if (!string.IsNullOrEmpty(itemNbt))
                                    {
                                        // "item:" 부분 제거하고 내용만 추가
                                        itemNbt = itemNbt.Substring("item:".Length); // "{id:..., components:...}"
                                        nbtParts.Add("item:" + itemNbt); // "item:{id:..., components:...}" 형태로 다시 조합
                                    }
                                }


                                // NBT 데이터가 있는 경우에만 명령어 생성
                                if (nbtParts.Count > 0)
                                {
                                    // NBT 부분을 합쳐서 { , , } 형태로 만듦
                                    string combinedNbt = "{" + string.Join(",", nbtParts) + "}";

                                    string command = GenerateCommand(entityId, entity, combinedNbt, settingManager.useFindMode); // NBT 결합된 문자열 전달
                                    if (!string.IsNullOrEmpty(command))
                                    {
                                        commandsForThisFrame.Add(command);
                                    }
                                }
                                // --- 명령어 생성 끝 ---
                            }

                            // if (commandsForThisFrame.Count == 0)
                            // {
                            //     commandsForThisFrame.Add($"# No changes in this frame");
                            // }


                            // // 파일 이름 생성 시 frame index 사용 (Python의 f{number} 형식 유지)
                            // int fileIndex = i + 1;
                            // if (i == -1) fileIndex = track.Count;
                            // string frameFilePath = Path.Combine(finalPath, $"f{fileIndex}.mcfunction");
                            // await File.WriteAllLinesAsync(frameFilePath, commandsForThisFrame, utf8NoBom);
                            if (isFirstFrame == false)
                            {
                                if (result.ContainsKey(tick))
                                {
                                    result[tick].AddRange(commandsForThisFrame); // tick이 이미 존재하면 추가
                                }
                                else
                                {
                                    result.Add(tick, commandsForThisFrame); // tick을 Key로 사용하여 명령어 리스트 저장
                                }
                            }


                            // 다음 프레임 비교를 위해 현재 상태를 이전 상태로 업데이트
                            previousTransforms = currentTransforms;
                            previousHeadTextures = currentHeadTextures;
                            entitiesInPreviousFrame = entitiesInCurrentFrame;
                            isFirstFrame = false; // 첫 프레임 처리 완료
                        }

                    // 파일 생성
                    for (int i = 0; i < result.Count; i++)
                    {
                        int tick = result.Keys[i]; // Tick 값
                        List<string> commands = result[tick]; // 해당 Tick에 대한 명령어 리스트
                        // Debug.Log($"Exporting frame {i + 1} with tick {tick}");

                        // 파일 이름 생성 시 tick 사용 (Python의 f{number} 형식 유지)
                        string frameFilePath = Path.Combine(finalPath, $"f{i + 1}.mcfunction");
                        await File.WriteAllLinesAsync(frameFilePath, commands, utf8NoBom);
                    }


                    // 5. 모든 ExportFrame의 tick 값을 모아, scoreboard 흐름을 위한 frame.mcfunction 생성
                    List<string> scoreLines = new List<string>();
                    int startScore = settingManager.startTick; // 시작 스코어
                    string nameSpace = settingManager.packNamespace; // 네임스페이스

                    // 네임스페이스 형식 정리 (끝에 : 또는 / 추가)
                    if (!string.IsNullOrEmpty(nameSpace)) // 네임스페이스가 있을 경우에만 처리
                    {
                        if (!nameSpace.Contains(":"))
                        {
                            nameSpace += ':'; // 기본적으로 ':' 추가
                        }
                    }
                    else // 네임스페이스가 없으면 빈 문자열로 처리 (오류 방지)
                    {
                        nameSpace = "";
                    }


                    // Python 코드에서는 score 증가분을 s값으로 조절했지만, 여기서는 ExportFrame의 Tick 값을 직접 사용
                    // 함수 이름도 f0, f1, f2... 순서대로 생성 (allFrames의 순서와 동일)
                    for (int i = 0; i < result.Count; i++)
                    {
                        int tickValue = result.Keys[i]; // 해당 프레임의 실제 Tick 값
                        int scoreToCheck = startScore + tickValue; // 시작 스코어 + 프레임 Tick 값
                        string functionName = $"{nameSpace}f{i + 1}"; // f0, f1, f2...
                        string scoreLine = $"execute if score {settingManager.fakePlayer} {settingManager.scoreboardName} matches {scoreToCheck} run function {functionName}";
                        scoreLines.Add(scoreLine);
                    }

                    string scoreFileName = string.IsNullOrWhiteSpace(settingManager.frameFileName) ? "frame.mcfunction" : $"{settingManager.frameFileName}.mcfunction";
                    string scoreFileFullPath = Path.Combine(finalPath, scoreFileName);
                    await File.WriteAllLinesAsync(scoreFileFullPath, scoreLines, utf8NoBom);

                    // 완료 로그
                    CustomLog.Log($"Export is Done! Export path: {finalPath}");
                    // Debug.Log($"Export is Done! Export path: {finalPath}"); // Unity 기본 로그 사용 예시
                }
                */

        // --- 기존 및 추가된 Helper 함수들 ---

        public void DeleteFrameAndFnumberFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return; // 폴더 없으면 삭제할 것도 없음

            string[] files = Directory.GetFiles(folderPath, "*.mcfunction");
            string frameFileNameToDelete = string.IsNullOrWhiteSpace(settingManager.frameFileName) ? "frame.mcfunction" : $"{settingManager.frameFileName}.mcfunction";

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                // frame 파일 이름 또는 f{number}.mcfunction 패턴 확인
                if (fileName.Equals(frameFileNameToDelete, StringComparison.OrdinalIgnoreCase) || RegexPatterns.FNumberRegex.IsMatch(fileName))
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

        /// <summary>
        /// Matrix를 문자열로 포맷
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="interpolation"></param>
        /// <returns></returns>
        private string FormatTransformation(in Matrix4x4 matrix, int interpolation)
        {
            // Python 스타일(행 우선)로 16개 원소를 나열
            // [m00, m01, m02, m03,  m10, m11, m12, m13,  m20, m21, m22, m23,  m30, m31, m32, m33]
            float[] elements =
            {
                matrix.m00, matrix.m01, matrix.m02, matrix.m03,
                matrix.m10, matrix.m11, matrix.m12, matrix.m13,
                matrix.m20, matrix.m21, matrix.m22, matrix.m23,
                matrix.m30, matrix.m31, matrix.m32, matrix.m33
            };

            // 파이썬 코드처럼 round(t, 4) + "f" 형태로 문자열을 만들고,
            // ".0f" → "f" 치환 로직을 수행
            // (Python: transforms_str = transforms_str.replace(".0f", "f"))
            var stringParts = elements.Select(x =>
            {
                // 소수점 4자리 반올림
                double rounded = Math.Round(x, 4);
                // 예: "1.2345f" 형태로 만들기
                string s = $"{rounded}f";
                // "0.0f"나 ".0f" 같은 걸 "f"만 남기도록 치환
                // (좀 더 파이썬스러운 단순 치환)
                return s.Replace(".0f", "f");
            });

            string joined = string.Join(",", stringParts);

            // interpolation 값에 따라 문자열 구성
            if (interpolation > 0)
            {
                if (MinecraftFileManager.MinecraftVersion == "1.21.4")
                {
                    return $"{{start_interpolation:0,interpolation_duration:{interpolation},transformation:[{joined}]}}";
                }
                return $"{{interpolation_duration: {interpolation}, transformation:[{joined}]}}";
            }
            else
            {
                return $"{{transformation:[{joined}]}}";
            }
        }


        // Texture 값 추출 (BdObject 구조에 따라 수정 필요)
        private string ExtractTextureValue(BdObject entity)
        {
            // BdObject에 텍스처 정보를 가져오는 방식이 구현되어 있어야 함
            // 예시: entity.GetNbtValue("tagHead.Value") 또는 entity.CustomTexture 등
            // 아래는 가상의 예시입니다. 실제 BdObject 구조에 맞게 수정하세요.
            /*
            if (entity.NbtData != null && entity.NbtData.ContainsKey("tagHead"))
            {
                // NbtData가 Dictionary<string, object> 형태이고, Value가 문자열이라고 가정
                if (entity.NbtData["tagHead"] is Dictionary<string, object> tagHead && tagHead.ContainsKey("Value"))
                {
                     return tagHead["Value"] as string;
                }
            }
            if (!string.IsNullOrEmpty(entity.CustomTexture)) // CustomTexture 필드가 있다고 가정
            {
                 return entity.CustomTexture;
            }
            */
            // 실제 구현: BdObject의 NBT 파싱 또는 필드 접근 로직 필요
            return null; // 텍스처 없으면 null 반환
        }

        // Item NBT 생성 (텍스처 값 기반)
        private string FormatItemNbt(string textureValue)
        {
            if (string.IsNullOrEmpty(textureValue))
            {
                return null;
            }
            // Minecraft 1.20.5+ components NBT format (profile)
            // 큰따옴표 이스케이프 필요
            return $"item:{{id:\"minecraft:player_head\",components:{{\"minecraft:profile\":{{properties:[{{name:\"textures\",value:\"{textureValue}\"}}]}}}}}}";
            // 이전 버전 NBT: return $"item:{{id:\"minecraft:player_head\",Count:1b,tag:{{SkullOwner:{{Properties:{{textures:[{{Value:\"{textureValue}\"}}]}}}}}}}}";
        }

        // Matrix 비교 함수 (Tolerance 사용)
        private bool MatricesAreEqual(Matrix4x4 m1, Matrix4x4 m2, float tolerance = 0.0001f)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(m1[i] - m2[i]) > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        // 기본 명령어 생성 (Selector 부분 포함)
        private string GenerateCommand(string entityId, BdObject entity, string combinedNbt, bool mode)
        {
            // entityId가 UUID 형식인지, 아니면 태그 기반인지 판별
            string selector;
            string entityType = entity.GetEntityType(); // BdObject에서 엔티티 타입 가져오기 (e.g., "item_display")

            if (string.IsNullOrEmpty(entityType)) // 타입 없으면 명령어 생성 불가
            {
                CustomLog.LogWarning($"엔티티 타입이 없어 명령어 생성 실패: {entityId}");
                return null;
            }

            var uuidmatch = RegexPatterns.UuidExtractedFormatRegex.Match(entityId);
            if (uuidmatch.Success)
            {
                // 각 그룹을 파싱 (Group[0]는 전체 문자열이므로, Group[1]부터 사용)
                if (!long.TryParse(uuidmatch.Groups[1].Value, out long a) ||
                    !int.TryParse(uuidmatch.Groups[2].Value, out int b) ||
                    !int.TryParse(uuidmatch.Groups[3].Value, out int c) ||
                    !int.TryParse(uuidmatch.Groups[4].Value, out int d))
                {
                    throw new FormatException("UUID 부분 중 하나를 숫자로 변환할 수 없습니다.");
                }

                // 각 값을 16진수 문자열로 변환합니다.
                // (비트마스크는 원래 적용되어 있었으나, long과 int는 이미 값이므로 필요에 따라 적용)
                string hexA = ((ulong)a).ToString("x");
                string hexB = (b & 0xFFFFFFFF).ToString("x");
                string hexC = (c & 0xFFFFFFFF).ToString("x");
                string hexD = (d & 0xFFFFFFFF).ToString("x");

                // Python 코드는 "hexA-0-hexB-hexC-hexD" 형식으로 조합합니다.
                string uuidHex = $"{hexA}-0-{hexB}-{hexC}-{hexD}";
                selector = uuidHex; // entityId 대신 이 UUID를 selector로 사용

                // UUID 직접 지정 시 모드 0/1 구분 없이 data merge를 사용
                return $"data merge entity {selector} {combinedNbt}";
            }
            else
            {
                // entityId가 태그 기반이거나 다른 식별자일 경우, BdObject에서 태그 정보 추출
                var tags = entityId.Split(',');
                // for (int i = 0; i < tags.Length; i++)
                // {
                //     Debug.Log($"Tag[{i}]: {tags[i]}");
                // }
                // 끝이 0으로 끝나지 않는 태그 선택
                string tag = null;
                for (int i = 0; i < tags.Length; i++)
                {
                    var rgx = RegexPatterns.TagZeroEndRegex.Match(tags[i]);
                    if (!rgx.Success)
                    {
                        tag = tags[i]; // 태그를 찾으면 저장
                        break; // 첫 번째 태그만 사용
                    }
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    if (!mode) // Execute @s context
                    {
                        selector = $"@s[type={entityType},tag={tag}]";
                        return $"execute if entity {selector} run data merge entity @s {combinedNbt}";
                    }
                    else // Mode 1: Target @e
                    {
                        selector = $"@e[type={entityType},tag={tag},limit=1]"; // 가장 가까운 엔티티 1개 타겟
                        return $"data merge entity {selector} {combinedNbt}";
                    }
                }
                else
                {
                    CustomLog.LogWarning($"UUID 또는 태그를 식별할 수 없어 명령어 생성 방식 불명확: {entityId}");
                    // 기본적으로 Mode 1처럼 처리하거나, 오류 반환
                    selector = $"@e[type={entityType},limit=1,sort=nearest]"; // 태그 없이 타입만으로 지정 (위험할 수 있음)
                    return $"data merge entity {selector} {combinedNbt}";
                    // return null; // 또는 명령어 생성 실패 처리
                }
            }
        }
    }

    #endregion
}