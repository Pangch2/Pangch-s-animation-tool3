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
        inputField.characterLimit = 3; // 일단 3자리까지 허용, 고정으로 제어할 거니까

        // 기본값 설정
        inputField.text = "3";

        inputField.onEndEdit.AddListener(ValidateAndApply);

        // SettingManager에도 초기값 반영
        SettingManager.instance.setting.defaultInterpolationDuration = 3;
    }

    void ValidateAndApply(string input)
    {
        int value = 0;

        if (!int.TryParse(input, out value))
        {
            value = MIN_VALUE;
        }

        // 범위 제한
        if (value < MIN_VALUE)
        {
            value = MIN_VALUE;
        }
        else if (value > MAX_VALUE)
        {
            value = MAX_VALUE;
        }

        // TMP_InputField 텍스트 갱신 (값 고정 반영)
        inputField.text = value.ToString();

        // 실제 적용
        SettingManager.instance.setting.defaultInterpolationDuration = value;
        Debug.Log("Duration set to: " + value);
    }
}
