using System.Collections.Generic;
using GameSystem;
using TMPro;
using UnityEngine;
using Animation.AnimFrame;
using FileSystem;
using UnityEngine.InputSystem;

namespace Animation.UI
{
    public class ContextMenuManager : BaseManager
    {
        public enum ContextMenuType
        {
            NewFrame = 0,
            Frame
        }

        [Header("Context Menu")]
        public ContextMenuType currentType;
        public GameObject contextMenu;
        public RectTransform contextMenuContent;

        [Header("Context Menu Buttons")]
        public List<GameObject> contextMenuBtns;

        public TMP_InputField[] frameInfo;
        // public TextMeshProUGUI frameName;

        [Header("Current Context")]
        public Frame currentFrame;
        public AnimObject currentObj;
        public int animObjectsTick;

        public bool isMenuActive;

        private void Start()
        {
            for (var i = 0; i < contextMenuBtns.Count; i++)
            {
                contextMenuBtns[i].SetActive(false);
            }
            contextMenu.SetActive(false);

            frameInfo[2].onEndEdit.AddListener((value) =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    frameInfo[2].text = currentFrame.fileName.ToString();
                }
                else
                {
                    currentFrame.fileName = value;
                }
            });

        }

        // Set Context Menu
        public void ShowContextMenu(Frame thisFrame)
        {
            currentType = ContextMenuType.Frame;
            currentFrame = thisFrame;

            frameInfo[0].text = currentFrame.tick.ToString();
            frameInfo[1].text = currentFrame.interpolation.ToString();
            frameInfo[2].text = currentFrame.fileName.ToString();
            // frameName.text = currentFrame.fileName;

            SetContextMenu();
        }

        // ���ο� ������ �߰� ������ �޴�
        public void ShowContextMenu(AnimObject obj, int tick)
        {
            currentType = ContextMenuType.NewFrame;
            currentObj = obj;
            animObjectsTick = tick;

            SetContextMenu();
        }

        // �޴� ����
        private void SetContextMenu()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            contextMenu.SetActive(true);

            RectTransform parentRect = contextMenuContent.parent as RectTransform;
            if (parentRect == null)
            {
                Debug.LogError("ContextMenuContent's parent must be a RectTransform for correct positioning.");
                // Fallback: 화면 좌표를 직접 사용하지만, 정확하지 않을 수 있습니다.
                contextMenuContent.position = mousePos; 
                contextMenuBtns[(int)currentType].SetActive(true);
                isMenuActive = true;
                return;
            }

            // 마우스 위치(화면 좌표)를 부모 RectTransform의 로컬 좌표로 변환합니다.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mousePos, null, out var localPoint))
            {
                contextMenuContent.anchoredPosition = localPoint;
            }
            else
            {
                // 변환 실패 시 대체 처리
                Debug.LogWarning("ScreenPointToLocalPointInRectangle failed. Falling back on setting position directly, which might be inaccurate.");
                contextMenuContent.position = mousePos;
            }

            // UI 레이아웃을 강제로 업데이트하여 정확한 크기와 위치를 가져옵니다.
            Canvas.ForceUpdateCanvases();

            // 메뉴가 화면 경계를 벗어나지 않도록 위치를 조정합니다.
            Vector3[] corners = new Vector3[4];
            contextMenuContent.GetWorldCorners(corners); // 월드 좌표계에서의 코너 위치

            // 월드 좌표를 화면 좌표로 변환합니다.
            // corners[0]=bottom-left, corners[1]=top-left, corners[2]=top-right, corners[3]=bottom-right
            Vector2 minScreenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 maxScreenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            Vector2 screenOffset = Vector2.zero;

            // 오른쪽 경계 확인
            if (maxScreenPoint.x > Screen.width)
                screenOffset.x = Screen.width - maxScreenPoint.x; // 왼쪽으로 이동할 음수 오프셋
            // 왼쪽 경계 확인 (오른쪽 경계 조정 후에도 벗어나는 경우)
            else if (minScreenPoint.x < 0)
                screenOffset.x = -minScreenPoint.x; // 오른쪽으로 이동할 양수 오프셋

            // 아래쪽 경계 확인 (Unity 화면 Y 좌표는 아래에서 위로 증가)
            if (minScreenPoint.y < 0)
                screenOffset.y = -minScreenPoint.y; // 위로 이동할 양수 오프셋
            // 위쪽 경계 확인 (아래쪽 경계 조정 후에도 벗어나는 경우)
            else if (maxScreenPoint.y > Screen.height)
                screenOffset.y = Screen.height - maxScreenPoint.y; // 아래로 이동할 음수 오프셋
            
            if (screenOffset != Vector2.zero)
            {
                // 계산된 화면 오프셋을 적용하여 메뉴 위치를 조정합니다.
                // 현재 피벗의 화면 위치에 오프셋을 더한 후, 다시 로컬 좌표로 변환하여 적용합니다.
                Vector2 currentPivotScreenPos = RectTransformUtility.WorldToScreenPoint(null, contextMenuContent.position);
                Vector2 newPivotScreenPos = currentPivotScreenPos + screenOffset;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, newPivotScreenPos, null, out var newLocalPoint))
                {
                    contextMenuContent.anchoredPosition = newLocalPoint;
                 }
            }
            
            // 참고: contextMenuContent의 Pivot 설정에 따라 메뉴가 마우스 커서에 정렬되는 방식이 결정됩니다.
            // 예를 들어 Pivot (0,1)은 메뉴의 왼쪽 상단이 커서 위치에 오도록 합니다.

            contextMenuBtns[(int)currentType].SetActive(true);
            isMenuActive = true;
        }

        // �޴� �ݱ�
        public void CloseContectMenu()
        {
            contextMenuBtns[(int)currentType].SetActive(false);
            contextMenu.SetActive(false);

            if (currentType == ContextMenuType.Frame)
            {
                GameManager.GetManager<AnimObjList>().DeSelectFrame(currentFrame);
            }

            isMenuActive = false;
        }

        public void OnAddFrameButtonClicked()
        {
            GameManager.GetManager<FileLoadManager>().ImportFrame(currentObj, animObjectsTick);
            CloseContectMenu();
        }

        public void OnFrameTickChanged(string value)
        {
            if (int.TryParse(value, out var tick))
            {
                frameInfo[0].text = currentFrame.SetTick(tick).ToString();

            }
            else
            {
                frameInfo[0].text = currentFrame.tick.ToString();
            }
        }

        public void OnFrameInterChanged(string value)
        {
            if (int.TryParse(value, out var inter))
            {
                if (!currentFrame.SetInter(inter))
                    frameInfo[1].text = currentFrame.interpolation.ToString();
            }
            else
            {
                frameInfo[1].text = currentFrame.interpolation.ToString();
            }
        }

        public void OnFrameRemoveButton()
        {
            CloseContectMenu();
            currentFrame.RemoveFrame();
        }
    }
}
