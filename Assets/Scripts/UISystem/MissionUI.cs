using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Simulation.Building;
using Simulation.Mission;
using Simulation.Physics;

namespace Simulation.UI
{
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
        [SerializeField] private GameObject[] starIcons;

        [Header("Progression UI")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private string levelSelectScreenName = "LevelSelect";

        [Header("Error Messages")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private float errorDisplayDuration = 3f;

        [Header("Style")]
        [SerializeField] private Color completedColor = new Color(0f, 0.6f, 0f); // เขียวเข้ม

        private Color _originalFloorsColor;
        private Color _originalAreaColor;
        private Color _originalPopulationColor;

        private void Start()
        {
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
        }

        private void HandleMissionCompleted(int stars)
        {
            if (startButtonText != null) startButtonText.text = "RESTART";
            
            if (resultsPanel != null) resultsPanel.SetActive(true);
            if (gameplayHUD != null) gameplayHUD.SetActive(false);
            if (missionPanel != null) missionPanel.SetActive(false);

            if (resultTitleText != null) resultTitleText.text = "MISSION COMPLETE!";
            
            // แสดงดาวตามจำนวนที่ได้
            if (starIcons != null)
            {
                for (int i = 0; i < starIcons.Length; i++)
                {
                    if (starIcons[i] != null) starIcons[i].SetActive(i < stars);
                }
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
            
            if (startButtonText != null) startButtonText.text = "START";
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
