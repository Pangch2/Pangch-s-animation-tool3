using FileSystem;
using TMPro;
using UnityEngine;

public class CommandLine : MonoBehaviour
{
    public string commandLineText = string.Empty;
    public int presetIndex = -1; // -1이면 사용자 입력, 0 이상이면 프리셋 인덱스

    public TextMeshProUGUI commandLineTextUI;
    CommandLineManager manager;

    public void Init(string commandLine, int presetIndex, CommandLineManager commandLineManager)
    {
        UpdateText(commandLine);
        this.presetIndex = presetIndex;

        manager = commandLineManager;
    }

    public void UpdateText(string newText)
    {
        commandLineText = newText;
        commandLineTextUI.text = commandLineText;
    }

    public void OnRemoveClicked()
    {
        manager.RemoveCommandLine(this);
    }


}
