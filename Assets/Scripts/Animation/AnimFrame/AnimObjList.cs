using System;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;
using Animation.UI;
using BDObjectSystem;
using FileSystem;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

namespace Animation.AnimFrame
{
    public class AnimObjList : BaseManager
    {
        public RectTransform importButton;
        public float jump;

        public AnimObject animObjectPrefab;
        public Frame framePrefab;
        public List<AnimObject> animObjects = new();

        public Timeline timeline;
        public Transform frameParent;

        public HashSet<Frame> selectedFrames = new();
        private Frame _lastAnchorFrame = null; // Shift 선택의 기준점

        public GameObject cancelPanel;


        private void Start()
        {
            GameManager.GetManager<FileLoadManager>().animObjList = this;
            timeline = GameManager.GetManager<AnimManager>().timeline;
            jump = importButton.sizeDelta.y * 1.5f;
        }

        public AnimObject AddAnimObject(string fileName)
        {
            //Debug.Log("EndAddObject: " + obj.name);

            var animObject = Instantiate(animObjectPrefab, frameParent);
            animObject.Init(fileName, this);
            animObject.rect.anchoredPosition = new Vector2(animObject.rect.anchoredPosition.x, importButton.anchoredPosition.y - 60f);

            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);

            var animMan = GameManager.GetManager<AnimManager>();
            animMan.Tick = 0;
            animMan.timeline.SetTickTexts(0);

            var SaveMan = GameManager.GetManager<SaveManager>();

            // 최초 삽입 시 세이브 파일 생성
            if (SaveMan.IsNoneSaved)
            {
                SaveMan.MakeNewMDEFile(fileName);
            }
            animObjects.Add(animObject);

            return animObject;
        }

        public void ResetAnimObject()
        {
            var objs = animObjects.ToArray();
            foreach (var obj in objs)
            {
                RemoveAnimObject(obj);
            }

        }

        public void RemoveAnimObject(AnimObject obj)
        {
            var idx = animObjects.IndexOf(obj);
            animObjects.RemoveAt(idx);

            // AnimObject가 제거될 때, 해당 Object에 속한 모든 프레임을 선택 해제합니다.
            List<Frame> framesToRemoveFromSelection = new List<Frame>();
            foreach (var selectedFrame in selectedFrames)
            {
                if (selectedFrame.animObject == obj)
                {
                    framesToRemoveFromSelection.Add(selectedFrame);
                }
            }
            foreach (var frameToRemove in framesToRemoveFromSelection)
            {
                selectedFrames.Remove(frameToRemove);
                // SetSelectedVisual(false)는 Frame의 GameObject가 Destroy될 것이므로 호출하지 않아도 됩니다.
            }
            if (_lastAnchorFrame != null && _lastAnchorFrame.animObject == obj)
            {
                _lastAnchorFrame = null;
            }

            GameManager.GetManager<BdObjectManager>().RemoveBdObject(obj.bdFileName);

            Destroy(obj.gameObject);

            for (var i = idx; i < animObjects.Count; i++)
            {
                animObjects[i].rect.anchoredPosition = new Vector2(animObjects[i].rect.anchoredPosition.x, animObjects[i].rect.anchoredPosition.y + jump);
            }
            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y + jump);

            CustomLog.Log("Line Removed: " + obj.bdFileName);
        }

        public List<SortedList<int, ExportFrame>> GetAllFrames()
        {
            var frames = new List<SortedList<int, ExportFrame>>();

            foreach (var animObject in animObjects)
            {
                var animFrames = new SortedList<int, ExportFrame>();
                foreach (var frame in animObject.frames.Values)
                {
                    animFrames.Add(frame.tick, new ExportFrame(frame));
                }
                frames.Add(animFrames);
            }

            return frames;
        }

        public void SelectFrame(Frame frame)
        {
            selectedFrames.Add(frame);
        }

        public void DeSelectFrame(Frame frame)
        {
            selectedFrames.Remove(frame);
        }

        public void HandleFrameClick(Frame clickedFrame, bool isCtrlPressed, bool isShiftPressed)
        {
            if (clickedFrame == null || clickedFrame.animObject == null) return;

            if (!isShiftPressed) // Shift가 눌리지 않은 경우
            {
                if (!isCtrlPressed) // Ctrl도 눌리지 않은 경우 (단일 선택)
                {
                    ClearAllSelections();
                    AddToSelection(clickedFrame);
                    _lastAnchorFrame = clickedFrame;
                }
                else // Ctrl만 눌린 경우 (토글 선택)
                {
                    if (selectedFrames.Contains(clickedFrame))
                    {
                        RemoveFromSelection(clickedFrame);
                        // 마지막으로 상호작용한 프레임을 anchor로 설정 (선택 해제된 경우에도)
                        _lastAnchorFrame = clickedFrame;
                    }
                    else
                    {
                        AddToSelection(clickedFrame);
                        _lastAnchorFrame = clickedFrame;
                    }
                }
            }
            else // Shift가 눌린 경우 (범위 선택)
            {
                if (_lastAnchorFrame == null || _lastAnchorFrame.animObject != clickedFrame.animObject)
                {
                    // 기준점이 없거나 다른 AnimObject의 프레임이면 단일 선택처럼 동작
                    ClearAllSelections();
                    AddToSelection(clickedFrame);
                    _lastAnchorFrame = clickedFrame;
                }
                else
                {
                    // 같은 AnimObject 내에서 범위 선택
                    if (!isCtrlPressed) // Ctrl이 안눌렸으면 기존 선택 모두 해제
                    {
                        ClearAllSelections();
                    }

                    var framesInObject = clickedFrame.animObject.frames.Values.ToList();
                    int anchorIndex = framesInObject.IndexOf(_lastAnchorFrame);
                    int clickedIndex = framesInObject.IndexOf(clickedFrame);

                    if (anchorIndex == -1 || clickedIndex == -1) return; // 프레임 못찾음

                    int startIndex = Mathf.Min(anchorIndex, clickedIndex);
                    int endIndex = Mathf.Max(anchorIndex, clickedIndex);

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        AddToSelection(framesInObject[i]);
                    }
                    // Shift 선택 후에는 clickedFrame이 새로운 anchor가 될 수 있지만,
                    // 일반적인 동작은 마지막 단일/Ctrl 클릭 프레임을 anchor로 유지하는 것입니다.
                    // 여기서는 _lastAnchorFrame을 변경하지 않습니다.
                }
            }
        }

        public void HandleFrameRemoval(Frame removedFrame)
        {
            if (selectedFrames.Contains(removedFrame))
            {
                RemoveFromSelection(removedFrame); // SetSelectedVisual(false)는 여기서 호출됨
            }
            if (_lastAnchorFrame == removedFrame)
            {
                _lastAnchorFrame = null;
            }
        }

        private void AddToSelection(Frame frame)
        {
            if (selectedFrames.Add(frame))
            {
                frame.SetSelectedVisual(true);
            }
        }

        private void RemoveFromSelection(Frame frame)
        {
            if (selectedFrames.Remove(frame))
            {
                frame.SetSelectedVisual(false);
            }
        }

        public void ClearAllSelections()
        {
            foreach (var frame in selectedFrames)
            {
                frame.SetSelectedVisual(false);
            }
            selectedFrames.Clear();
            _lastAnchorFrame = null;
        }


        public void DuplicateSelectedFrames()
        {
            if (selectedFrames.Count == 0) return;

            List<Frame> newlySelectedFrames = new List<Frame>();
            List<Frame> originalSelectedFrames = selectedFrames.ToList(); // 반복 중 컬렉션 수정을 피하기 위해 복사

            foreach (var frame in originalSelectedFrames)
            {
                frame.SetSelectedVisual(false); // 원본 프레임 선택 해제 (시각적으로)
            }
            selectedFrames.Clear(); // 선택 목록에서 원본 모두 제거

            foreach (var frame in originalSelectedFrames)
            {
                var newFrame = frame.Duplicate(); // Duplicate는 새 tick으로 프레임을 생성하고 AnimObject에 추가
                if (newFrame != null)
                {
                    newlySelectedFrames.Add(newFrame);
                }
            }

            foreach (var newFrame in newlySelectedFrames)
            {
                AddToSelection(newFrame); // 복제된 새 프레임들을 선택 상태로 만듦
            }

            if (newlySelectedFrames.Count > 0)
            {
                _lastAnchorFrame = newlySelectedFrames.LastOrDefault(); // 마지막으로 복제된 프레임을 새 anchor로 설정
            }
        }

        public void CancelPanelClicked(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerData && pointerData.button == PointerEventData.InputButton.Left
            && (pointerData.pointerCurrentRaycast.gameObject == cancelPanel || pointerData.pointerCurrentRaycast.gameObject == null))
            {
                ClearAllSelections();
            }
        }
    }
}
