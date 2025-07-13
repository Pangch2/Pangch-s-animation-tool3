using GameSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Animation.UI
{
    public class DragPanel : MonoBehaviour, 
        IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool _isDragging;
        [FormerlySerializedAs("AnimPanel")] public RectTransform animPanel;
        public RectTransform rect;

        public RectTransform canvasRectTransform;

        [Tooltip("패널 상단이 최대로 올라갈 수 있는 위치를 캔버스 높이 대비 비율로 설정합니다. (예: 0.9는 캔버스 높이의 90% 지점까지).")]
        public float maxPanelTopRatio = 0.9f;

        private bool isOnOff;

        private float _lastHeight;
        private float _lastPanelSize;

        private void Start()
        {
            _lastHeight = canvasRectTransform.rect.height;
        }

        public void SetDragPanel(bool isOn)
        {
            isOnOff = isOn;
        }

        private void Update()
        {

            if (!Mathf.Approximately(_lastHeight, canvasRectTransform.rect.height))
            {
                //Debug.Log("Canvas Height Changed");
                SetPanelSize(_lastPanelSize);
                _lastHeight = canvasRectTransform.rect.height;
            }

            if (_isDragging)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    Input.mousePosition,                 
                    null,                
                    out var localPoint       
                );
                
                float targetY = canvasRectTransform.rect.height / 2 + localPoint.y;
                float maxYPosition = canvasRectTransform.rect.height * maxPanelTopRatio;
                targetY = Mathf.Min(targetY, maxYPosition);

                SetPanelSize(targetY);

                if (Input.GetMouseButtonUp(0))
                {
                    _isDragging = false;
                    // UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnDraggingPanel;
                    UIManager.SetUIStatus(UIManager.UIStatus.OnDraggingPanel, false);

                    CursorManager.SetCursor(CursorManager.CursorType.Default);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isOnOff) return;

            _isDragging = true;
            // UIManager.CurrentUIStatus |= UIManager.UIStatus.OnDraggingPanel;
            UIManager.SetUIStatus(UIManager.UIStatus.OnDraggingPanel, true);
        }

        // [추가] 마우스가 패널 영역에 들어왔을 때 호출됩니다.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isOnOff) return;
            
            // 드래그 중이 아닐 때만 Drag 커서로 변경합니다.
            // 드래그 중에는 이미 Drag 상태이므로 불필요한 호출을 막습니다.
            if (!_isDragging)
            {
                CursorManager.SetCursor(CursorManager.CursorType.Drag);
            }
        }

        // [추가] 마우스가 패널 영역에서 나갔을 때 호출됩니다.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isOnOff) return;

            // 드래그 중이 아닐 때만 기본 커서로 되돌립니다.
            // 드래그 중에 마우스가 패널 밖으로 나가도 커서는 Drag 모양을 유지해야 합니다.
            if (!_isDragging)
            {
                CursorManager.SetCursor(CursorManager.CursorType.Default);
            }
        }

        public void SetPanelSize(float y)
        {
            var height = -(canvasRectTransform.rect.height - y);
            animPanel.offsetMax = new Vector2(animPanel.offsetMax.x, height);
            _lastPanelSize = y;

            //rect.position = new Vector3(rect.position.x, y, rect.position.z);
        }

        //public void OnPointerUp(PointerEventData eventData)
        //{
        //    isDragging = false;
        //    BDEngineStyleCameraMovement.CanMoveCamera = true;
        //}
    }
}
