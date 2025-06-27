using UnityEngine;
using System.IO;

public class FrameRateLimiterAndFPSDisplay : MonoBehaviour
{
    [Tooltip("���⿡ ����� ��Ʈ�� �巡���ϼ���.")]
    public Font customFont;

    float deltaTime = 0.0f;
    GUIStyle style;
    Rect rect;

    void Start()
    {
        // "��ġ�� result" ������ ������ ����
        string folderName = "��ġ�� result";
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"���� ������: {folderPath}");
        }
        else
        {
            Debug.Log($"���� �̹� ������: {folderPath}");
        }

        //ȭ��ũ�� ����
        Screen.SetResolution(900, 550, false);
        // ���� �ػ��� refreshRateRatio���� ���� �ֻ��� ���
        Resolution currentRes = Screen.currentResolution;
        float refreshRate = (float)currentRes.refreshRateRatio.numerator / currentRes.refreshRateRatio.denominator;
        int targetFPS = Mathf.CeilToInt(refreshRate) + 10;

        QualitySettings.vSyncCount = 0;  // VSync ����
        Application.targetFrameRate = targetFPS;

        Debug.Log($"�ֻ���: {refreshRate}Hz, FPS ����: {targetFPS}");

        int h = Screen.height;

        // GUIStyle �ʱ�ȭ �� Ŀ���� ��Ʈ ����
        style = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = h * 2 / 50,
            normal = { textColor = Color.black },
            font = customFont
        };

        rect = new Rect(10, 10, Screen.width, h * 2 / 100);
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.05f;
    }

    void OnGUI()
    {
        float fps = 1.0f / deltaTime;
        string text = $"FPS: {fps:0.}";
        GUI.Label(rect, text, style);
    }
}
