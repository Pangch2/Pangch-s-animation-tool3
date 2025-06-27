using UnityEngine;
using TMPro;
using System.Reflection;

public class StringPickup : MonoBehaviour
{
    public TMP_InputField inputField;
    public string settingFieldName;

    void Start()
    {
        inputField.onEndEdit.AddListener(ApplyInput);
    }

    void ApplyInput(string input)
    {
        // 빈 문자열이면 아무 작업도 하지 않음
        if (string.IsNullOrWhiteSpace(input)) return;

        var setting = SettingManager.instance.setting;
        var field = setting.GetType().GetField(settingFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(setting, input);
        }
        else
        {
            Debug.LogWarning($"Field '{settingFieldName}' not found on setting object.");
        }
    }
}
