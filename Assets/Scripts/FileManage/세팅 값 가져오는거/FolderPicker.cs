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
            Debug.LogError("�߸��� �ε����Դϴ�.");
            return;
        }

        var target = folderTargets[index];

        var paths = StandaloneFileBrowser.OpenFolderPanel("���� ����", "", false);

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

            Debug.Log($"[{target.settingKey}]�� ��� ������: {selectedPath}");
        }
        else
        {
            Debug.Log("����ڰ� ���� ������ ����߽��ϴ�.");
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
            Debug.LogError($"SettingManager.setting�� '{propertyName}'��� �Ӽ��̳� �ʵ尡 �����ϴ�.");
        }
    }
}
