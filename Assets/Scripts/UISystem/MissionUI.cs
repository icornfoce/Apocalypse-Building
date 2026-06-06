using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Simulation.Building;
using Simulation.Mission;
using Simulation.Physics;

namespace Simulation.UI
{
    [System.Serializable]
    public class StarEntry
    {
        [Tooltip("รูป Texture ของดาวดวงนี้ (ลาก Texture มาใส่ได้เลย)")]
        public Texture starTexture;
        [Tooltip("RawImage Component ที่จะใช้แสดง Texture นี้")]
        public RawImage displayImage;
        [Tooltip("เสียงที่เล่นตอนดาวดวงนี้ขึ้น")]
        public AudioClip sound;
        [Range(0f, 1f)]
        [Tooltip("ความดังของเสียงดาวดวงนี้")]
        public float volume = 1f;
    }

    /// <summary>
    /// UI สำหรับแสดงข้อมูลด่าน, งบประมาณ และผลการประเมิน
    /// </summary>
    public class MissionUI : MonoBehaviour
    {
        [Header("Budget UI")]
        [SerializeField] private TextMeshProUGUI budgetText;

        [Header("Panels")]
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject gameplayHUD;

        [Header("Mission Info UI (Inside MissionInfo Screen)")]
        [SerializeField] private TextMeshProUGUI missionNameText;
        [SerializeField] private TextMeshProUGUI missionDescText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Requirements UI")]
        [SerializeField] private TextMeshProUGUI floorsStatusText;
        [SerializeField] private TextMeshProUGUI areaStatusText;
        [SerializeField] private TextMeshProUGUI populationStatusText;

        [Header("Simulation Controls")]
        [SerializeField] private Button startSimButton;
        [SerializeField] private TextMeshProUGUI startButtonText;

        [Header("Results UI (Inside MissionResults Screen)")]
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [Tooltip("รายการดาว แต่ละดวงใส่ Icon และเสียงของตัวเองได้เลย")]
        [SerializeField] private StarEntry[] starEntries;

        [Header("Progression UI")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private string levelSelectScreenName = "LevelSelect";

        [Header("Error Messages")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private float errorDisplayDuration = 3f;

        [Header("Sound Effects")]
        [Tooltip("เสียงที่เล่นเมื่อกดเริ่มแล้วผ่านเงื่อนไข (เกมเริ่ม)")]
        [SerializeField] private AudioClip startSuccessSound;
        [Tooltip("เสียงที่เล่นเมื่อกดเริ่มแล้วไม่ผ่านเงื่อนไข")]
        [SerializeField] private AudioClip startFailSound;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Header("Star Reveal Settings")]
        [Tooltip("ระยะเวลาหน่วง (วินาที) ระหว่างการขึ้นดาวแต่ละดวง")]
        [SerializeField] private float starRevealDelay = 0.5f;

        private AudioSource _sfxSource;

        [Header("Style")]
        [SerializeField] private Color completedColor = new Color(0f, 0.6f, 0f); // เขียวเข้ม

        private Color _originalFloorsColor;
        private Color _originalAreaColor;
        private Color _originalPopulationColor;

        private void Start()
        {
            // สร้าง AudioSource สำหรับเสียง SFX
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            // เชื่อมต่อ Events จาก MissionManager
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted += HandleMissionStarted;
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
                MissionManager.Instance.OnValidationFailed += HandleValidationFailed;
            }

            if (startSimButton != null)
            {
                startSimButton.onClick.AddListener(OnStartButtonClick);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClick);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevelClick);
            }

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.AddListener(OnBackToLevelSelectClick);
            }

            if (errorText != null) errorText.gameObject.SetActive(false);
            
            // เก็บสีเริ่มต้น
            if (floorsStatusText != null) _originalFloorsColor = floorsStatusText.color;
            if (areaStatusText != null) _originalAreaColor = areaStatusText.color;
            if (populationStatusText != null) _originalPopulationColor = populationStatusText.color;

            UpdateMissionInfo();
        }

        private void Update()
        {
            // อัปเดตเงินทุกเฟรม
            if (budgetText != null && BuildingSystem.Instance != null)
            {
                budgetText.text = $"${BuildingSystem.Instance.CurrentBudget:F0}";
                budgetText.color = BuildingSystem.Instance.CurrentBudget >= 0 ? Color.white : Color.red;
            }

            // อัปเดตเวลาและสถานะด่าน
            if (MissionManager.Instance != null && MissionManager.Instance.IsMissionActive)
            {
                if (timerText != null)
                {
                    timerText.text = $"Time: {MissionManager.Instance.SimulationTimeRemaining:F1}s";
                }
            }

            // อัปเดตสถานะเงื่อนไข (แบบ Realtime ก่อนเริ่ม)
            if (MissionManager.Instance != null && !MissionManager.Instance.IsMissionActive)
            {
                UpdateRequirementStatus();
            }
        }

        private void OnEnable()
        {
            UpdateMissionInfo();
        }

        public void UpdateMissionInfo()
        {
            if (MissionManager.Instance == null || MissionManager.Instance.CurrentMission == null) return;

            var mission = MissionManager.Instance.CurrentMission;
            if (missionNameText != null) missionNameText.text = mission.missionName;
            if (missionDescText != null) missionDescText.text = mission.description;
            
            // รีเซ็ตสถานะข้อความเงื่อนไขให้เป็นปัจจุบันทันที
            UpdateRequirementStatus();
        }

        private void UpdateRequirementStatus()
        {
            if (MissionManager.Instance == null || MissionManager.Instance.CurrentMission == null)
            {
                if (floorsStatusText != null) floorsStatusText.text = "";
                if (areaStatusText != null) areaStatusText.text = "";
                if (populationStatusText != null) populationStatusText.text = "";
                return;
            }

            var mission = MissionManager.Instance.CurrentMission;
            var stats = MissionManager.Instance.GetCurrentStats();

            // แสดงสถานะ ชั้น (ซ่อนถ้า 0 = ไม่บังคับ)
            if (floorsStatusText != null)
            {
                if (mission.requiredFloors > 0)
                {
                    bool ok = stats.floors >= mission.requiredFloors;
                    floorsStatusText.text = $"Floors: {stats.floors}/{mission.requiredFloors}";
                    floorsStatusText.color = ok ? completedColor : _originalFloorsColor;
                    floorsStatusText.gameObject.SetActive(true);
                }
                else
                {
                    floorsStatusText.gameObject.SetActive(false);
                }
            }

            // แสดงสถานะ พื้นที่
            if (areaStatusText != null)
            {
                if (mission.requiredAreaPerFloor > 0)
                {
                    bool ok = stats.area >= mission.requiredAreaPerFloor;
                    areaStatusText.text = $"Area: {stats.area}/{mission.requiredAreaPerFloor} m²";
                    areaStatusText.color = ok ? completedColor : _originalAreaColor;
                    areaStatusText.gameObject.SetActive(true);
                }
                else
                {
                    areaStatusText.gameObject.SetActive(false);
                }
            }

            // แสดงสถานะ คน
            if (populationStatusText != null)
            {
                if (mission.requiredPopulation > 0)
                {
                    bool ok = stats.people >= mission.requiredPopulation;
                    populationStatusText.text = $"People: {stats.people}/{mission.requiredPopulation}";
                    populationStatusText.color = ok ? completedColor : _originalPopulationColor;
                    populationStatusText.gameObject.SetActive(true);
                }
                else
                {
                    populationStatusText.gameObject.SetActive(false);
                }
            }
        }

        private void OnStartButtonClick()
        {
            if (MissionManager.Instance == null) return;

            if (MissionManager.Instance.IsMissionActive)
            {
                // ถ้ากำลังเล่นอยู่ ปุ่มนี้อาจทำหน้าที่ Stop
                MissionManager.Instance.EndMission(false);
                if (startButtonText != null) startButtonText.text = "START";
            }
            else
            {
                // เริ่ม Mission (ซึ่งจะเรียก StartSimulation ให้อัตโนมัติ)
                MissionManager.Instance.StartMission();
            }
        }

        private void HandleMissionStarted()
        {
            if (startButtonText != null) startButtonText.text = "STOP";
            
            if (missionPanel != null) missionPanel.SetActive(false);
            if (gameplayHUD != null) gameplayHUD.SetActive(true);
            if (resultsPanel != null) resultsPanel.SetActive(false);

            if (errorText != null) errorText.gameObject.SetActive(false);

            // เล่นเสียงเมื่อเริ่มภารกิจสำเร็จ (ผ่านเงื่อนไขแล้ว)
            if (startSuccessSound != null && _sfxSource != null)
                _sfxSource.PlayOneShot(startSuccessSound, sfxVolume);
        }

        private void HandleMissionCompleted(int stars)
        {
            if (startButtonText != null) startButtonText.text = "RESTART";
            
            if (resultsPanel != null) resultsPanel.SetActive(true);
            if (gameplayHUD != null) gameplayHUD.SetActive(false);
            if (missionPanel != null) missionPanel.SetActive(false);

            if (resultTitleText != null) resultTitleText.text = "MISSION COMPLETE!";

            // ซ่อนดาวทั้งหมดก่อน แล้วค่อยๆ ขึ้นทีละดวงพร้อมเสียง
            if (starEntries != null)
            {
                foreach (var entry in starEntries)
                {
                    if (entry?.displayImage != null)
                        entry.displayImage.gameObject.SetActive(false);
                }

                StartCoroutine(RevealStarsSequentially(stars));
            }
        }

        /// <summary>
        /// ค่อยๆ แสดงดาวทีละดวงพร้อมเสียงเฉพาะของดาวดวงนั้น
        /// </summary>
        private IEnumerator RevealStarsSequentially(int stars)
        {
            for (int i = 0; i < starEntries.Length; i++)
            {
                if (i >= stars) break; // แสดงแค่จำนวนดาวที่ได้

                // แสดงผลดาวดวงแรกทันที ดวงถัดๆ ไปค่อยหน่วงเวลา
                if (i > 0)
                {
                    yield return new WaitForSeconds(starRevealDelay);
                }

                var entry = starEntries[i];
                if (entry == null) continue;

                // แสดงดาว พร้อมเซ็ต Texture ลงบน RawImage
                if (entry.displayImage != null)
                {
                    if (entry.starTexture != null)
                        entry.displayImage.texture = entry.starTexture;
                    entry.displayImage.gameObject.SetActive(true);
                }

                // เล่นเสียงเฉพาะของดาวดวงนี้
                if (entry.sound != null && _sfxSource != null)
                    _sfxSource.PlayOneShot(entry.sound, entry.volume);
            }
        }

        private void HandleValidationFailed(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideError));
                Invoke(nameof(HideError), errorDisplayDuration);
            }

            // เล่นเสียงเมื่อไม่ผ่านเงื่อนไข
            if (startFailSound != null && _sfxSource != null)
                _sfxSource.PlayOneShot(startFailSound, sfxVolume);
        }

        private void HideError()
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
        }

        /// <summary>
        /// เรียกใช้งานโดยปุ่ม Restart บน Results Panel หรือปุ่มอื่นๆ
        /// ทำหน้าที่แค่ปิดหน้าต่างสรุปผล เพื่อให้ผู้เล่นแก้สิ่งก่อสร้างต่อได้
        /// </summary>
        public void OnRestartClick()
        {
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (gameplayHUD != null) gameplayHUD.SetActive(true);
            if (missionPanel != null) missionPanel.SetActive(true);
            
            if (startButtonText != null) startButtonText.text = "START";

            // รีเซ็ต PersonTarget ทั้งหมดให้กลับมาแสดงผล (Marker) ผ่าน MissionManager
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ResetAllPersonTargets();
            }
        }

        /// <summary>
        /// ไปยังด่านถัดไป (ถ้ามี)
        /// </summary>
        public void OnNextLevelClick()
        {
            if (MissionManager.Instance == null || MissionManager.Instance.CurrentMission == null) return;

            var nextMission = MissionManager.Instance.CurrentMission.nextMission;
            if (nextMission != null)
            {
                MissionManager.Instance.SetMission(nextMission);
                
                // ปิดหน้าสรุปผล และเปิดหน้าข้อมูลด่านใหม่
                if (resultsPanel != null) resultsPanel.SetActive(false);
                if (missionPanel != null) missionPanel.SetActive(true);
                
                UpdateMissionInfo();
                Debug.Log($"[MissionUI] Transition to next level: {nextMission.missionName}");
            }
            else
            {
                Debug.LogWarning("[MissionUI] No next mission assigned in MissionData.");
                OnBackToLevelSelectClick(); // ถ้าไม่มีด่านถัดไป ให้กลับไปหน้าเลือกด่าน
            }
        }

        /// <summary>
        /// กลับไปยังหน้าเลือกด่าน (ScreenManager)
        /// </summary>
        public void OnBackToLevelSelectClick()
        {
            // ปิด UI ทุกอย่างของ Mission นี้
            if (missionPanel != null) missionPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (gameplayHUD != null) gameplayHUD.SetActive(false);

            // เปิดหน้าจอเลือกด่านผ่าน ScreenManager
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.OpenScreen(levelSelectScreenName);
            }
            else
            {
                Debug.LogError("[MissionUI] ScreenManager Instance not found for LevelSelect!");
            }
        }
    }
}
