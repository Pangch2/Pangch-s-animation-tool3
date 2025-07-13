using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool isInitialized = false;
    async void Start()
    {
        if (Application.isEditor) return; // 에디터에서는 초기화하지 않음

        await UnityServices.InitializeAsync();
        AnalyticsService.Instance.StartDataCollection();
        isInitialized = true;
    }
}
