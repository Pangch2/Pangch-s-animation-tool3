
using System.Collections.Generic;
using System.Linq;
using Animation.AnimFrame;
using FileSystem;
using FileSystem.Export;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandLineManager : MonoBehaviour
{
    public List<CommandLine> commandLines = new List<CommandLine>();
    public Transform commandLineParent;
    public TMP_InputField commandLineInputField;

    public CommandLine commandLinePrefab;

    public string Target => exportSettingUIManager.fakePlayer;
    public string Scoreboard => exportSettingUIManager.scoreboardName;
    public int LastScore => GetLastTick();

    public string[] commandPresets;
    public Button[] commandPresetButtons;

    private ExportSettingUIManager exportSettingUIManager;


    void Start()
    {
        exportSettingUIManager = GameManager.GetManager<ExportSettingUIManager>();
    }

    void OnEnable()
    {
        UpdatePresetLines();        
    }

    public void AddCommandLine(string commandLineText, int presetIndex = -1)
    {
        CommandLine newCommandLine = Instantiate(commandLinePrefab, commandLineParent);
        newCommandLine.Init(commandLineText, presetIndex, this);
        commandLines.Add(newCommandLine);
    }

    public void AddCommandLineFromPreset(int presetIndex)
    {
        commandPresetButtons[presetIndex].interactable = false; // 버튼 비활성화
        string formattedCommand = string.Format(commandPresets[presetIndex], Target, Scoreboard, LastScore);
        AddCommandLine(formattedCommand, presetIndex);
    }

    public void RemoveCommandLine(CommandLine commandLine)
    {
        if (commandLines.Contains(commandLine))
        {
            if (commandLine.presetIndex >= 0)
            {
                commandPresetButtons[commandLine.presetIndex].interactable = true; // 버튼 활성화
            }

            commandLines.Remove(commandLine);
            Destroy(commandLine.gameObject);
        }
    }

    public void OnAddButtonClicked()
    {
        string commandLineText = commandLineInputField.text.Trim();
        if (!string.IsNullOrEmpty(commandLineText))
        {
            AddCommandLine(commandLineText);
            commandLineInputField.text = string.Empty; // 입력 필드 초기화
        }
    }

    public void SetMCDEAnimFile(MCDEANIMFile file)
    {
        file.commandLines.Clear();
        foreach (var commandLine in commandLines)
        {
            file.commandLines.Add(commandLine.commandLineText);
        }
    }

    public void LoadMCDEAFile(MCDEANIMFile file)
    {
        for (int i = commandLines.Count - 1; i >= 0; i--)
        {
            CommandLine commandLine = commandLines[i];
            if (commandLine.presetIndex >= 0)
            {
                commandPresetButtons[commandLine.presetIndex].interactable = true; // 버튼 활성화
            }
            Destroy(commandLine.gameObject);
        }
        commandLines.Clear();
        
        foreach (var commandLineText in file.commandLines)
        {
            AddCommandLine(commandLineText);
        }
    }

    public void UpdatePresetLines()
    {
        for (int i = 0; i < commandPresets.Length; i++)
        {
            if (commandPresetButtons[i].interactable == false)
            {
                CommandLine commandLine = commandLines.Find(cl => cl.presetIndex == i);
                commandLine?.UpdateText(string.Format(commandPresets[i], Target, Scoreboard, LastScore));
            }
        }
    }

    public int GetLastTick()
    {
        AnimObjList animObjList = GameManager.GetManager<AnimObjList>();

        int tick = 0;
        foreach (var animObj in animObjList.animObjects)
        {
            tick = Mathf.Max(tick, animObj.frames.Values[^1].tick);
        }
        return tick;
    }
}