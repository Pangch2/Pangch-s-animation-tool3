using SFB;
using TMPro;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    public Setting setting;

    void Awake()
    {
        DontDestroyOnLoad(this);

        if (instance == null) instance = this;
        else Destroy(this);

        CreatSetting();
    }
    void Start()
    {

    }


    void Update()
    {

    }
    void CreatSetting()
    {
        Debug.Log("너의 세팅은 생성된샌즈");
        setting = new Setting();
    }
    public void SaveSettingFile()
    {
        string explorerPath = "";
        var extensions = new[] { new ExtensionFilter("팽치류 세이브 파일", "pangch") };
        string filePath = StandaloneFileBrowser.SaveFilePanel("팽팽치류 세이브 불러오기", explorerPath, "팽치류세이브파일.pangch", extensions);

        setting.SaveToFile(filePath);
    }


    public GameObject uiRoot; // "UI모아두기" 오브젝트 (Inspector에서 연결)
    public void LoadSettingFile()
    {
        Debug.Log("세팅 파일이 로딩됨.");
        string explorerPath = "";


        var extensions = new[] { new ExtensionFilter("팽팽치류 세팅 파일", "pangch") };
        string[] filePath = StandaloneFileBrowser.OpenFilePanel("팽팽치류 세팅 불러오기", explorerPath, extensions, false);

        if (filePath.Length == 0)
        {
            Debug.Log("끼에에에ㅔ겍에ㅔㄱㅇ");
        }
        else
        {
            setting = FileManager.LoadSettingFromFile(filePath[0]);

            // 여기부터 UI에 바로 적용
            if (uiRoot == null)
            {
                Debug.LogWarning("uiRoot가 연결이 안대ㅔ이짜나!!!!!!!");
                return;
            }

            uiRoot.transform.Find("팽치류 저장위치").GetComponent<TMP_InputField>().text = setting.pangchFolderPath;
            uiRoot.transform.Find("데이터팩 저장위치/데펙위치").GetComponent<TMP_InputField>().text = setting.datapackSavePath;
            uiRoot.transform.Find("데이터팩 이름/데팩이름").GetComponent<TMP_InputField>().text = setting.selectDatapackName;

            var dropdown = uiRoot.transform.Find("생성모드/생성모드 선택기").GetComponent<TMP_Dropdown>();
            if (setting.generationMode >= 0 && setting.generationMode < dropdown.options.Count)
            {
                dropdown.value = setting.generationMode;
                dropdown.RefreshShownValue();
            }

            uiRoot.transform.Find("스코어오브젝티브/스코어오브젝트이름").GetComponent<TMP_InputField>().text = setting.scoreObjectName;
            uiRoot.transform.Find("스코어플레이어즈/스코어플레이어즈이름").GetComponent<TMP_InputField>().text = setting.scoreTempPlayerName;
            uiRoot.transform.Find("보간값입력/interpolation_duration").GetComponent<TMP_InputField>().text = setting.defaultInterpolationDuration.ToString();
            uiRoot.transform.Find("스코어증가값입력/스코어증가").GetComponent<TMP_InputField>().text = setting.defaultScoreIncreaseValue.ToString();
            uiRoot.transform.Find("시작 스코어 값/시작 스코어").GetComponent<TMP_InputField>().text = setting.startScoreValue.ToString();

            uiRoot.transform.Find("프레임 저장위치/프레임 저장").GetComponent<TMP_InputField>().text = setting.frameSavePath;
            uiRoot.transform.Find("스코어 제어기 위치/스코어 제어위치").GetComponent<TMP_InputField>().text = setting.scoreSavePath;
            uiRoot.transform.Find("스코어 제어기 이름/스코어 제어이름").GetComponent<TMP_InputField>().text = setting.scoreSaveName;

            //여긴 혹시몰라서
            //var extensions = new[] { new ExtensionFilter("팽팽치류 세팅 파일", "pangch") };
            //string[] filePath = StandaloneFileBrowser.OpenFilePanel("팽팽치류 세팅 불러오기", explorerPath, extensions, false);

            //if (filePath.Length == 0) Debug.Log("끼에에에ㅔ겍에ㅔㄱㅇ");
            //else setting = FileManager.LoadSettingFromFile(filePath[0]);
        }
    }
}
