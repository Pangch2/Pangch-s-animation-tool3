using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using GameSystem;

public class PopupPanelManager : MonoBehaviour
{
    [Header("Yes or No Popup Panel")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText1;
    public TextMeshProUGUI popupText2;
    public Button popupApplyButton;
    public Button popupCancelButton;

    private Action<bool> _onApplyOrCancel;
    private Image backgroundImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        popupPanel.SetActive(false);
        popupApplyButton.onClick.AddListener(() => OnPopupButton(true));
        popupCancelButton.onClick.AddListener(() => OnPopupButton(false));

        backgroundImage = GetComponent<Image>();
        backgroundImage.enabled = false;
    }

    /// <summary>
    /// 팝업을 표시하고 콜백을 등록합니다.
    /// </summary>
    public void ShowPopup(string text1, string text2, Action<bool> onApplyOrCancelCallback)
    {
        popupPanel.SetActive(true);
        popupText1.text = text1;
        popupText2.text = text2;
        _onApplyOrCancel = onApplyOrCancelCallback;
        backgroundImage.enabled = true; // 배경 이미지 활성화

        UIManager.SetUIStatus(UIManager.UIStatus.OnPopupPanel, true); // UI 상태 설정
    }

    private void OnPopupButton(bool isApply)
    {
        popupPanel.SetActive(false);
        _onApplyOrCancel?.Invoke(isApply);
        _onApplyOrCancel = null; // 콜백 사용 후 정리
        UIManager.SetUIStatus(UIManager.UIStatus.OnPopupPanel, false); // UI 상태 해제
        backgroundImage.enabled = false; // 배경 이미지 비활성화
    }
}
