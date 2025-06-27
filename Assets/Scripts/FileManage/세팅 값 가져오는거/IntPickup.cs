using UnityEngine;
using TMPro;
using System.Reflection;

public class IntPickup : MonoBehaviour
{
    public TMP_InputField inputField;
    public string settingFieldName;

    [Tooltip("Inspector에서 지정하는 기본값")]
    public int defaultValue = 1;

    void Start()
    {
        // 기본값을 인풋 필드에 세팅
        inputField.text = defaultValue.ToString();

        inputField.onValueChanged.AddListener(FilterInput); // 실시간 필터링
        inputField.onEndEdit.AddListener(ApplyInput);       // 적용
    }

    // 입력 중 필터링 (숫자만 허용)
    void FilterInput(string input)
    {
        string filtered = "";

        foreach (char c in input)
        {
            if (char.IsDigit(c)) filtered += c;
        }

        if (filtered != input)
        {
            inputField.text = filtered;
        }
    }

    // 입력 완료 시 처리
    void ApplyInput(string input)
    {
        if (int.TryParse(input, out int value))
        {
            if (value < 0) value = defaultValue; // 음수면 Inspector에서 지정한 기본값으로 변경
        }
        else
        {
            value = defaultValue;               // 잘못된 입력이면 기본값
            inputField.text = defaultValue.ToString();   // UI에도 반영
        }

        var setting = SettingManager.instance.setting;
        var field = setting.GetType().GetField(settingFieldName, BindingFlags.Public | BindingFlags.Instance);

        if (field != null && field.FieldType == typeof(int))
        {
            field.SetValue(setting, value);
        }
        else
        {
            Debug.LogWarning($"Field '{settingFieldName}' is not of type int or does not exist.");
        }
    }
}
