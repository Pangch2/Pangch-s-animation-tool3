using UnityEngine;
using TMPro;

public class DefaultInterpolationDuration : MonoBehaviour
{
    public TMP_InputField inputField;

    private const int MIN_VALUE = 0;
    private const int MAX_VALUE = 99;

    void Start()
    {
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.characterLimit = 3; // �ϴ� 3�ڸ����� ���, �������� ������ �Ŵϱ�

        // �⺻�� ����
        inputField.text = "3";

        inputField.onEndEdit.AddListener(ValidateAndApply);

        // SettingManager���� �ʱⰪ �ݿ�
        SettingManager.instance.setting.defaultInterpolationDuration = 3;
    }

    void ValidateAndApply(string input)
    {
        int value = 0;

        if (!int.TryParse(input, out value))
        {
            value = MIN_VALUE;
        }

        // ���� ����
        if (value < MIN_VALUE)
        {
            value = MIN_VALUE;
        }
        else if (value > MAX_VALUE)
        {
            value = MAX_VALUE;
        }

        // TMP_InputField �ؽ�Ʈ ���� (�� ���� �ݿ�)
        inputField.text = value.ToString();

        // ���� ����
        SettingManager.instance.setting.defaultInterpolationDuration = value;
        Debug.Log("Duration set to: " + value);
    }
}
