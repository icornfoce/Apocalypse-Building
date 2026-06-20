using UnityEngine;
using UnityEngine.UI;

namespace AudioSystem
{
    /// <summary>
    /// VolumeSettingsUI - ตัวควบคุม UI Slider สำหรับปรับระดับเสียง BGM และ SFX
    /// สามารถวางสคริปต์นี้ไว้บน UI Panel ของหน้า Settings แล้วลาก Slider มาใส่อ้างอิงได้เลย
    /// </summary>
    public class VolumeSettingsUI : MonoBehaviour
    {
        [Header("UI Sliders")]
        [Tooltip("Slider สำหรับปรับระดับเสียงเพลง BGM")]
        [SerializeField] private Slider bgmVolumeSlider;

        [Tooltip("Slider สำหรับปรับระดับเสียงเอฟเฟกต์ SFX")]
        [SerializeField] private Slider sfxVolumeSlider;

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
        }

        /// <summary>
        /// โหลดค่าเริ่มต้นจาก PlayerPrefs มาใส่ใน Slider
        /// </summary>
        private void InitializeSliders()
        {
            // โหลดค่าความดังเสียง (Default เป็น 0.5f หรือ 50%)
            float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = savedBGM;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = savedSFX;
            }

            // อัปเดตไปยังตัวจัดการเสียงจริง
            ApplyBGMVolume(savedBGM);
            ApplySFXVolume(savedSFX);
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า BGM Volume Slider
        /// </summary>
        private void OnBGMVolumeChanged(float value)
        {
            ApplyBGMVolume(value);
            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// เรียกเมื่อเปลี่ยนค่า SFX Volume Slider
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            ApplySFXVolume(value);
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        private void ApplyBGMVolume(float value)
        {
            if (BGMManager.Instance != null)
            {
                BGMManager.Instance.BGMVolume = value;
            }
        }

        private void ApplySFXVolume(float value)
        {
            if (ClickSound.Instance != null)
            {
                ClickSound.Instance.SFXVolume = value;
            }
        }

        private void OnDestroy()
        {
            // ล้าง EventListeners เมื่อ UI โดนทำลาย
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            }
        }
    }
}
