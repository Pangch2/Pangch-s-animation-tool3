using UnityEngine;
using TMPro;

public class GenerationModeDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        // �⺻���� 0�� �ɼ����� ����
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        // ������ ����
        SettingManager.instance.setting.generationMode = 0;

        // ��Ӵٿ� �� ���� �� �̺�Ʈ ����
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void OnDropdownValueChanged(int index)
    {
        // generationMode�� ������ �ɼ� �ε��� ������ ����
        SettingManager.instance.setting.generationMode = index;

        Debug.Log("generationMode ������: " + index);
    }
}
