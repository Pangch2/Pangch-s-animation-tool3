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
            Vector2 mousePos = Input.mousePosition;
            contextMenu.SetActive(true);

            // if (mousePos.x + contextMenuContent.sizeDelta.x > Screen.width)
            // {
            //     mousePos.x -= contextMenuContent.sizeDelta.x;
            // }
            contextMenuContent.anchoredPosition = mousePos;
            contextMenuBtns[(int)currentType].SetActive(true);

            isMenuActive = true;
        }

        // �޴� �ݱ�
        public void CloseContectMenu()
        {
            contextMenuBtns[(int)currentType].SetActive(false);
            contextMenu.SetActive(false);

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
            currentFrame.RemoveFrame();
            CloseContectMenu();
        }
    }
}
