using UnityEngine;
using TMPro;

public class GenerationModeDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        // 기본값을 0번 옵션으로 설정
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        // 설정에 적용
        SettingManager.instance.setting.generationMode = 0;

        // 드롭다운 값 변경 시 이벤트 연결
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void OnDropdownValueChanged(int index)
    {
        // generationMode를 선택한 옵션 인덱스 값으로 설정
        SettingManager.instance.setting.generationMode = index;

        Debug.Log("generationMode 설정됨: " + index);
    }
}
