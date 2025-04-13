using System.Collections;
using Cysharp.Threading.Tasks;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
                StopAllCoroutines();
                // StartCoroutine(MovePanelCoroutine(_initPos.y));
                MovePanelAsync(_initPos.y).Forget();
            }
            else
            {
                // 아래로 내리기 
                StopAllCoroutines();
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

        //public void OnAnimPanelPointer(bool IsEnter)
        //{
        //    BDEngineStyleCameraMovement.CanMoveCamera = !IsEnter;
        //    IsMouseEnter = IsEnter;
        //}

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
    }
}
