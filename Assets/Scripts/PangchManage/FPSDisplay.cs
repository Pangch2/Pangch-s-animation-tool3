using UnityEngine;

public class FrameRateLimiterAndFullscreenToggle : MonoBehaviour
{
    [Tooltip("여기에 사용할 폰트를 드래그하세요.")]
    public Font customFont;

    float deltaTime = 0.0f;
    GUIStyle style;
    Rect rect;

    bool isFullscreen = false;
    int windowedWidth = 900;
    int windowedHeight = 550;

    void Start()
    {
        // 창모드로 시작하면서 크기 지정
        Screen.SetResolution(windowedWidth, windowedHeight, false);

        // 현재 주사율 계산 후 FPS 설정
        Resolution currentRes = Screen.currentResolution;
        float refreshRate = (float)currentRes.refreshRateRatio.numerator / currentRes.refreshRateRatio.denominator;
        int targetFPS = Mathf.CeilToInt(refreshRate) + 10;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;
        Debug.Log($"주사율: {refreshRate}Hz, FPS 제한: {targetFPS}");

        // FPS 표시 스타일 설정
        int h = Screen.height;
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

        // F11 누르면 전체화면 전환
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullscreen();
        }
    }

    void ToggleFullscreen()
    {
        if (isFullscreen)
        {
            // 전체화면 → 창모드
            Screen.SetResolution(windowedWidth, windowedHeight, false);
            Screen.fullScreen = false;
        }
        else
        {
            // 창모드 → 전체화면 (전환 전에 창 크기 저장)
            windowedWidth = Screen.width;
            windowedHeight = Screen.height;

            Resolution currentRes = Screen.currentResolution;
            Screen.SetResolution(currentRes.width, currentRes.height, true);
        }

        isFullscreen = !isFullscreen;
    }

    void OnGUI()
    {
        float fps = 1.0f / deltaTime;
        string text = $"FPS: {fps:0.}";
        GUI.Label(rect, text, style);
    }
}
