using System;
using Animation;
using GameSystem;
using TMPro;
using FileSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameSystem
{
    public class SettingManager : BaseManager
    {
        #region 변수
        /// <summary>
        /// 프레임 기본 틱 간격
        /// </summary>
        public int defaultInterpolation;

        /// <summary>
        /// 프레임 기본 보간 값
        /// </summary>
        public int defaultTickInterval;

        [SerializeField]
        private bool _useNameInfoExtract = true;
        /// <summary>
        /// 이름 정보 추출 사용 여부
        /// </summary>
        public bool UseNameInfoExtract
        {
            get => _useNameInfoExtract;
            set
            {
                _useNameInfoExtract = value;
                PlayerPrefs.SetInt("useNameInfoExtract", value ? 1 : 0);
            }
        }
        [SerializeField]
        private bool _useFrameTxtFile = true;
        /// <summary>
        /// 프레임 txt 파일 사용 여부
        /// </summary>
        public bool UseFrameTxtFile
        {
            get => _useFrameTxtFile;
            set
            {
                _useFrameTxtFile = value;
                PlayerPrefs.SetInt("useFrameTxtFile", value ? 1 : 0);
            }
        }

        /// <summary>
        /// 생성 모드
        /// </summary>
        public bool useFindMode = true;

        public string fakePlayer = "anim";
        public string scoreboardName = "anim";

        /// <summary>
        /// 내보냈을 때 최초 틱 
        /// </summary>
        public int startTick;
        /// <summary>
        /// 내보냈을 때 데이터팩 네임스페이스 
        /// </summary>
        public string packNamespace = "PotanAnim:anim/";
        /// <summary>
        /// 프레임 파일들의 이름
        /// </summary>
        public string frameFileName = "frame";
        /// <summary>
        /// 1초당 틱 수 
        /// </summary>
        public int tickUnit = 10;

        [FormerlySerializedAs("FindModeToggle")] public Toggle findModeToggle;
        public TMP_InputField[] inputFields;
        [FormerlySerializedAs("SettingPanel")] public GameObject settingPanel;

        public ExportManager exportManager;
        public BdEngineStyleCameraMovement cameraMovement;

        public Slider[] sliders;
        public Toggle[] toggleButtons;

        enum SliderType
        {
            CameraSpeed,
            CameraMoveSpeed,
            CameraZoomSpeed,
        }
        #endregion

        private void Start()
        {
            // 토글 이벤트 리스너 등록
            findModeToggle.onValueChanged.AddListener(
                value =>
                {
                    useFindMode = value;
                    if (!useFindMode)
                        inputFields[2].text = "@s";
                });

            // PlayerPrefs에서 값 불러오기 (키가 없으면 현재 필드에 지정된 기본값 사용)
            defaultTickInterval = PlayerPrefs.GetInt("defaultTickInterval", defaultTickInterval);
            defaultInterpolation = PlayerPrefs.GetInt("defaultInterpolation", defaultInterpolation);
            UseNameInfoExtract = PlayerPrefs.GetInt("useNameInfoExtract", UseNameInfoExtract ? 1 : 0) == 1;
            UseFrameTxtFile = PlayerPrefs.GetInt("useFrameTxtFile", UseFrameTxtFile ? 1 : 0) == 1;
            tickUnit = PlayerPrefs.GetInt("tickUnit", tickUnit);
            GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;

            cameraMovement.rotateSpeed = PlayerPrefs.GetFloat("cameraSpeed", cameraMovement.rotateSpeed);
            cameraMovement.panSpeed = PlayerPrefs.GetFloat("cameraMoveSpeed", cameraMovement.panSpeed);
            cameraMovement.zoomSpeed = PlayerPrefs.GetFloat("cameraZoomSpeed", cameraMovement.zoomSpeed);

            // 슬라이더에 불러온 값 반영
            sliders[(int)SliderType.CameraSpeed].value = cameraMovement.rotateSpeed;
            sliders[(int)SliderType.CameraMoveSpeed].value = cameraMovement.panSpeed;
            sliders[(int)SliderType.CameraZoomSpeed].value = cameraMovement.zoomSpeed;

            // inputFields에도 불러온 값 반영 (인덱스 순서에 맞게 할당)
            // 인덱스 0: defaultTickInterval
            inputFields[0].text = defaultTickInterval.ToString();
            // 인덱스 1: defaultInterpolation
            inputFields[1].text = defaultInterpolation.ToString();
            // 인덱스 2: fakePlayer (useFindMode가 false이면 "@s")
            inputFields[2].text = useFindMode ? fakePlayer : "@s";
            // 인덱스 3: scoreboardName
            inputFields[3].text = scoreboardName;
            // 인덱스 4: startTick
            inputFields[4].text = startTick.ToString();
            // 인덱스 5: packNamespace
            inputFields[5].text = packNamespace;
            // 인덱스 6: frameFileName
            inputFields[6].text = frameFileName;
            // 인덱스 7: exportManager.ExportPath
            inputFields[7].text = exportManager.ExportFolder;
            // 인덱스 8: tickUnit
            inputFields[8].text = tickUnit.ToString();

            toggleButtons[0].isOn = UseNameInfoExtract;
            toggleButtons[1].isOn = UseFrameTxtFile;
            // toggleButtons[2].isOn = useFindMode;
            // toggleButtons[3].isOn = !useFindMode;

            // 입력 필드 이벤트 리스너 등록
            for (var i = 0; i < inputFields.Length; i++)
            {
                int idx = i;
                inputFields[i].onEndEdit.AddListener(value => OnEndEditValue(value, idx));
            }

            // 슬라이더 이벤트 리스너 등록
            for (var i = 0; i < sliders.Length; i++)
            {
                int idx = i;
                sliders[i].onValueChanged.AddListener(value => OnSliderValueEdited(value, (SliderType)idx));
            }
        }

        public void LoadMCDEAnim(MCDEANIMFile file)
        {
            //Debug.Log(file.name);
            scoreboardName = file.scoreboardName;
            startTick = file.startTick;
            packNamespace = file.packNamespace;
            frameFileName = file.frameFileName;
            fakePlayer = file.fakePlayer;
            useFindMode = file.findMode;
            exportManager.ExportFolder = file.resultFileName;

            inputFields[3].text = scoreboardName;
            inputFields[4].text = startTick.ToString();
            inputFields[5].text = packNamespace;
            inputFields[6].text = frameFileName;
            inputFields[2].text = fakePlayer;

            findModeToggle.isOn = useFindMode;

            exportManager.SetPathText(file.exportPath);
        }


        public void SetSettingPanel(bool isOn)
        {
            settingPanel.SetActive(isOn);
            if (isOn)
            {
                UIManager.CurrentUIStatus |= UIManager.UIStatus.OnSettingPanel;
            }
            else
            {
                UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnSettingPanel;
            }
        }

        private void OnEndEditValue(string value, int idx)
        {
            //Debug.Log(idx);
            switch (idx)
            {
                case 0:
                    if (int.TryParse(value, out var tickInterval) && tickInterval >= 1)
                    {
                        defaultTickInterval = tickInterval;
                        PlayerPrefs.SetInt("defaultTickInterval", defaultTickInterval);
                    }
                    else
                    {
                        value = defaultTickInterval.ToString();
                    }
                    break;
                case 1:
                    if (int.TryParse(value, out var interpolation) && interpolation >= 0)
                    {
                        defaultInterpolation = interpolation;
                        PlayerPrefs.SetInt("defaultInterpolation", defaultInterpolation);
                    }
                    else
                    {
                        value = interpolation.ToString();
                    }
                    break;
                case 2:
                    if (!useFindMode)
                        value = "@s";
                    else
                        fakePlayer = value;
                    break;
                case 3:
                    scoreboardName = value;
                    break;
                case 4:
                    if (int.TryParse(value, out var tick) && tick >= 0)
                        startTick = tick;
                    else
                        value = startTick.ToString();
                    break;
                case 5:
                    packNamespace = value;
                    break;
                case 6:
                    frameFileName = value;
                    break;
                case 7:
                    exportManager.ExportFolder = value;
                    break;
                case 8:
                    if (int.TryParse(value, out var unit) && unit >= 1)
                    {
                        tickUnit = unit;
                        GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;
                        PlayerPrefs.SetInt("tickUnit", tickUnit);
                    }
                    else
                        value = tickUnit.ToString();
                    break;
            }
            inputFields[idx].text = value;
        }

        private void OnSliderValueEdited(float value, SliderType type)
        {
            switch (type)
            {
                case SliderType.CameraSpeed:
                    cameraMovement.rotateSpeed = value;
                    PlayerPrefs.SetFloat("cameraSpeed", value);
                    break;
                case SliderType.CameraMoveSpeed:
                    cameraMovement.panSpeed = value;
                    PlayerPrefs.SetFloat("cameraMoveSpeed", value);
                    break;
                case SliderType.CameraZoomSpeed:
                    cameraMovement.zoomSpeed = value;
                    PlayerPrefs.SetFloat("cameraZoomSpeed", value);
                    break;
            }
        }
    }
}
