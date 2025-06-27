using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using BDObjectSystem.Utility;
using UnityEngine.UI;
using Animation.UI;
using Animation;
using System;
using UnityEngine.InputSystem;

namespace Animation.AnimFrame
{
    public class Frame : MonoBehaviour, IPointerDownHandler
    {
        [Header("Frame Components")]
        public RectTransform rect;
        public Image outlineImage;
        public AnimObject animObject;

        private Color _initColor;
        private readonly Color _selectedColor = Color.yellow;
        // private bool isMouseDown;
        // isSelected는 이제 AnimObjList에 의해 SetSelectedVisual을 통해 관리됩니다.
        // 드래그 로직 등 Frame 내부 로직에 필요할 수 있어 유지합니다.
        private bool isSelected = false;
        public bool IsBeingDragged { get; set; } = false;

        [Header("Frame Info")]
        public int tick;
        public int interpolation;
        public string fileName;
        public TickLine tickLine;
        public RectTransform interpolationRect;

        [Header("BDObject Info")]
        public BdObject Info;
        public Dictionary<string, BdObject> leafObjects;

        public bool IsModelDiffrent;
        public Dictionary<string, Matrix4x4> worldMatrixDict = null;
        public Dictionary<string, Matrix4x4> modelMatrixDict = new Dictionary<string, Matrix4x4>();
        public Dictionary<string, Matrix4x4> interJumpDict = new Dictionary<string, Matrix4x4>();

        public bool IsJump = false;

        private Timeline _timeline;
        private AnimObjList _animObjList; // AnimObjList 참조

        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj, Timeline timeLine)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            if (outlineImage != null) // outlineImage가 할당되었는지 확인
            {
                _initColor = outlineImage.color;
            }
            _timeline = timeLine;
            Info = info;
            tick = initTick;
            leafObjects = BdObjectHelper.SetDisplayDict(info, modelMatrixDict);

            _animObjList = GameManager.GetManager<AnimObjList>(); // AnimObjList 인스턴스 가져오기

            UpdatePos();
            _timeline.OnGridChanged += UpdatePos;

            IsModelDiffrent = animObject.animator.RootObject.BdObjectID != info.ID;

            worldMatrixDict = AffineTransformation.GetAllLeafWorldMatrices(info);
            SetInter(inter);
        }

        public Matrix4x4 GetMatrix(string id)
        {
            if (IsJump && interJumpDict.TryGetValue(id, out var matrix))
            {
                return matrix;
            }
            if (modelMatrixDict.TryGetValue(id, out matrix))
            {
                return matrix;
            }
            CustomLog.UnityLog($"Matrix not found for ID: {id}");
            return Matrix4x4.identity;
        }

        public Matrix4x4 GetWorldMatrix(string id)
        {
            if (IsJump && interJumpDict.TryGetValue(id, out Matrix4x4 matrix))
            {
                return matrix;
            }
            if (worldMatrixDict.TryGetValue(id, out matrix))
            {
                return matrix;
            }
            CustomLog.UnityLog($"World Matrix not found for ID: {id}");
            return Matrix4x4.identity;
        }

        public int SetTick(int newTick)
        {
            // 0�̸� ����
            if (tick == 0)
                return tick;
            if (!animObject.ChangePos(this, tick, newTick)) return tick;

            tick = newTick;
            UpdatePos();
            animObject.UpdateAllFrameInterJump();
            return newTick;

        }

        // 변경된 Grid에 맞추어 위치 변경 
        private void UpdatePos()
        {
            var line = _timeline.GetTickLine(tick, false);
            if (line is null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                //var pos = line.rect.anchoredPosition.x;
                //rect.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, rect.anchoredPosition.y);
                rect.position = new Vector2(line.rect.position.x, rect.position.y);
                tickLine = line;
            }
            UpdateInterpolationBar();
        }

        public bool SetInter(int inter)
        {
            if (interpolation < 0)
                return false;

            interpolation = inter;
            UpdateInterpolationBar();

            animObject.UpdateAllFrameInterJump();

            return true;
        }

        /// <summary>
        /// Interpolation Bar를 업데이트합니다.
        /// Interpolation이 0일 경우에는 Bar를 비활성화합니다.
        /// </summary>
        private void UpdateInterpolationBar()
        {
            if (interpolation == 0)
            {
                interpolationRect.gameObject.SetActive(false);
            }
            else
            {
                interpolationRect.gameObject.SetActive(true);

                var line = _timeline.GetTickLine(tick + interpolation, false);
                line ??= _timeline.grid[_timeline.gridCount - 1];

                // 1. line의 World Position 구함
                Vector3 lineWorldPos = line.rect.position;

                // 2. interpolationRect의 부모 기준 Local Position으로 변환
                RectTransform parentRect = interpolationRect.parent as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                    RectTransformUtility.WorldToScreenPoint(null, lineWorldPos),
                    null,
                    out Vector2 localPos);

                // 3. interpolationRect 위치 조정 (왼쪽 고정이니까 anchoredPosition은 그대로)
                float width = localPos.x - interpolationRect.anchoredPosition.x;
                width += line.rect.rect.width / 2; // line의 rect width 반영

                interpolationRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }

        /// <summary>
        /// Interpolation Jump를 업데이트합니다.
        /// </summary>
        public void UpdateInterpolationJump()
        {
            int idx = animObject.frames.IndexOfKey(tick);
            if (idx <= 0 || idx >= animObject.frames.Count - 1)
            {
                IsJump = false;
                interJumpDict.Clear();
                return; // 범위 밖이면 Jump를 하지 않음
            }

            // 다음 프레임
            Frame nextFrame = animObject.frames.Values[idx + 1];

            // (1) 점프 발생 여부 체크
            // "tick + interpolation > nextFrame.tick" → 보간점프 발생

            bool isJump = (tick + interpolation) > nextFrame.tick;

            interJumpDict.Clear();
            if (isJump)
            {

                float ratio = (float)(nextFrame.tick - tick) / interpolation;
                Frame beforeFrame = animObject.frames.Values[idx - 1];

                bool isStructureDifferent = IsModelDiffrent || nextFrame.IsModelDiffrent || beforeFrame.IsModelDiffrent;

                foreach (var obj in leafObjects)
                {
                    var id = obj.Value.ID;

                    if (interJumpDict.ContainsKey(id))
                        continue;

                    if (isStructureDifferent)
                    {
                        // 월드 행렬 기반 보간
                        //worldMatrixDict.TryGetValue(id, out var currentMatrix);
                        Matrix4x4 currentMatrix = GetWorldMatrix(id); // 현재 프레임의 ID별 월드행렬
                        Matrix4x4 beforeMatrix = beforeFrame.GetWorldMatrix(id); // 이전 프레임의 ID별 월드행렬

                        // 최종 보간 적용
                        Matrix4x4 interpolated = BDObjectAnimator.InterpolateMatrixTRS(beforeMatrix, currentMatrix, ratio);
                        interJumpDict.Add(id, interpolated);
                    }
                    else
                    {
                        // 트리 기반 보간
                        var current = obj.Value;

                        while (current != null)
                        {
                            if (interJumpDict.ContainsKey(current.ID))
                                break;

                            Matrix4x4 aMatrix = beforeFrame.GetMatrix(current.ID);
                            Matrix4x4 bMatrix = current.transforms.GetMatrix();

                            Matrix4x4 lerpedMatrix = BDObjectAnimator.InterpolateMatrixTRS(aMatrix, bMatrix, ratio);
                            interJumpDict.Add(current.ID, lerpedMatrix);


                            current = current.Parent;
                        }

                        continue; // 트리 기반 처리했으면 아래로 내려가지 않게
                    }
                }
                IsJump = true;
            }
            else
            {
                IsJump = false;
            }

        }

        /// <summary>
        /// Frame을 클릭했을 때 호출됩니다.
        /// 좌클릭일 경우 선택 상태로 변경하고, 우클릭일 경우 ContextMenu를 띄웁니다.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this);
                return; // 오른쪽 클릭 시 선택 로직 중단
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                bool isCtrlPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool isShiftPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

                // AnimObjList에 클릭 이벤트 처리를 위임합니다.
                // AnimObjList는 이 정보를 바탕으로 선택 상태를 결정하고,
                // 필요한 프레임들의 SetSelectedVisual을 호출합니다.
                _animObjList.HandleFramePointerDown(this, isCtrlPressed, isShiftPressed);

                _animObjList.SelectAnimObject(animObject);

                // 왼쪽 클릭 시 드래그 가능 상태로 만듭니다.
                // 실제 드래그 동작은 Update 메서드에서 isSelected 상태를 확인 후 처리됩니다.
                // isMouseDown = true;
            }
        }

        /// <summary>
        /// 프레임의 선택된 시각적 상태를 설정합니다. AnimObjList에 의해 호출됩니다.
        /// </summary>
        /// <param name="select">선택 여부</param>
        public void SetSelectedVisual(bool select)
        {
            this.isSelected = select;
            if (outlineImage != null)
            {
                outlineImage.color = select ? _selectedColor : _initColor;
            }
        }

        public void RemoveFrame()
        {
            if (animObject != null)
            {
                animObject.RemoveFrame(this); // AnimObject에서 프레임 제거
            }

            // AnimObjList에 프레임 제거를 알려 선택 목록 등에서 처리하도록 합니다.
            _animObjList.HandleFrameRemoval(this);
            // 로컬 isSelected 상태는 SetSelectedVisual을 통해 AnimObjList가 관리하므로 직접 false로 설정할 필요는 없습니다.
            // 만약 HandleFrameRemoval이 SetSelectedVisual(false)를 호출하지 않는다면 여기서 호출해야 할 수 있습니다.
        }

        void OnDestroy()
        {
            if (_timeline != null)
            {
                _timeline.OnGridChanged -= UpdatePos;
            }
        }

        public Frame Duplicate()
        {
            if (this.Info == null)
            {
                CustomLog.UnityLog("Cannot duplicate Frame: Original Info is null.");
                return null;
            }
            if (animObject == null)
            {
                CustomLog.UnityLog("Cannot duplicate Frame: animObject is null.");
                return null;
            }

            BdObject clonedInfo = this.Info.Clone();
            // AddFrame 내부에서 tick 충돌을 처리하므로, 여기서는 원하는 tick (예: 현재 tick + 1)을 전달합니다.
            return animObject.AddFrame(this.fileName + "_copy", clonedInfo, this.tick + 1, this.interpolation);
        }

        /// <summary>
        /// 두 Frame의 leafObjects를 비교하여 이름이 다른 객체들의 리스트를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static List<string> CompareFrameLeafObjects(Dictionary<string, BdObject> frame1Objects, Dictionary<string, BdObject> frame2Objects)
        {
            List<string> differences = new List<string>();

            if (frame1Objects == null || frame2Objects == null)
            {
                CustomLog.UnityLog("One or both frame objects are null.");
                return differences;
            }

            foreach (var obj1 in frame1Objects)
            {
                if (frame2Objects.TryGetValue(obj1.Key, out var obj2))
                {
                    // 이름이 다르면 differences에 추가
                    if (obj1.Value != obj2)
                    {
                        differences.Add(obj1.Key);
                    }
                }
            }
            return differences;
        }
    }
}
