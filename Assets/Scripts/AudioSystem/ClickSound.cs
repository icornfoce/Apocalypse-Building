using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace AudioSystem
{
    /// <summary>
    /// จัดการเสียง SFX กลางของทั้งเกม
    /// รองรับการเล่นเสียง SFX จากที่ไหนก็ได้ผ่าน Instance
    /// และสามารถติดลงบน GameObject เพื่อให้ปุ่มทุกปุ่มมีเสียงได้
    /// </summary>
    [MovedFrom(true, "AudioSystem", "Assembly-CSharp", "SoundManager")]
    public class ClickSound : MonoBehaviour
    {
        public static ClickSound Instance { get; private set; }

        [Header("Global Button SFX")]
        [Tooltip("เสียงที่เล่นเมื่อกดปุ่มทั่วไปใน UI")]
        public AudioClip buttonClickSound;

        [Range(0f, 1f)]
        [Tooltip("ระดับความดังของ Button Click Sound")]
        public float buttonClickVolume = 1f;

        private AudioSource audioSource;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound
        }

        private void Start()
        {
            AttachSoundToAllButtons();
        }

        /// <summary>
        /// ค้นหาปุ่มทั้งหมดใน Scene แล้วเพิ่มเสียง Click ให้อัตโนมัติ
        /// </summary>
        public void AttachSoundToAllButtons()
        {
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
            foreach (Button btn in allButtons)
            {
                if (btn.gameObject.scene.IsValid())
                {
                    btn.onClick.RemoveListener(PlayButtonClickSound);
                    btn.onClick.AddListener(PlayButtonClickSound);
                }
            }
            Debug.Log($"[ClickSound] ผูกเสียงกับปุ่มทั้งหมด {allButtons.Length} ปุ่ม");
        }

        /// <summary>
        /// เล่นเสียง Button Click (ถูกเรียกจากปุ่มทุกปุ่มอัตโนมัติ)
        /// </summary>
        public void PlayButtonClickSound()
        {
            PlaySFX(buttonClickSound, buttonClickVolume);
        }

        /// <summary>
        /// เล่นเสียง SFX ใดๆ ผ่าน SoundManager.Instance.PlaySFX(clip)
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[ClickSound] ไม่มีไฟล์เสียง SFX ที่จะเล่น");
                return;
            }

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
