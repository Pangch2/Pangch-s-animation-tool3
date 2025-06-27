using UnityEngine;
using TMPro;
using SFB;
using System;
using System.Reflection;

public class FolderPicker : MonoBehaviour
{
    public FolderSelectionTarget[] folderTargets;

    public void PickFolder(int index)
    {
        if (index < 0 || index >= folderTargets.Length)
        {
            Debug.LogError("잘못된 인덱스입니다.");
            return;
        }

        var target = folderTargets[index];

        var paths = StandaloneFileBrowser.OpenFolderPanel("폴더 선택", "", false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedPath = paths[0];

            if (target.inputFieldTarget != null)
            {
                target.inputFieldTarget.text = selectedPath;
                target.inputFieldTarget.onEndEdit?.Invoke(selectedPath);
            }
            else if (target.textTarget != null)
            {
                target.textTarget.text = selectedPath;
            }

            SetSettingValue(target.settingKey, selectedPath);

            Debug.Log($"[{target.settingKey}]에 경로 설정됨: {selectedPath}");
        }
        else
        {
            Debug.Log("사용자가 폴더 선택을 취소했습니다.");
        }
    }

    private void SetSettingValue(string propertyName, string value)
    {
        var setting = SettingManager.instance.setting;
        Type type = setting.GetType();
        PropertyInfo prop = type.GetProperty(propertyName);
        FieldInfo field = type.GetField(propertyName);

        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(setting, value);
        }
        else if (field != null)
        {
            field.SetValue(setting, value);
        }
        else
        {
            Debug.LogError($"SettingManager.setting에 '{propertyName}'라는 속성이나 필드가 없습니다.");
        }
    }
}
