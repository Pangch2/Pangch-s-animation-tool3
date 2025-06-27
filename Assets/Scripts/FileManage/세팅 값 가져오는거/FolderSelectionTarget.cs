using TMPro;
using UnityEngine;

[System.Serializable]
public class FolderSelectionTarget
{
    public TMP_Text textTarget;                 // TMP_Text에 경로를 넣고 싶으면 여기 할당
    public TMP_InputField inputFieldTarget;     // InputField에 넣고 싶으면 여기 할당
    public string settingKey;                   // SettingManager.instance.setting의 필드명 (예: "frameFolderPath")
}
