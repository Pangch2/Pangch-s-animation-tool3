using TMPro;
using UnityEngine;

[System.Serializable]
public class FolderSelectionTarget
{
    public TMP_Text textTarget;                 // TMP_Text�� ��θ� �ְ� ������ ���� �Ҵ�
    public TMP_InputField inputFieldTarget;     // InputField�� �ְ� ������ ���� �Ҵ�
    public string settingKey;                   // SettingManager.instance.setting�� �ʵ�� (��: "frameFolderPath")
}
