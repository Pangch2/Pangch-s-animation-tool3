using System.Collections;
using Animation.AnimFrame;
using BDObjectSystem;
using Cysharp.Threading.Tasks;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Animation.UI
{
    public class AnimPanel : MonoBehaviour
    {
        private AnimManager _manager;

        public DragPanel dragPanel;
        public RectTransform animPanel;
        private Vector2 _initPos;
        private bool _isHiding;
        public bool isMouseEnter;
        public TMP_InputField tickField;
        public TMP_InputField tickSpeedField;

        public Image playPauseButton;
        public Sprite playSprite;
        public Sprite pauseSprite;

        public bool IsMiddleClicked;
        // 누적값 변수 선언 (픽셀 단위)
        private float accumulatedDelta = 0f;

        private void Start()
        {
            _manager = GetComponent<AnimManager>();

            animPanel = dragPanel.animPanel;
            _initPos = new Vector2(0, 225);
            AnimManager.TickChanged += AnimManager_TickChanged;

            _isHiding = false;
            dragPanel.SetDragPanel(!_isHiding);
            dragPanel.SetPanelSize(_initPos.y);
        }

        private void Update()
        {
            isMouseEnter = RectTransformUtility.RectangleContainsScreenPoint(
                    animPanel, Input.mousePosition, null
                );

            // 마우스가 패널 안에 있으면 카메라 이동 불가능

            if (!isMouseEnter && UIManager.CurrentUIStatus.HasFlag(UIManager.UIStatus.OnAnimUIPanel))
            {
                UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnAnimUIPanel;
            }
            else if (isMouseEnter && !UIManager.CurrentUIStatus.HasFlag(UIManager.UIStatus.OnAnimUIPanel))
            {
                UIManager.CurrentUIStatus |= UIManager.UIStatus.OnAnimUIPanel;
            }

            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                // 마우스 버튼이 눌리면 IsMiddleClicked를 true로 설정
                IsMiddleClicked = true;
            }
            // 예: Update() 내에서 처리
            if (isMouseEnter && IsMiddleClicked)
            {
                // 마우스 이동량 (픽셀 단위) 읽기
                var mouseDelta = Mouse.current.delta.ReadValue();
                float move = mouseDelta.x; // 좌우 이동량

                // 누적 값에 현재 프레임의 이동량을 더합니다.
                accumulatedDelta += move;

                // 임계치 (threshold): 누적 이동량이 몇 픽셀 이상이면 한 칸 이동
                float threshold = 10f; // 필요에 따라 조정

                // 누적 이동량이 threshold 이상일 경우 처리 (양방향)
                if (Mathf.Abs(accumulatedDelta) >= threshold)
                {
                    // 넘은 픽셀 수만큼 몇 칸 이동할지 정하기
                    // 예를 들어, 10픽셀당 2 tick 이동
                    int ticksToMove = (int)(accumulatedDelta / threshold);

                    // 현재 시작 tick 가져오기
                    int startTick = _manager.timeline.startTick;

                    // 음수, 양수에 따라 새로운 tick 계산 (예외 처리 필요 시 추가)
                    int newTick = startTick + ticksToMove;
                    // 예: newTick이 0보다 작으면 0으로 제한
                    newTick = Mathf.Max(newTick, 0);

                    // Debug.Log($"Tick: {_manager.Tick} / StartTick: {startTick} / TicksToMove: {ticksToMove} / NewTick: {newTick}");

                    // 그리드 업데이트 (tick 텍스트 수정)
                    _manager.timeline.SetTickTexts(newTick);
                    _manager.Tick = _manager.Tick;

                    // 이미 적용한 픽셀 값만큼 누적 값을 차감
                    accumulatedDelta -= ticksToMove * threshold;

                }

                // 휠 버튼이 해제되었으면 상태와 누적값 초기화
                if (Mouse.current.middleButton.isPressed == false)
                {
                    IsMiddleClicked = false;
                    accumulatedDelta = 0f;
                }
            }

        }

        public void OnTickFieldEndEdit(string value)
        {
            if (int.TryParse(value, out var t))
                _manager.Tick = t;
            else
                tickField.text = _manager.Tick.ToString();
        }

        public void OnTickSpeedFieldEndEdit(string value)
        {
            if (float.TryParse(value, out var t))
                _manager.TickSpeed = t;
            else
                tickSpeedField.text = _manager.TickSpeed.ToString();
        }

        private void AnimManager_TickChanged(float obj)
        {
            tickField.text = obj.ToString("F2");
        }

        public void Stop()
        {
            _manager.IsPlaying = false;
            playPauseButton.sprite = playSprite;
            _manager.Tick = 0;
        }

        public void PlayPause()
        {

            if (_manager.IsPlaying)
            {
                playPauseButton.sprite = playSprite;
            }
            else
            {
                playPauseButton.sprite = pauseSprite;
            }
            _manager.IsPlaying = !_manager.IsPlaying;
        }

        // 패널 토글 버튼 
        public void TogglePanel()
        {
            if (_isHiding)
            {
                // 위로 올리기
                // StopAllCoroutines();
                // StartCoroutine(MovePanelCoroutine(_initPos.y));
                MovePanelAsync(_initPos.y).Forget();
            }
            else
            {
                // 아래로 내리기 
                // StopAllCoroutines();
                // StartCoroutine(MovePanelCoroutine(0));
                MovePanelAsync(0).Forget();
            }
            dragPanel.SetDragPanel(_isHiding);

            _isHiding = !_isHiding;
        }

        private async UniTask MovePanelAsync(float targetY)
        {
            float pos = dragPanel.rect.position.y;
            float time = 0f;

            while (time < 1f)
            {
                // 매 프레임 0.03 비율로 Lerp
                pos = Mathf.Lerp(pos, targetY, 0.03f);
                dragPanel.SetPanelSize(pos);

                time += Time.deltaTime;

                // 다음 프레임까지 대기
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 마지막으로 정확히 targetY 설정
            dragPanel.SetPanelSize(targetY);
        }

        public void OnScrollWheel(InputAction.CallbackContext callback)
        {
            if (!isMouseEnter) return;

            var scroll = callback.ReadValue<Vector2>();

            switch (scroll.y)
            {
                case > 0.1f:
                    _manager.timeline.ChangeGrid(5);
                    break;
                case < -0.1f when _manager.timeline.gridCount > 20:
                    _manager.timeline.ChangeGrid(-5);
                    break;
            }
        }

        public void MoveTickLeft(InputAction.CallbackContext callback)
        {
            if (callback.started)
                _manager.TickAdd(-1);

        }

        public void MoveTickRight(InputAction.CallbackContext callback)
        {
            if (callback.started)
                _manager.TickAdd(1);
        }

        public void OnResetButton()
        {
            // 애니메이션 초기화
            Stop();

            // 모든 모델 초기화
            GameManager.GetManager<BdObjectManager>().ClearAllObject();

            // 모든 트랙 제거
            GameManager.GetManager<AnimObjList>().ResetAnimObject();

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        }

        public void OnPressDeleteKey()
        {
            var contextMenu = GameManager.GetManager<ContextMenuManager>();
            if (contextMenu.currentType == ContextMenuManager.ContextMenuType.Frame && contextMenu.isMenuActive == true)
            {
                contextMenu.OnFrameRemoveButton();
            }
        }

        public void OnPressAddKey()
        {
            var contextMenu = GameManager.GetManager<ContextMenuManager>();
            if (contextMenu.currentType == ContextMenuManager.ContextMenuType.NewFrame && contextMenu.isMenuActive == true)
            {
                contextMenu.OnAddFrameButtonClicked();
            }
        }
    }
}
