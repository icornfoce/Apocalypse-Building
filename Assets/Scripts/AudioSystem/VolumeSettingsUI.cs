using UnityEngine;
using UnityEngine.UI;
using Simulation.UI;

namespace AudioSystem
{
    /// <summary>
    /// VolumeSettingsUI - ตัวควบคุม UI Slider สำหรับปรับระดับเสียง
    /// รองรับ Volume channels ทั้งหมด: Master, Music(BGM), Ambience, UI, SFX, Physics, Block
    /// สามารถวางสคริปต์นี้ไว้บน UI Panel ของหน้า Settings แล้วลาก Slider มาใส่อ้างอิงได้เลย
    /// </summary>
    public class VolumeSettingsUI : MonoBehaviour
    {
        [Header("UI Sliders")]
        [Tooltip("Slider สำหรับปรับระดับเสียงเพลง BGM")]
        [SerializeField] private Slider bgmVolumeSlider;

        [Tooltip("Slider สำหรับปรับระดับเสียงเอฟเฟกต์ SFX")]
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Extended Volume Sliders")]
        [Tooltip("Slider สำหรับปรับ Master Volume")]
        [SerializeField] private Slider masterVolumeSlider;

        [Tooltip("Slider สำหรับปรับ Ambience Volume")]
        [SerializeField] private Slider ambienceVolumeSlider;

        [Tooltip("Slider สำหรับปรับ UI Volume")]
        [SerializeField] private Slider uiVolumeSlider;

        [Tooltip("Slider สำหรับปรับ Physics Volume")]
        [SerializeField] private Slider physicsVolumeSlider;

        [Tooltip("Slider สำหรับปรับ Block Volume")]
        [SerializeField] private Slider blockVolumeSlider;

        private bool _isInternalChange = false;

        private void OnEnable()
        {
            InitializeSliders();
        }

        private void Start()
        {
            InitializeSliders();

            // ลงทะเบียน EventListeners เมื่อผู้เล่นเลื่อน Slider
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (ambienceVolumeSlider != null)
            {
                ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);
            }

            if (uiVolumeSlider != null)
            {
                uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
            }

            if (physicsVolumeSlider != null)
            {
                physicsVolumeSlider.onValueChanged.AddListener(OnPhysicsVolumeChanged);
            }

            if (blockVolumeSlider != null)
            {
                blockVolumeSlider.onValueChanged.AddListener(OnBlockVolumeChanged);
            }

            // รับฟัง Event การรีเซ็ตหรือเปลี่ยนแปลงค่าผ่านระบบตั้งค่า
            Simulation.UI.GameSettings.OnSettingsChanged += OnSettingsChangedUpdate;
        }

        private void OnSettingsChangedUpdate()
        {
            if (_isInternalChange) return;
            InitializeSliders();
        }

        /// <summary>
        /// โหลดค่าเริ่มต้นจาก GameSettings มาใส่ใน Slider
        /// </summary>
        private void InitializeSliders()
        {
            _isInternalChange = true;

            // โหลดค่าจาก GameSettings (ซึ่งอ่านจาก PlayerPrefs)
            float savedBGM = GameSettings.LoadMusicVolume();
            float savedSFX = GameSettings.LoadSFXVolume();
            float savedMaster = GameSettings.LoadMasterVolume();
            float savedAmbience = GameSettings.LoadAmbienceVolume();
            float savedUI = GameSettings.LoadUIVolume();
            float savedPhysics = GameSettings.LoadPhysicsVolume();
            float savedBlock = GameSettings.LoadBlockVolume();

            if (bgmVolumeSlider != null) bgmVolumeSlider.value = savedBGM;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = savedSFX;
            if (masterVolumeSlider != null) masterVolumeSlider.value = savedMaster;
            if (ambienceVolumeSlider != null) ambienceVolumeSlider.value = savedAmbience;
            if (uiVolumeSlider != null) uiVolumeSlider.value = savedUI;
            if (physicsVolumeSlider != null) physicsVolumeSlider.value = savedPhysics;
            if (blockVolumeSlider != null) blockVolumeSlider.value = savedBlock;

            // อัปเดตไปยังตัวจัดการเสียงจริง
            ApplyBGMVolume(savedBGM);
            ApplySFXVolume(savedSFX);

            _isInternalChange = false;
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า BGM Volume Slider
        /// </summary>
        private void OnBGMVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            ApplyBGMVolume(value);
            GameSettings.SaveMusicVolume(value);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า SFX Volume Slider
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            ApplySFXVolume(value);
            GameSettings.SaveSFXVolume(value);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า Master Volume Slider
        /// </summary>
        private void OnMasterVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            GameSettings.SaveMasterVolume(value);
            // Master Volume มีผลต่อทุก channel
            GameSettings.ApplyMusicVolume();
            GameSettings.ApplySFXVolume();
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า Ambience Volume Slider
        /// </summary>
        private void OnAmbienceVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            GameSettings.SaveAmbienceVolume(value);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า UI Volume Slider
        /// </summary>
        private void OnUIVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            GameSettings.SaveUIVolume(value);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า Physics Volume Slider
        /// </summary>
        private void OnPhysicsVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            GameSettings.SavePhysicsVolume(value);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า Block Volume Slider
        /// </summary>
        private void OnBlockVolumeChanged(float value)
        {
            if (_isInternalChange) return;
            GameSettings.SaveBlockVolume(value);
        }

        private void ApplyBGMVolume(float value)
        {
            if (BGMManager.Instance != null)
            {
                float master = GameSettings.LoadMasterVolume();
                BGMManager.Instance.BGMVolume = value * master;
            }
        }

        private void ApplySFXVolume(float value)
        {
            if (ClickSound.Instance != null)
            {
                float master = GameSettings.LoadMasterVolume();
                ClickSound.Instance.SFXVolume = value * master;
            }
        }

        private void OnDestroy()
        {
            // ล้าง Event สำหรับระบบตั้งค่า
            Simulation.UI.GameSettings.OnSettingsChanged -= OnSettingsChangedUpdate;

            // ล้าง EventListeners เมื่อ UI โดนทำลาย
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (ambienceVolumeSlider != null)
                ambienceVolumeSlider.onValueChanged.RemoveListener(OnAmbienceVolumeChanged);

            if (uiVolumeSlider != null)
                uiVolumeSlider.onValueChanged.RemoveListener(OnUIVolumeChanged);

            if (physicsVolumeSlider != null)
                physicsVolumeSlider.onValueChanged.RemoveListener(OnPhysicsVolumeChanged);

            if (blockVolumeSlider != null)
                blockVolumeSlider.onValueChanged.RemoveListener(OnBlockVolumeChanged);
        }
    }
}
