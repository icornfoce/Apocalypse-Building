using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [System.Serializable]
    public class BGMTrack
    {
        [Tooltip("ไฟล์เสียงเพลง BGM")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("ระดับความดังของเพลงนี้ (0 = เงียบ, 1 = ดังสุด)")]
        public float volume = 1f;
    }

    /// <summary>
    /// จัดการเพลงพื้นหลัง (BGM) โดยสุ่มเล่นจาก Playlist
    /// เมื่อเพลงจบจะสุ่มเพลงใหม่ และมีโอกาสได้เพลงเดิมซ้ำได้
    /// </summary>
    public class BGMManager : MonoBehaviour
    {
        public static BGMManager Instance { get; private set; }

        [Header("BGM Playlist")]
        [Tooltip("รายการเพลงทั้งหมดที่จะสุ่มเล่น (ใส่กี่อันก็ได้)")]
        public List<BGMTrack> bgmTracks = new List<BGMTrack>();

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
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound
        }

        private void Start()
        {
            PlayRandomBGM();
        }

        private void Update()
        {
            // เมื่อเพลงจบ ให้สุ่มเพลงใหม่ทันที
            if (!audioSource.isPlaying && bgmTracks.Count > 0)
            {
                PlayRandomBGM();
            }
        }

        /// <summary>
        /// สุ่มเพลงจาก Playlist แล้วเล่น (มีโอกาสได้เพลงเดิมซ้ำได้)
        /// </summary>
        public void PlayRandomBGM()
        {
            if (bgmTracks == null || bgmTracks.Count == 0)
            {
                Debug.LogWarning("[BGMManager] ยังไม่มีเพลงใน Playlist");
                return;
            }

            int index = Random.Range(0, bgmTracks.Count);
            BGMTrack track = bgmTracks[index];

            if (track == null || track.clip == null)
            {
                Debug.LogWarning($"[BGMManager] BGMTrack ที่ Index {index} ไม่มีไฟล์เสียง");
                return;
            }

            audioSource.clip = track.clip;
            audioSource.volume = track.volume;
            audioSource.Play();

            Debug.Log($"[BGMManager] กำลังเล่น: {track.clip.name} (Volume: {track.volume})");
        }

        /// <summary>
        /// หยุดเพลง BGM
        /// </summary>
        public void StopBGM()
        {
            audioSource.Stop();
        }

        /// <summary>
        /// พักเพลง BGM
        /// </summary>
        public void PauseBGM()
        {
            audioSource.Pause();
        }

        /// <summary>
        /// เล่นเพลง BGM ต่อจากที่พัก
        /// </summary>
        public void ResumeBGM()
        {
            audioSource.UnPause();
        }

        /// <summary>
        /// ปรับ Volume รวมของ BGM ทั้งหมด
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}
