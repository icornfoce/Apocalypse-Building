using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Simulation.UI
{
    /// <summary>
    /// ตัวควบคุมหน้าจอตั้งค่า (Options UI) จัดการสลับหน้าต่างย่อย (Tabs)
    /// ได้แก่ General, Graphics, Sound, Controls
    /// พร้อมระบบ Settings จริงที่บันทึก/โหลด/Reset ได้
    /// </summary>
    public class OptionsUI : MonoBehaviour
    {
        [System.Serializable]
        public class TabGroup
        {
            [Tooltip("หน้าต่างย่อย (Panel) ของแท็บนี้")]
            public GameObject panelObject;
            [Tooltip("ข้อความบนปุ่ม (สำหรับเปลี่ยนสี/สไตล์เพื่อระบุสถานะ active)")]
            public TextMeshProUGUI buttonText;
        }

        [Header("Tab Setup")]
        [SerializeField] private TabGroup generalTab;
        [SerializeField] private TabGroup graphicsTab;
        [SerializeField] private TabGroup soundTab;
        [SerializeField] private TabGroup controlsTab;

        [Header("Button Style Settings")]
        [Tooltip("สีของปุ่ม/ข้อความ เมื่อถูกเลือก")]
        [SerializeField] private Color activeColor = Color.white;
        [Tooltip("สีของปุ่ม/ข้อความ เมื่อไม่ได้เลือก")]
        [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

        // ─────────────────────────────────────────────
        //  General Tab — UI Controls
        // ─────────────────────────────────────────────

        [Header("═══ GENERAL SETTINGS ═══")]

        [Header("Language")]
        [Tooltip("Dropdown สำหรับเลือกภาษา (Thai, English, Japanese, Chinese)")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Resolution")]
        [Tooltip("Dropdown สำหรับเลือกความละเอียดหน้าจอ")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("VSync")]
        [Tooltip("Toggle เปิด/ปิด VSync")]
        [SerializeField] private Toggle vsyncToggle;

        [Header("Framerate")]
        [Tooltip("Dropdown สำหรับเลือก Framerate (30, 60, 120, 144, Unlimited)")]
        [SerializeField] private TMP_Dropdown framerateDropdown;

        [Header("Fullscreen")]
        [Tooltip("Toggle เปิด/ปิด Fullscreen")]
        [SerializeField] private Toggle fullscreenToggle;

        [Header("UI Scale")]
        [Tooltip("Slider ปรับขนาด UI (0.5 - 2.0)")]
        [SerializeField] private Slider uiScaleSlider;
        [Tooltip("แสดงค่า UI Scale ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI uiScaleValueText;

        [Header("Show Tutorials")]
        [Tooltip("Toggle เปิด/ปิด แสดง Tutorials")]
        [SerializeField] private Toggle showTutorialsToggle;

        [Header("Tooltips")]
        [Tooltip("Toggle เปิด/ปิด Tooltips")]
        [SerializeField] private Toggle tooltipsToggle;

        [Header("Show Node Building Grid")]
        [Tooltip("Toggle เปิด/ปิด แสดงตาราง Node Building")]
        [SerializeField] private Toggle showNodeBuildingGridToggle;

        [Header("Camera Sensitivity")]
        [Tooltip("Slider ปรับความไวกล้อง (0.1 - 3.0)")]
        [SerializeField] private Slider cameraSensitivitySlider;
        [Tooltip("แสดงค่า Camera Sensitivity ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI cameraSensitivityValueText;

        [Header("Camera Shake")]
        [Tooltip("Toggle เปิด/ปิด Camera Shake")]
        [SerializeField] private Toggle cameraShakeToggle;

        [Header("Occlusion Transparency")]
        [Tooltip("Slider ปรับความโปร่งใส Occlusion (0.0 - 1.0)")]
        [SerializeField] private Slider occlusionTransparencySlider;
        [Tooltip("แสดงค่า Occlusion Transparency ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI occlusionTransparencyValueText;

        [Header("General Reset Button")]
        [Tooltip("ปุ่ม Reset General Settings กลับเป็นค่าเริ่มต้น")]
        [SerializeField] private Button resetGeneralButton;

        // ─────────────────────────────────────────────
        //  Sound Tab — UI Controls
        // ─────────────────────────────────────────────

        [Header("═══ SOUND SETTINGS ═══")]

        [Header("Master Volume")]
        [Tooltip("Slider ปรับ Master Volume (0 - 1)")]
        [SerializeField] private Slider masterVolumeSlider;
        [Tooltip("แสดงค่า Master Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI masterVolumeValueText;

        [Header("Music Volume")]
        [Tooltip("Slider ปรับ Music Volume (0 - 1)")]
        [SerializeField] private Slider musicVolumeSlider;
        [Tooltip("แสดงค่า Music Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI musicVolumeValueText;

        [Header("Ambience Volume")]
        [Tooltip("Slider ปรับ Ambience Volume (0 - 1)")]
        [SerializeField] private Slider ambienceVolumeSlider;
        [Tooltip("แสดงค่า Ambience Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI ambienceVolumeValueText;

        [Header("UI Volume")]
        [Tooltip("Slider ปรับ UI Volume (0 - 1)")]
        [SerializeField] private Slider uiVolumeSlider;
        [Tooltip("แสดงค่า UI Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI uiVolumeValueText;

        [Header("SFX Volume")]
        [Tooltip("Slider ปรับ SFX Volume (0 - 1)")]
        [SerializeField] private Slider sfxVolumeSlider;
        [Tooltip("แสดงค่า SFX Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI sfxVolumeValueText;

        [Header("Physics Volume")]
        [Tooltip("Slider ปรับ Physics Volume (0 - 1)")]
        [SerializeField] private Slider physicsVolumeSlider;
        [Tooltip("แสดงค่า Physics Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI physicsVolumeValueText;

        [Header("Block Volume")]
        [Tooltip("Slider ปรับ Block Volume (0 - 1)")]
        [SerializeField] private Slider blockVolumeSlider;
        [Tooltip("แสดงค่า Block Volume ปัจจุบัน (ถ้ามี)")]
        [SerializeField] private TextMeshProUGUI blockVolumeValueText;

        [Header("Sound Reset Button")]
        [Tooltip("ปุ่ม Reset Sound Settings กลับเป็นค่าเริ่มต้น")]
        [SerializeField] private Button resetSoundButton;

        // ─────────────────────────────────────────────
        //  Flag ป้องกัน Listener ยิงซ้ำตอนโหลดค่า
        // ─────────────────────────────────────────────
        private bool _isLoadingValues = false;

        // ═════════════════════════════════════════════
        //  Unity Lifecycle
        // ═════════════════════════════════════════════

        private void Start()
        {
            SetupGeneralControls();
            SetupSoundControls();
            RegisterListeners();

            // เปิดหน้าแรก (General) เป็นค่าเริ่มต้น
            ShowGeneral();
        }

        private void OnEnable()
        {
            // โหลดค่าจาก GameSettings มาแสดงบน UI ทุกครั้งที่เปิดหน้า Options
            LoadAllValues();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        // ═════════════════════════════════════════════
        //  SETUP — ตั้งค่า UI Controls
        // ═════════════════════════════════════════════

        /// <summary>
        /// ตั้งค่า Dropdown และ Slider ranges สำหรับ General Settings
        /// </summary>
        private void SetupGeneralControls()
        {
            // Language Dropdown
            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new List<string>(GameSettings.LanguageOptions));
            }

            // Resolution Dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new List<string>(GameSettings.GetResolutionLabels()));
            }

            // Framerate Dropdown
            if (framerateDropdown != null)
            {
                framerateDropdown.ClearOptions();
                framerateDropdown.AddOptions(new List<string>(GameSettings.FramerateLabels));
            }

            // UI Scale Slider range
            if (uiScaleSlider != null)
            {
                uiScaleSlider.minValue = 0.5f;
                uiScaleSlider.maxValue = 2.0f;
            }

            // Camera Sensitivity Slider range
            if (cameraSensitivitySlider != null)
            {
                cameraSensitivitySlider.minValue = 0.1f;
                cameraSensitivitySlider.maxValue = 3.0f;
            }

            // Occlusion Transparency Slider range
            if (occlusionTransparencySlider != null)
            {
                occlusionTransparencySlider.minValue = 0.0f;
                occlusionTransparencySlider.maxValue = 1.0f;
            }
        }

        /// <summary>
        /// ตั้งค่า Slider ranges สำหรับ Sound Settings
        /// </summary>
        private void SetupSoundControls()
        {
            SetupVolumeSlider(masterVolumeSlider);
            SetupVolumeSlider(musicVolumeSlider);
            SetupVolumeSlider(ambienceVolumeSlider);
            SetupVolumeSlider(uiVolumeSlider);
            SetupVolumeSlider(sfxVolumeSlider);
            SetupVolumeSlider(physicsVolumeSlider);
            SetupVolumeSlider(blockVolumeSlider);
        }

        private void SetupVolumeSlider(Slider slider)
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
            }
        }

        // ═════════════════════════════════════════════
        //  LOAD — โหลดค่าจาก GameSettings มาแสดงบน UI
        // ═════════════════════════════════════════════

        /// <summary>
        /// โหลดค่า Settings ทั้งหมดจาก GameSettings มาแสดงบน UI Controls
        /// </summary>
        private void LoadAllValues()
        {
            _isLoadingValues = true;

            LoadGeneralValues();
            LoadSoundValues();

            _isLoadingValues = false;
        }

        private void LoadGeneralValues()
        {
            // Language
            if (languageDropdown != null)
            {
                int langIndex = GameSettings.LoadLanguage();
                languageDropdown.value = Mathf.Clamp(langIndex, 0, GameSettings.LanguageOptions.Length - 1);
            }

            // Resolution
            if (resolutionDropdown != null)
            {
                int resIndex = GameSettings.LoadResolutionIndex();
                var resolutions = GameSettings.GetAvailableResolutions();
                if (resIndex < 0 || resIndex >= resolutions.Length)
                {
                    // หาตัวที่ตรงกับ resolution ปัจจุบัน
                    resIndex = FindCurrentResolutionIndex(resolutions);
                }
                resolutionDropdown.value = Mathf.Clamp(resIndex, 0, resolutions.Length - 1);
            }

            // VSync
            if (vsyncToggle != null)
                vsyncToggle.isOn = GameSettings.LoadVSync();

            // Framerate
            if (framerateDropdown != null)
            {
                int fps = GameSettings.LoadFramerate();
                int fpsIndex = FindFramerateIndex(fps);
                framerateDropdown.value = fpsIndex;
            }

            // Fullscreen
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = GameSettings.LoadFullscreen();

            // UI Scale
            if (uiScaleSlider != null)
            {
                uiScaleSlider.value = GameSettings.LoadUIScale();
                UpdateValueText(uiScaleValueText, uiScaleSlider.value, "x{0:F1}");
            }

            // Show Tutorials
            if (showTutorialsToggle != null)
                showTutorialsToggle.isOn = GameSettings.LoadShowTutorials();

            // Tooltips
            if (tooltipsToggle != null)
                tooltipsToggle.isOn = GameSettings.LoadTooltips();

            // Show Node Building Grid
            if (showNodeBuildingGridToggle != null)
                showNodeBuildingGridToggle.isOn = GameSettings.LoadShowNodeBuildingGrid();

            // Camera Sensitivity
            if (cameraSensitivitySlider != null)
            {
                cameraSensitivitySlider.value = GameSettings.LoadCameraSensitivity();
                UpdateValueText(cameraSensitivityValueText, cameraSensitivitySlider.value, "x{0:F1}");
            }

            // Camera Shake
            if (cameraShakeToggle != null)
                cameraShakeToggle.isOn = GameSettings.LoadCameraShake();

            // Occlusion Transparency
            if (occlusionTransparencySlider != null)
            {
                occlusionTransparencySlider.value = GameSettings.LoadOcclusionTransparency();
                UpdateValueText(occlusionTransparencyValueText, occlusionTransparencySlider.value, "{0:P0}");
            }
        }

        private void LoadSoundValues()
        {
            SetSliderValue(masterVolumeSlider, GameSettings.LoadMasterVolume(), masterVolumeValueText);
            SetSliderValue(musicVolumeSlider, GameSettings.LoadMusicVolume(), musicVolumeValueText);
            SetSliderValue(ambienceVolumeSlider, GameSettings.LoadAmbienceVolume(), ambienceVolumeValueText);
            SetSliderValue(uiVolumeSlider, GameSettings.LoadUIVolume(), uiVolumeValueText);
            SetSliderValue(sfxVolumeSlider, GameSettings.LoadSFXVolume(), sfxVolumeValueText);
            SetSliderValue(physicsVolumeSlider, GameSettings.LoadPhysicsVolume(), physicsVolumeValueText);
            SetSliderValue(blockVolumeSlider, GameSettings.LoadBlockVolume(), blockVolumeValueText);
        }

        private void SetSliderValue(Slider slider, float value, TextMeshProUGUI valueText)
        {
            if (slider != null)
            {
                slider.value = value;
                UpdateValueText(valueText, value, "{0:P0}");
            }
        }

        // ═════════════════════════════════════════════
        //  REGISTER / UNREGISTER Listeners
        // ═════════════════════════════════════════════

        private void RegisterListeners()
        {
            // ── General ──
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            if (framerateDropdown != null)
                framerateDropdown.onValueChanged.AddListener(OnFramerateChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);

            if (showTutorialsToggle != null)
                showTutorialsToggle.onValueChanged.AddListener(OnShowTutorialsChanged);

            if (tooltipsToggle != null)
                tooltipsToggle.onValueChanged.AddListener(OnTooltipsChanged);

            if (showNodeBuildingGridToggle != null)
                showNodeBuildingGridToggle.onValueChanged.AddListener(OnShowNodeBuildingGridChanged);

            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);

            if (cameraShakeToggle != null)
                cameraShakeToggle.onValueChanged.AddListener(OnCameraShakeChanged);

            if (occlusionTransparencySlider != null)
                occlusionTransparencySlider.onValueChanged.AddListener(OnOcclusionTransparencyChanged);

            if (resetGeneralButton != null)
                resetGeneralButton.onClick.AddListener(OnResetGeneralClicked);

            // ── Sound ──
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (ambienceVolumeSlider != null)
                ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);

            if (uiVolumeSlider != null)
                uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (physicsVolumeSlider != null)
                physicsVolumeSlider.onValueChanged.AddListener(OnPhysicsVolumeChanged);

            if (blockVolumeSlider != null)
                blockVolumeSlider.onValueChanged.AddListener(OnBlockVolumeChanged);

            if (resetSoundButton != null)
                resetSoundButton.onClick.AddListener(OnResetSoundClicked);
        }

        private void UnregisterListeners()
        {
            // ── General ──
            if (languageDropdown != null)
                languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);

            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);

            if (framerateDropdown != null)
                framerateDropdown.onValueChanged.RemoveListener(OnFramerateChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.RemoveListener(OnUIScaleChanged);

            if (showTutorialsToggle != null)
                showTutorialsToggle.onValueChanged.RemoveListener(OnShowTutorialsChanged);

            if (tooltipsToggle != null)
                tooltipsToggle.onValueChanged.RemoveListener(OnTooltipsChanged);

            if (showNodeBuildingGridToggle != null)
                showNodeBuildingGridToggle.onValueChanged.RemoveListener(OnShowNodeBuildingGridChanged);

            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.RemoveListener(OnCameraSensitivityChanged);

            if (cameraShakeToggle != null)
                cameraShakeToggle.onValueChanged.RemoveListener(OnCameraShakeChanged);

            if (occlusionTransparencySlider != null)
                occlusionTransparencySlider.onValueChanged.RemoveListener(OnOcclusionTransparencyChanged);

            if (resetGeneralButton != null)
                resetGeneralButton.onClick.RemoveListener(OnResetGeneralClicked);

            // ── Sound ──
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (ambienceVolumeSlider != null)
                ambienceVolumeSlider.onValueChanged.RemoveListener(OnAmbienceVolumeChanged);

            if (uiVolumeSlider != null)
                uiVolumeSlider.onValueChanged.RemoveListener(OnUIVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (physicsVolumeSlider != null)
                physicsVolumeSlider.onValueChanged.RemoveListener(OnPhysicsVolumeChanged);

            if (blockVolumeSlider != null)
                blockVolumeSlider.onValueChanged.RemoveListener(OnBlockVolumeChanged);

            if (resetSoundButton != null)
                resetSoundButton.onClick.RemoveListener(OnResetSoundClicked);
        }

        // ═════════════════════════════════════════════
        //  CALLBACKS — General Settings
        // ═════════════════════════════════════════════

        private void OnLanguageChanged(int index)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveLanguage(index);
            Debug.Log($"[OptionsUI] Language → {GameSettings.LanguageOptions[index]}");
        }

        private void OnResolutionChanged(int index)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveResolutionIndex(index);
            GameSettings.ApplyResolution(index, GameSettings.LoadFullscreen());
            Debug.Log($"[OptionsUI] Resolution → index {index}");
        }

        private void OnVSyncChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveVSync(value);
            GameSettings.ApplyVSync(value);
            Debug.Log($"[OptionsUI] VSync → {value}");
        }

        private void OnFramerateChanged(int index)
        {
            if (_isLoadingValues) return;
            if (index >= 0 && index < GameSettings.FramerateOptions.Length)
            {
                int fps = GameSettings.FramerateOptions[index];
                GameSettings.SaveFramerate(fps);
                GameSettings.ApplyFramerate(fps);
                Debug.Log($"[OptionsUI] Framerate → {GameSettings.FramerateLabels[index]}");
            }
        }

        private void OnFullscreenChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveFullscreen(value);
            GameSettings.ApplyFullscreen(value);
            Debug.Log($"[OptionsUI] Fullscreen → {value}");
        }

        private void OnUIScaleChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveUIScale(value);
            UpdateValueText(uiScaleValueText, value, "x{0:F1}");
            Debug.Log($"[OptionsUI] UI Scale → {value:F1}");
        }

        private void OnShowTutorialsChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveShowTutorials(value);
            Debug.Log($"[OptionsUI] Show Tutorials → {value}");
        }

        private void OnTooltipsChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveTooltips(value);
            Debug.Log($"[OptionsUI] Tooltips → {value}");
        }

        private void OnShowNodeBuildingGridChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveShowNodeBuildingGrid(value);
            Debug.Log($"[OptionsUI] Show Node Building Grid → {value}");
        }

        private void OnCameraSensitivityChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveCameraSensitivity(value);
            UpdateValueText(cameraSensitivityValueText, value, "x{0:F1}");
            Debug.Log($"[OptionsUI] Camera Sensitivity → {value:F1}");
        }

        private void OnCameraShakeChanged(bool value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveCameraShake(value);
            Debug.Log($"[OptionsUI] Camera Shake → {value}");
        }

        private void OnOcclusionTransparencyChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveOcclusionTransparency(value);
            UpdateValueText(occlusionTransparencyValueText, value, "{0:P0}");
            Debug.Log($"[OptionsUI] Occlusion Transparency → {value:P0}");
        }

        // ═════════════════════════════════════════════
        //  CALLBACKS — Sound Settings
        // ═════════════════════════════════════════════

        private void OnMasterVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveMasterVolume(value);
            UpdateValueText(masterVolumeValueText, value, "{0:P0}");

            // Master Volume มีผลกับทุก channel ดังนั้นต้อง re-apply ทั้งหมด
            GameSettings.ApplyMusicVolume();
            GameSettings.ApplySFXVolume();
            Debug.Log($"[OptionsUI] Master Volume → {value:P0}");
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveMusicVolume(value);
            UpdateValueText(musicVolumeValueText, value, "{0:P0}");
            GameSettings.ApplyMusicVolume();
            Debug.Log($"[OptionsUI] Music Volume → {value:P0}");
        }

        private void OnAmbienceVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveAmbienceVolume(value);
            UpdateValueText(ambienceVolumeValueText, value, "{0:P0}");
            Debug.Log($"[OptionsUI] Ambience Volume → {value:P0}");
        }

        private void OnUIVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveUIVolume(value);
            UpdateValueText(uiVolumeValueText, value, "{0:P0}");
            Debug.Log($"[OptionsUI] UI Volume → {value:P0}");
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveSFXVolume(value);
            UpdateValueText(sfxVolumeValueText, value, "{0:P0}");
            GameSettings.ApplySFXVolume();
            Debug.Log($"[OptionsUI] SFX Volume → {value:P0}");
        }

        private void OnPhysicsVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SavePhysicsVolume(value);
            UpdateValueText(physicsVolumeValueText, value, "{0:P0}");
            Debug.Log($"[OptionsUI] Physics Volume → {value:P0}");
        }

        private void OnBlockVolumeChanged(float value)
        {
            if (_isLoadingValues) return;
            GameSettings.SaveBlockVolume(value);
            UpdateValueText(blockVolumeValueText, value, "{0:P0}");
            Debug.Log($"[OptionsUI] Block Volume → {value:P0}");
        }

        // ═════════════════════════════════════════════
        //  RESET Callbacks
        // ═════════════════════════════════════════════

        /// <summary>
        /// กดปุ่ม Reset General — รีเซ็ตค่า General ทั้งหมดกลับเป็นค่าเริ่มต้น
        /// </summary>
        private void OnResetGeneralClicked()
        {
            GameSettings.ResetGeneralToDefaults();
            LoadAllValues(); // โหลดค่าใหม่กลับมาแสดงบน UI
            Debug.Log("[OptionsUI] General Settings → Reset to Defaults");
        }

        /// <summary>
        /// กดปุ่ม Reset Sound — รีเซ็ตค่า Sound ทั้งหมดกลับเป็นค่าเริ่มต้น
        /// </summary>
        private void OnResetSoundClicked()
        {
            GameSettings.ResetSoundToDefaults();
            LoadAllValues(); // โหลดค่าใหม่กลับมาแสดงบน UI
            Debug.Log("[OptionsUI] Sound Settings → Reset to Defaults");
        }

        // ═════════════════════════════════════════════
        //  TAB SWITCHING (เดิม)
        // ═════════════════════════════════════════════

        /// <summary>
        /// สลับแท็บไปที่ General
        /// </summary>
        public void ShowGeneral()
        {
            SwitchTab(generalTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Graphics
        /// </summary>
        public void ShowGraphics()
        {
            SwitchTab(graphicsTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Sound
        /// </summary>
        public void ShowSound()
        {
            SwitchTab(soundTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Controls
        /// </summary>
        public void ShowControls()
        {
            SwitchTab(controlsTab);
        }

        /// <summary>
        /// เมธอดหลักสำหรับสลับหน้าต่างและปรับแต่งสไตล์ปุ่ม
        /// </summary>
        private void SwitchTab(TabGroup targetTab)
        {
            if (targetTab == null) return;

            // จัดการแท็บ General
            SetTabActive(generalTab, targetTab == generalTab);

            // จัดการแท็บ Graphics
            SetTabActive(graphicsTab, targetTab == graphicsTab);

            // จัดการแท็บ Sound
            SetTabActive(soundTab, targetTab == soundTab);

            // จัดการแท็บ Controls
            SetTabActive(controlsTab, targetTab == controlsTab);
        }

        /// <summary>
        /// เปิด/ปิด Panel และเปลี่ยนสีปุ่มของแท็บตามสถานะการเลือก
        /// </summary>
        private void SetTabActive(TabGroup tab, bool isActive)
        {
            if (tab == null) return;

            // เปิด/ปิด Panel
            if (tab.panelObject != null)
            {
                tab.panelObject.SetActive(isActive);
            }

            // เปลี่ยนสีข้อความเพื่อบอกสถานะ Active/Inactive
            if (tab.buttonText != null)
            {
                tab.buttonText.color = isActive ? activeColor : inactiveColor;
            }
        }

        // ═════════════════════════════════════════════
        //  HELPER METHODS
        // ═════════════════════════════════════════════

        /// <summary>
        /// อัปเดตข้อความแสดงค่า (ถ้ามี TextMeshProUGUI)
        /// </summary>
        private void UpdateValueText(TextMeshProUGUI text, float value, string format)
        {
            if (text != null)
            {
                text.text = string.Format(format, value);
            }
        }

        /// <summary>
        /// หา index ของ Framerate ใน FramerateOptions array
        /// </summary>
        private int FindFramerateIndex(int framerate)
        {
            for (int i = 0; i < GameSettings.FramerateOptions.Length; i++)
            {
                if (GameSettings.FramerateOptions[i] == framerate)
                    return i;
            }
            return 1; // Default = 60 FPS (index 1)
        }

        /// <summary>
        /// หา index ของ Resolution ปัจจุบันใน array
        /// </summary>
        private int FindCurrentResolutionIndex(Resolution[] resolutions)
        {
            int currentWidth = Screen.currentResolution.width;
            int currentHeight = Screen.currentResolution.height;

            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == currentWidth && resolutions[i].height == currentHeight)
                    return i;
            }
            return resolutions.Length > 0 ? resolutions.Length - 1 : 0;
        }
    }
}
