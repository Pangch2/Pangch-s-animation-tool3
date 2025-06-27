using UnityEngine;
using System.IO;

public class FrameRateLimiterAndFPSDisplay : MonoBehaviour
{
    [Tooltip("여기에 사용할 폰트를 드래그하세요.")]
    public Font customFont;

    float deltaTime = 0.0f;
    GUIStyle style;
    Rect rect;

    void Start()
    {
        // "팽치류 result" 폴더가 없으면 생성
        string folderName = "팽치류 result";
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"폴더 생성됨: {folderPath}");
        }
        else
        {
            Debug.Log($"폴더 이미 존재함: {folderPath}");
        }

        //화면크기 설정
        Screen.SetResolution(900, 550, false);
        // 현재 해상도의 refreshRateRatio에서 실제 주사율 계산
        Resolution currentRes = Screen.currentResolution;
        float refreshRate = (float)currentRes.refreshRateRatio.numerator / currentRes.refreshRateRatio.denominator;
        int targetFPS = Mathf.CeilToInt(refreshRate) + 10;

        QualitySettings.vSyncCount = 0;  // VSync 끄기
        Application.targetFrameRate = targetFPS;

        Debug.Log($"주사율: {refreshRate}Hz, FPS 제한: {targetFPS}");

        int h = Screen.height;

        // GUIStyle 초기화 및 커스텀 폰트 지정
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
