using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using BDObjectSystem.Utility;
using UnityEngine.UI;
using Animation.UI;
using Animation;
using System;

namespace Animation.AnimFrame
{
    public class Frame : MonoBehaviour, IPointerDownHandler//, IPointerUpHandler
    {
        [Header("Frame Components")]
        public RectTransform rect;
        public Image outlineImage;
        public AnimObject animObject;

        private Color _initColor;
        private readonly Color _selectedColor = Color.yellow;

        [Header("Frame Info")]
        public bool isSelected;
        public int tick;
        public int interpolation;
        public string fileName;
        public TickLine tickLine;
        public RectTransform interpolationRect;

        [Header("BDObject Info")]
        public BdObject Info;
        public Dictionary<string, BdObject> leafObjects;

        // 모델의 부모 자식 구조가 다를 경우
        public bool IsModelDiffrent;
        public Dictionary<string, Matrix4x4> worldMatrixDict = null;
        // 모델의 모든 노드 ID:Matrix4x4을 저장하는 딕셔너리
        public Dictionary<string, Matrix4x4> modelMatrixDict = new Dictionary<string, Matrix4x4>();
        public Dictionary<string, Matrix4x4> interJumpDict = new Dictionary<string, Matrix4x4>();
        public bool IsJump = false;

        private Timeline _timeline;

        public string specialTag = "psolidsteve0,psolidsteve67";


        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj, Timeline timeLine)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            _initColor = outlineImage.color;
            _timeline = timeLine;
            Info = info;
            tick = initTick;
            leafObjects = BdObjectHelper.SetDisplayDict(info, modelMatrixDict);

            UpdatePos();
            _timeline.OnGridChanged += UpdatePos;


            IsModelDiffrent = animObject.animator.RootObject.bdObjectID != info.ID;
            worldMatrixDict = AffineTransformation.GetAllLeafWorldMatrices(info);
            SetInter(inter);
            //Debug.Log(animObject.animator);
            // if (IsModelDiffrent)
            // {
            //     Debug.Log($"Model is different, name : {fileName}\nModel : {animObject.animator.RootObject.bdObjectID}\nInfo : {info.ID}");

            // }


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
            CustomLog.UnityLogErr($"Matrix not found for ID: {id}");
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
            CustomLog.UnityLogErr($"World Matrix not found for ID: {id}");
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
            if (idx <= 0 || idx >= animObject.frames.Count - 1) return;
            // 범위 밖이면 업데이트 불가

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
                            Matrix4x4 bMatrix = current.Transforms.GetMatrix();

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
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isSelected = true;
                outlineImage.color = _selectedColor;
            }
            else
            {
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this);
            }
        }

        private void Update()
        {
            if (!isSelected) return;

            Vector2 mouse = Input.mousePosition;
            var line = _timeline.GetTickLine(mouse);

            SetTick(line.Tick);

            if (Input.GetMouseButtonUp(0))
            {
                isSelected = false;
                outlineImage.color = _initColor;
            }
        }


        public void RemoveFrame()
        {
            _timeline.OnGridChanged -= UpdatePos;
            animObject.RemoveFrame(this);
        }

    }
}
