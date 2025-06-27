using UnityEngine;
using TMPro;
using System.Reflection;

public class IntPickup : MonoBehaviour
{
    public TMP_InputField inputField;
    public string settingFieldName;

    [Tooltip("Inspector���� �����ϴ� �⺻��")]
    public int defaultValue = 1;

    void Start()
    {
        // �⺻���� ��ǲ �ʵ忡 ����
        inputField.text = defaultValue.ToString();

        inputField.onValueChanged.AddListener(FilterInput); // �ǽð� ���͸�
        inputField.onEndEdit.AddListener(ApplyInput);       // ����
    }

    // �Է� �� ���͸� (���ڸ� ���)
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

    // �Է� �Ϸ� �� ó��
    void ApplyInput(string input)
    {
        if (int.TryParse(input, out int value))
        {
            if (value < 0) value = defaultValue; // ������ Inspector���� ������ �⺻������ ����
        }
        else
        {
            value = defaultValue;               // �߸��� �Է��̸� �⺻��
            inputField.text = defaultValue.ToString();   // UI���� �ݿ�
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
