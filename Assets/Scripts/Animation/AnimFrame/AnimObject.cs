using System;
using System.Collections.Generic;
using BDObjectSystem;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Animation.UI;
using BDObjectSystem.Utility;
using FileSystem;
using Animation;
using FileSystem.Export;

namespace Animation.AnimFrame
{
    public partial class AnimObject : MonoBehaviour
    {
        #region Variables
        public RectTransform rect;
        public TextMeshProUGUI title;
        public Frame firstFrame;

        public SortedList<int, Frame> frames = new SortedList<int, Frame>();
        public string bdFileName;

        private int MaxTick => frames.Count == 0 ? 0 : frames.Values[frames.Count - 1].tick;

        private AnimObjList _manager;

        public BDObjectAnimator animator;
        public Transform framePanel;

        public string tagName = string.Empty;
        public int uuidNumber = -1;

        public GameObject triggerObject;
        public GameObject selectPanel;
        public bool IsSelected => selectPanel.activeSelf;

        public List<string> differentNames = new List<string>();
        public Frame beforeFrame;
        #endregion

        #region Functions
        // Set initial values and initialize first frame
        public void Init(string fileName, AnimObjList list)
        {
            title.text = fileName;
            _manager = list;
            bdFileName = fileName;

            animator = GameManager.GetManager<BdObjectManager>().BDObjectAnim[fileName];

            GetTickAndInterByFileName(bdFileName, out _, out var inter);

            Canvas.ForceUpdateCanvases();

            firstFrame.Init(bdFileName, 0, inter, animator.RootObject.BdObject, this, _manager.timeline);

            frames[0] = firstFrame;

            AnimManager.TickChanged += OnTickChanged;
            selectPanel.SetActive(false);

            beforeFrame = firstFrame;

            SetTagName(animator.RootObject.BdObject);
        }

        void SetTagName(BdObject obj)
        {
            // 디스플레이로 이동
            BdObject p = obj;
            while (p.IsDisplay == false)
            {
                p = p.Children[0];
            }

            var tags = BdObjectHelper.GetTags(p.Nbt);
            string uuid = BdObjectHelper.GetUuid(p.Nbt);

            if (string.IsNullOrEmpty(tags))
            {
                tagName = string.Empty;
                return;
            }

            foreach (var tag in tags.Split(','))
            {
                var rgx = ExportManager.TagZeroEndRegex.Match(tag);
                if (rgx.Success)
                {
                    tagName = tag[..^1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(uuid) == false)
            {
                var numbers = uuid.Split(',');
                uuidNumber = int.Parse(numbers[0]);

            }

        }

        public void SetSelectPanel(bool isOn)
        {
            selectPanel.SetActive(isOn);
        }
        #endregion

        #region Transform

        public void OnTickChanged(float tick)
        {
            // get left frame index
            var left = GetLeftFrame(tick);
            if (left < 0) return;
            var leftFrame = frames.Values[left];


            // 보간 없이 적용해야 하는 경우: interpolation이 0이거나, 보간 종료됐거나, 첫 프레임인 경우
            if (leftFrame.interpolation == 0 || leftFrame.tick + leftFrame.interpolation < tick || left == 0)
            {
                if (leftFrame.IsModelDiffrent)
                    animator.ApplyDiffrentStructureTransform(leftFrame);
                else
                    animator.ApplyTransformation(leftFrame);
            }
            else
            {
                SetObjectTransformationInterpolation(tick, left);
            }

            /*
            모델의 텍스쳐가 바뀌는 경우
            1. NBT - 아이템의 name, 블록의 name이 바뀌는 경우
            2. 머리의 텍스쳐 값이 바뀌는 경우
            */

            // 이전 프레임과 다른 프레임에 도달한 경우
            if (leftFrame != beforeFrame)
            {
                differentNames.Clear();
                Frame.CompareFrameLeafObjects(beforeFrame?.leafObjects, leftFrame.leafObjects, differentNames);

                foreach (var name in differentNames)
                {   
                    ApplyTextureChange(name, leftFrame);
                }
            }

            beforeFrame = leftFrame;

        }

        private void SetObjectTransformationInterpolation(float tick, int indexOf)
        {
            Frame a = frames.Values[indexOf - 1];
            Frame b = frames.Values[indexOf];

            // b 프레임 기준 보간 비율 t 계산 (0~1로 클램프)
            float t = Mathf.Clamp01((tick - b.tick) / b.interpolation);
            animator.ApplyTransformation(a, b, t);
        }

        // 현재 tick에 맞는 왼쪽 프레임의 인덱스를 찾음 (binary search)
        private int GetLeftFrame(float tick)
        {
            tick = (int)tick;
            if (frames.Values[0].tick > tick)
                return -1;

            var left = 0;
            var right = frames.Count - 1;
            var keys = frames.Keys;
            var idx = -1;

            while (left <= right)
            {
                var mid = (left + right) / 2;
                if (keys[mid] <= tick)
                {
                    idx = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return idx >= 0 ? idx : -1;
        }

        public void UpdateAllFrameInterJump()
        {
            foreach (var frame in frames.Values)
            {
                frame.UpdateInterpolationJump();
            }
        }

        void ApplyTextureChange(string tag, Frame currentFrame)
        {
            if (animator.modelDict.TryGetValue(tag, out var container) && currentFrame.leafObjects.TryGetValue(tag, out var bdObject))
            {
                // 이름이 다르면 텍스쳐 변경
                if (container.BdObject.Name != bdObject.Name)
                {
                    container.ChangeBDObject(bdObject);
                }
                else if (bdObject.IsHeadDisplay)
                {
                    // 플레이어 머리 텍스쳐 변경
                    string frameTexture = bdObject.GetHeadTexture();
                    string modelTexture = container.BdObject.GetHeadTexture();

                    if (frameTexture != modelTexture)
                    {
                        container.ChangeBDObject(bdObject);
                    }
                }
            }

        }
        #endregion


        #region EditFrame

        // 이름은 클릭이지만 down 이벤트로 처리
        public void OnEventTriggerClick(BaseEventData eventData)
        {
            // click event
            if (eventData is PointerEventData pointerData)
            {
                if (pointerData.button == PointerEventData.InputButton.Left)
                {
                    if (pointerData.pointerCurrentRaycast.gameObject == triggerObject)
                    {
                        // 선택한 프레임 초기화 
                        _manager.ClearAllSelections();

                    }
                    // 타임 라인에 메세지 전달
                    _manager.timeline.OnPointerDown(pointerData);
                    _manager.SelectAnimObject(this);
                }
                else
                {
                    var line = _manager.timeline.GetTickLine(pointerData.position);
                    GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
                }
                //Debug.Log("Right Click");

            }
        }

        // add frame with tick and inter
        public Frame AddFrame(string fileName, BdObject frameInfo, int tick, int inter, bool useDefaultTickInterval = false)
        {
            //Debug.Log("fileName : " + fileName + ", tick : " + tick + ", inter : " + inter);

            var frame = Instantiate(_manager.framePrefab, framePanel);

            // if already exists, tick increment
            while (frames.ContainsKey(tick))
            {
                tick += useDefaultTickInterval ? GameManager.GetManager<SettingManager>().defaultTickInterval : 1;
            }

            frames.Add(tick, frame);
            frame.Init(fileName, tick, inter, frameInfo, this, _manager.timeline);

            return frame;
        }

        // add frame with fileName
        public Frame AddFrame(BdObject frameInfo, string fileName)
        {
            //CustomLog.Log("AddFrame : " + fileName);    
            GetTickAndInterByFileName(fileName, out var tick, out var inter);
            return AddFrame(fileName, frameInfo, tick, inter);
        }

        // get tick and inter from fileName
        private void GetTickAndInterByFileName(string fileName, out int tick, out int inter)
        {
            var setting = GameManager.GetManager<SettingManager>();

            // default setting
            tick = MaxTick;
            inter = setting.defaultInterpolation;

            var fileManager = GameManager.GetManager<FileLoadManager>();

            // frame.txt 쓴다면
            if (setting.UseFrameTxtFile)
            {
                var frame = BdObjectHelper.ExtractFrame(fileName, "f");
                if (!string.IsNullOrEmpty(frame))
                {
                    if (fileManager.FrameInfo.TryGetValue(frame, out var info))
                    {
                        tick += info.Item1;
                        inter = info.Item2;
                        return;
                    }
                }
            }

            // if using name info extract
            if (setting.UseNameInfoExtract)
            {
                var sValue = BdObjectHelper.ExtractNumber(fileName, "s", setting.defaultTickInterval);
                inter = BdObjectHelper.ExtractNumber(fileName, "i", inter);

                if (sValue > 0)
                    tick += sValue;
            }
            else
            {
                // using default setting
                tick += setting.defaultTickInterval;
            }
        }

        // remove frame
        public void RemoveFrame(Frame frame)
        {
            if (frames == null) return;

            if (frame == beforeFrame)
            {
                beforeFrame = null;
            }

            frames.Remove(frame.tick);
            Destroy(frame.gameObject);

            if (frames.Count == 0)
            {
                RemoveAnimObj();
                return;
            }
            else if (frame.tick == 0)
            {
                frames.Values[0].SetTick(0);
            }
        }

        // remove self
        public void RemoveAnimObj()
        {
            var frame = frames;
            frames = null;
            while (frame.Count > 0)
            {
                frame.Values[0].RemoveFrame();
                Destroy(frame.Values[0].gameObject);
                frame.RemoveAt(0);
            }
            _manager.RemoveAnimObject(this);
        }

        // change frame's tick
        public bool ChangePos(Frame frame, int firstTick, int changedTick)
        {
            //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
            if (firstTick == changedTick) return true;

            // if already exists, return false
            if (frames.ContainsKey(changedTick)) return false;

            frames.Remove(firstTick);
            frames.Add(changedTick, frame);

            OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
            return true;
        }
        #endregion

        void OnDestroy()
        {
            AnimManager.TickChanged -= OnTickChanged;
        }

        public async void OnRemoveButtonClicked()
        {
            var uiMan = GameManager.GetManager<UIManager>();
            bool check = await uiMan.ShowPopupPanelAsync("정말로 이 트랙을 삭제하시겠습니까?", bdFileName);

            if (check)
            {
                RemoveAnimObj();
            }
            else
            {
                CustomLog.Log("Remove AnimObject Cancelled: " + bdFileName);
            }

        }

        #region Change Frame Texture

        /// <summary>
        /// 프레임이 BDObject와 모델의 텍스쳐를 비교해서 다른 부분이 있으면 프레임의 텍스쳐로 수정한다.
        /// </summary>
        /// <param name="back"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public void SetDiffrentTexture(Frame current)
        {
            var model = animator.modelDict;
            var bdobject = current.leafObjects;

            foreach (var leaf in bdobject)
            {
                var leafID = leaf.Key;
                var leafObj = leaf.Value;

                if (model.TryGetValue(leafID, out var modelObj))
                {
                    if (leafObj.Name != modelObj.name)
                    {
                        // 이름 다른 케이스 (블록이 다름)
                        // TODO: 블록의 텍스쳐를 프레임의 텍스쳐로 변경하는 로직 구현
                    }
                    else if (leafObj.IsHeadDisplay)
                    {
                        // 플레이어 머리 텍스쳐 변경 케이스
                        string frameTexture = leafObj.GetHeadTexture();
                        string modelTexture = modelObj.BdObject.GetHeadTexture();

                        if (frameTexture != modelTexture)
                        {
                            // 프레임의 텍스쳐가 모델의 텍스쳐와 다르면 프레임의 텍스쳐로 변경
                            // TODO: 프레임의 텍스쳐를 변경하는 로직 구현

                        }
                    }
                }
                else
                {
                    // 모델에 ID가 없는 경우 소환
                    // ! 미구현
                }
            }



        }

        #endregion

        public int DebugTick;
        [ContextMenu("Debug Find Frame")]
        public void DebugFindFrame()
        {
            var left = GetLeftFrame(DebugTick);
            CustomLog.Log($"DebugFindFrame: {DebugTick} -> {left}, {frames.Values[left].fileName} frames found.");
        }
    }
}
