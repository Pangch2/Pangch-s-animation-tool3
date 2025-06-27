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
        Debug.Log("���� ������ �����Ȼ���");
        setting = new Setting();
    }
    public void SaveSettingFile()
    {
        string explorerPath = "";
        var extensions = new[] { new ExtensionFilter("��ġ�� ���̺� ����", "pangch") };
        string filePath = StandaloneFileBrowser.SaveFilePanel("����ġ�� ���̺� �ҷ�����", explorerPath, "��ġ�����̺�����.pangch", extensions);

        setting.SaveToFile(filePath);
    }


    public GameObject uiRoot; // "UI��Ƶα�" ������Ʈ (Inspector���� ����)
    public void LoadSettingFile()
    {
        Debug.Log("���� ������ �ε���.");
        string explorerPath = "";


        var extensions = new[] { new ExtensionFilter("����ġ�� ���� ����", "pangch") };
        string[] filePath = StandaloneFileBrowser.OpenFilePanel("����ġ�� ���� �ҷ�����", explorerPath, extensions, false);

        if (filePath.Length == 0)
        {
            Debug.Log("���������ā����Ĥ���");
        }
        else
        {
            setting = FileManager.LoadSettingFromFile(filePath[0]);

            // ������� UI�� �ٷ� ����
            if (uiRoot == null)
            {
                Debug.LogWarning("uiRoot�� ������ �ȴ����¥��!!!!!!!");
                return;
            }

            uiRoot.transform.Find("��ġ�� ������ġ").GetComponent<TMP_InputField>().text = setting.pangchFolderPath;
            uiRoot.transform.Find("�������� ������ġ/������ġ").GetComponent<TMP_InputField>().text = setting.datapackSavePath;
            uiRoot.transform.Find("�������� �̸�/�����̸�").GetComponent<TMP_InputField>().text = setting.selectDatapackName;

            var dropdown = uiRoot.transform.Find("�������/������� ���ñ�").GetComponent<TMP_Dropdown>();
            if (setting.generationMode >= 0 && setting.generationMode < dropdown.options.Count)
            {
                dropdown.value = setting.generationMode;
                dropdown.RefreshShownValue();
            }

            uiRoot.transform.Find("���ھ������Ƽ��/���ھ������Ʈ�̸�").GetComponent<TMP_InputField>().text = setting.scoreObjectName;
            uiRoot.transform.Find("���ھ��÷��̾���/���ھ��÷��̾����̸�").GetComponent<TMP_InputField>().text = setting.scoreTempPlayerName;
            uiRoot.transform.Find("�������Է�/interpolation_duration").GetComponent<TMP_InputField>().text = setting.defaultInterpolationDuration.ToString();
            uiRoot.transform.Find("���ھ��������Է�/���ھ�����").GetComponent<TMP_InputField>().text = setting.defaultScoreIncreaseValue.ToString();
            uiRoot.transform.Find("���� ���ھ� ��/���� ���ھ�").GetComponent<TMP_InputField>().text = setting.startScoreValue.ToString();

            uiRoot.transform.Find("������ ������ġ/������ ����").GetComponent<TMP_InputField>().text = setting.frameSavePath;
            uiRoot.transform.Find("���ھ� ����� ��ġ/���ھ� ������ġ").GetComponent<TMP_InputField>().text = setting.scoreSavePath;
            uiRoot.transform.Find("���ھ� ����� �̸�/���ھ� �����̸�").GetComponent<TMP_InputField>().text = setting.scoreSaveName;

            //���� Ȥ�ø���
            //var extensions = new[] { new ExtensionFilter("����ġ�� ���� ����", "pangch") };
            //string[] filePath = StandaloneFileBrowser.OpenFilePanel("����ġ�� ���� �ҷ�����", explorerPath, extensions, false);

            //if (filePath.Length == 0) Debug.Log("���������ā����Ĥ���");
            //else setting = FileManager.LoadSettingFromFile(filePath[0]);
        }
    }
}
