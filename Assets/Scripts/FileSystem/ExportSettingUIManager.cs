using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameSystem;

namespace FileSystem
{
    public class ExportSettingUIManager : BaseManager
    {
        [Header("UI Elements")]
        public TMP_InputField fakePlayerInput;
        public TMP_InputField scoreboardNameInput;
        public TMP_InputField startTickInput;
        public TMP_InputField packNamespaceInput;
        public TMP_InputField frameFileNameInput;
        public TMP_InputField exportFolderInput; // ExportManager의 ExportFolder와 연동
        public Toggle findModeToggle;

        private ExportManager exportManager; // ExportManager 참조

        // 내부 데이터 저장용 (MCDEANIMFile과 동기화될 값들)
        public string fakePlayer = "anim";
        public string scoreboardName = "anim";
        public int startTick = 0;
        public string packNamespace = "PotanAnim:anim/";
        public string frameFileName = "frame";
        public bool useFindMode = true;
        // exportManager.ExportFolder는 exportManager가 직접 관리

        void Start()
        {
            exportManager = GameManager.GetManager<ExportManager>();

            // 이벤트 리스너 등록
            findModeToggle.onValueChanged.AddListener(OnFindModeToggleChanged);
            fakePlayerInput.onEndEdit.AddListener(OnEndEditFakePlayer);
            scoreboardNameInput.onEndEdit.AddListener(OnEndEditScoreboardName);
            startTickInput.onEndEdit.AddListener(OnEndEditStartTick);
            packNamespaceInput.onEndEdit.AddListener(OnEndEditPackNamespace);
            frameFileNameInput.onEndEdit.AddListener(OnEndEditFrameFileName);
            exportFolderInput.onEndEdit.AddListener(OnEndEditExportFolder);

            // 초기 UI 업데이트 (기본값 또는 로드된 값으로)
            UpdateUIFromData();
        }

        private void OnFindModeToggleChanged(bool value)
        {
            useFindMode = value;
            if (!useFindMode)
            {
                fakePlayerInput.text = "@s"; // fakePlayer 값도 @s로 변경할지, UI만 변경할지 결정 필요
                fakePlayer = "@s"; // 데이터도 변경
            }
            else
            {
                // FindMode가 true가 되면, 이전 _fakePlayer 값으로 복원하거나 기본값으로 설정
                // 여기서는 간단히 입력 필드를 이전 값으로 되돌리도록 유도 (또는 기본값 "anim" 설정)
                fakePlayerInput.text = fakePlayer == "@s" ? "anim" : fakePlayer;
            }
        }

        private void OnEndEditFakePlayer(string value)
        {
            if (!useFindMode)
            {
                fakePlayerInput.text = "@s";
                fakePlayer = "@s";
            }
            else
            {
                fakePlayer = value;
            }
        }

        private void OnEndEditScoreboardName(string value)
        {
            scoreboardName = value;
        }

        private void OnEndEditStartTick(string value)
        {
            if (int.TryParse(value, out var tick) && tick >= 0)
            {
                startTick = tick;
            }
            else
            {
                startTickInput.text = startTick.ToString();
            }
        }

        private void OnEndEditPackNamespace(string value)
        {
            packNamespace = value;
        }

        private void OnEndEditFrameFileName(string value)
        {
            frameFileName = value;
        }

        private void OnEndEditExportFolder(string value)
        {
            if (exportManager != null)
            {
                exportManager.ExportFolder = value;
            }
        }

        /// <summary>
        /// MCDEANIMFile 데이터로 UI와 내부 데이터를 업데이트합니다.
        /// </summary>
        public void LoadSettingsFromFile(MCDEANIMFile file)
        {
            scoreboardName = file.scoreboardName;
            startTick = file.startTick;
            packNamespace = file.packNamespace;
            frameFileName = file.frameFileName;
            fakePlayer = file.fakePlayer;
            useFindMode = file.findMode;

            exportManager.ExportFolder = file.resultFileName;
            exportManager.SetPathText(file.exportPath);
            UpdateUIFromData();
        }

        /// <summary>
        /// 현재 UI/내부 데이터를 MCDEANIMFile 객체에 적용합니다. (저장 시 호출)
        /// </summary>
        public void ApplySettingsToFile(MCDEANIMFile file)
        {
            file.scoreboardName = scoreboardName;
            file.startTick = startTick;
            file.packNamespace = packNamespace;
            file.frameFileName = frameFileName;
            file.fakePlayer = fakePlayer;
            file.findMode = useFindMode;


            file.resultFileName = exportManager.ExportFolder;
        }

        /// <summary>
        /// 내부 데이터를 기반으로 UI를 업데이트합니다.
        /// </summary>
        private void UpdateUIFromData()
        {
            scoreboardNameInput.text = scoreboardName;
            startTickInput.text = startTick.ToString();
            packNamespaceInput.text = packNamespace;
            frameFileNameInput.text = frameFileName;
            fakePlayerInput.text = fakePlayer; // _useFindMode에 따라 @s가 될 수 있음
            findModeToggle.isOn = useFindMode;
            if (exportManager != null)
            {
                exportFolderInput.text = exportManager.ExportFolder;
            }
        }
    }
}
