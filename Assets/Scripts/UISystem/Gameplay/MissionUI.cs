using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Simulation.Building;
using Simulation.Mission;
using Simulation.Core;

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

        [Header("NPC Info")]
        [Tooltip("Text แสดงจำนวน NPC ทั้งหมดที่วางในฉาก")]
        [SerializeField] private TextMeshProUGUI npcText;

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
        [SerializeField] private Button stopSimButton;
        [SerializeField] private TextMeshProUGUI startButtonText;

        [Header("Results UI (Inside MissionResults Screen)")]
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [Tooltip("รายการดาว แต่ละดวงใส่ Icon และเสียงของตัวเองได้เลย")]
        [SerializeField] private StarEntry[] starEntries;

        [Header("Progression UI")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button levelSelectButton;

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

        private float _lastStatsCheckTime;
        private const float StatsUpdateInterval = 0.2f;

        // โหมด sandbox: แสดง "เงินที่ใช้ไป" (บวก) แทนงบคงเหลือที่อาจติดลบ
        private bool _isSandbox;

        // แคชค่าที่แสดงผล เพื่อไม่ให้ Update() สร้าง string ใหม่ทุกเฟรม (ลด GC)
        private float _lastBudgetValue = float.NaN;
        private int _lastTimerQuant = int.MinValue;

        // ข้อความคงที่ (รวม magic string ไว้ที่เดียว)
        private const string LevelSelectSceneName = "LevelSelect";
        private const string MissionCompleteTitle = "MISSION COMPLETE!";
        private const string MissionFailedTitle = "MISSION FAILED";
        private const string StartLabel = "START";
        private const string StopLabel = "STOP";

        private void Start()
        {
            // ตรวจว่าเป็นโหมด sandbox จากชื่อซีน (sandbox ใช้ MissionData ร่วมกับ Lv.5 จึงแยกด้วยชื่อซีน)
            _isSandbox = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("sandbox");

            // สร้าง AudioSource สำหรับเสียง SFX
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            // เชื่อมต่อ Events จาก MissionManager
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted += HandleMissionStarted;
                MissionManager.Instance.OnMissionStopped += HandleMissionStopped;
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
                MissionManager.Instance.OnValidationFailed += HandleValidationFailed;
            }

            if (startSimButton != null) startSimButton.onClick.AddListener(OnStartButtonClick);
            if (stopSimButton != null) stopSimButton.onClick.AddListener(OnStopButtonClick);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClick);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClick);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnBackToLevelSelectClick);

            if (errorText != null) errorText.gameObject.SetActive(false);

            // เก็บสีเริ่มต้น
            if (floorsStatusText != null) _originalFloorsColor = floorsStatusText.color;
            if (areaStatusText != null) _originalAreaColor = areaStatusText.color;
            if (populationStatusText != null) _originalPopulationColor = populationStatusText.color;

            // ตั้งค่าสถานะเริ่มต้นของปุ่ม
            UpdateButtonStates(false);

            UpdateMissionInfo();
        }

        private void Update()
        {
            UpdateBudgetText();
            UpdateTimerAndRequirements();
        }

        private void OnEnable()
        {
            UpdateMissionInfo();
        }

        private void OnDestroy()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted -= HandleMissionStarted;
                MissionManager.Instance.OnMissionStopped -= HandleMissionStopped;
                MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
                MissionManager.Instance.OnValidationFailed -= HandleValidationFailed;
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Per-frame display (อัปเดตข้อความเฉพาะตอนค่าจริงเปลี่ยน เพื่อเลี่ยง GC)
        // ────────────────────────────────────────────────────────────────

        private void UpdateBudgetText()
        {
            if (budgetText == null || BuildingSystem.Instance == null) return;

            // sandbox: แสดง "เงินที่ใช้ไป" เป็นค่าบวก (ไม่ติดลบ), ปกติ: แสดงงบคงเหลือ
            float value = _isSandbox
                ? Mathf.Max(0f, BuildingSystem.Instance.AmountSpent)
                : BuildingSystem.Instance.CurrentBudget;

            if (value == _lastBudgetValue) return; // ค่าไม่เปลี่ยน ไม่ต้องสร้าง string ใหม่

            _lastBudgetValue = value;
            budgetText.text = $"${value:F0}";
            budgetText.color = (_isSandbox || value >= 0) ? Color.white : Color.red;
        }

        private void UpdateTimerAndRequirements()
        {
            if (MissionManager.Instance == null) return;

            if (MissionManager.Instance.IsMissionActive)
            {
                if (timerText == null) return;

                // อัปเดตเวลาเฉพาะตอนค่าที่แสดง (ทศนิยม 1 ตำแหน่ง) เปลี่ยนจริง
                float remaining = MissionManager.Instance.SimulationTimeRemaining;
                int quant = Mathf.RoundToInt(remaining * 10f);
                if (quant != _lastTimerQuant)
                {
                    _lastTimerQuant = quant;
                    timerText.text = $"Time: {remaining:F1}s";
                }
            }
            else
            {
                _lastTimerQuant = int.MinValue; // รีเซ็ตให้รอบจำลองถัดไปอัปเดตเวลาแน่นอน

                // อัปเดตสถานะเงื่อนไข (แบบ Realtime ก่อนเริ่ม) โดยเช็คเป็นระยะแทนทุกเฟรม
                if (Time.time - _lastStatsCheckTime >= StatsUpdateInterval)
                {
                    _lastStatsCheckTime = Time.time;
                    UpdateRequirementStatus();
                }
            }
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

            // ชั้น และ พื้นที่ ใช้รูปแบบเดียวกัน (x/y, ซ่อนถ้าไม่บังคับ)
            UpdateRequirementLine(floorsStatusText, stats.floors, mission.requiredFloors, "Floors", _originalFloorsColor, "");
            UpdateRequirementLine(areaStatusText, stats.area, mission.requiredAreaPerFloor, "Area", _originalAreaColor, " m²");

            // คน: ไม่ซ่อนแม้ไม่บังคับ — บังคับ → "Need N person/people", ไม่บังคับ → จำนวนคนปัจจุบัน
            if (populationStatusText != null)
            {
                if (mission.requiredPopulation > 0)
                {
                    bool ok = stats.people >= mission.requiredPopulation;
                    string personWord = mission.requiredPopulation == 1 ? "person" : "people";
                    populationStatusText.text = $"Need {mission.requiredPopulation} {personWord}";
                    populationStatusText.color = ok ? completedColor : _originalPopulationColor;
                }
                else
                {
                    populationStatusText.text = $"People: {stats.people}";
                    populationStatusText.color = _originalPopulationColor;
                }
                if (!populationStatusText.gameObject.activeSelf) populationStatusText.gameObject.SetActive(true);
            }

            // อัปเดตจำนวน NPC ทั้งหมด
            if (npcText != null)
            {
                npcText.text = $"NPC: {stats.people}";
            }
        }

        /// <summary>
        /// อัปเดตข้อความเงื่อนไขแบบ "x/y" (เช่น ชั้น, พื้นที่): ซ่อนถ้าไม่บังคับ (required == 0);
        /// ถ้าบังคับให้แสดงพร้อมสี — ครบเงื่อนไขเป็นสีเขียว ยังไม่ครบใช้สีเดิม
        /// </summary>
        private void UpdateRequirementLine(TextMeshProUGUI text, int current, int required, string label, Color originalColor, string suffix)
        {
            if (text == null) return;

            if (required > 0)
            {
                bool ok = current >= required;
                text.text = $"{label}: {current}/{required}{suffix}";
                text.color = ok ? completedColor : originalColor;
                if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);
            }
            else if (text.gameObject.activeSelf)
            {
                text.gameObject.SetActive(false);
            }
        }

        private void OnStartButtonClick()
        {
            if (MissionManager.Instance == null) return;

            if (MissionManager.Instance.IsMissionActive)
            {
                // ถ้าสลับปุ่มเป็นปุ่มเดียวแล้วยังค้างอยู่ ให้ Stop
                MissionManager.Instance.EndMission(false);
            }
            else
            {
                // เริ่ม Mission (ซึ่งจะเรียก StartSimulation ให้อัตโนมัติ)
                MissionManager.Instance.StartMission();
            }
        }

        private void OnStopButtonClick()
        {
            if (MissionManager.Instance == null) return;
            MissionManager.Instance.EndMission(false);
        }

        private void HandleMissionStarted()
        {
            UpdateButtonStates(true);

            SetScreen(missionPanel, false);
            SetScreen(gameplayHUD, true);
            SetScreen(resultsPanel, false);

            if (errorText != null) errorText.gameObject.SetActive(false);

            // เล่นเสียงเมื่อเริ่มภารกิจสำเร็จ (ผ่านเงื่อนไขแล้ว)
            if (startSuccessSound != null && _sfxSource != null)
            {
                float vol = sfxVolume * Simulation.UI.GameSettings.LoadUIVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                _sfxSource.PlayOneShot(startSuccessSound, vol);
            }
        }

        private void HandleMissionStopped()
        {
            UpdateButtonStates(false);

            SetScreen(resultsPanel, false);
            SetScreen(gameplayHUD, true);
            SetScreen(missionPanel, true);
        }

        private void HandleMissionCompleted(int stars)
        {
            UpdateButtonStates(false);

            SetScreen(resultsPanel, true);
            SetScreen(gameplayHUD, false);
            SetScreen(missionPanel, false);

            if (resultTitleText != null)
                resultTitleText.text = stars >= 1 ? MissionCompleteTitle : MissionFailedTitle;

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
                {
                    float vol = entry.volume * Simulation.UI.GameSettings.LoadUIVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                    _sfxSource.PlayOneShot(entry.sound, vol);
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

            // เล่นเสียงเมื่อไม่ผ่านเงื่อนไข
            if (startFailSound != null && _sfxSource != null)
            {
                float vol = sfxVolume * Simulation.UI.GameSettings.LoadUIVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                _sfxSource.PlayOneShot(startFailSound, vol);
            }
        }

        private void HideError()
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
        }

        /// <summary>
        /// เรียกใช้งานโดยปุ่ม Restart บน Results Panel หรือปุ่มอื่นๆ
        /// ทำหน้าที่แค่ปิดหน้าต่างสรุปผล เพื่อให้ผู้เล่นแก้สิ่งก่อสร้างต่อได้
        /// </summary>
        public void OnRestartClick()
        {
            SetScreen(resultsPanel, false);
            SetScreen(gameplayHUD, true);
            SetScreen(missionPanel, true);

            UpdateButtonStates(false);

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
                SetScreen(resultsPanel, false);
                SetScreen(missionPanel, true);

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
        /// กลับไปยังหน้าเลือกด่าน (ย้ายซีน)
        /// </summary>
        public void OnBackToLevelSelectClick()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.LoadScene(LevelSelectSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(LevelSelectSceneName);
            }
        }

        /// <summary>
        /// เปิด/ปิดหน้าจอ UI — ใช้ UIManager ถ้ามี (เพื่อให้จัดการ transition กลาง),
        /// ถ้าไม่มีค่อย fallback ไป SetActive ตรง ๆ
        /// ใช้ ShowOverlay แทน OpenScreen เพื่อไม่ให้ปิด panel อื่นพร้อมกัน
        /// </summary>
        private void SetScreen(GameObject panel, bool open)
        {
            if (panel == null) return;

            if (UIManager.Instance != null)
            {
                if (open) UIManager.Instance.ShowOverlay(panel);
                else UIManager.Instance.CloseScreen(panel);
            }
            else
            {
                panel.SetActive(open);
            }
        }

        /// <summary>
        /// อัปเดตสถานะการแสดงผลและข้อความของปุ่มตามสถานะการจำลอง (Start / Stop)
        /// </summary>
        private void UpdateButtonStates(bool isSimulating)
        {
            if (startSimButton != null && stopSimButton != null)
            {
                startSimButton.gameObject.SetActive(!isSimulating);
                stopSimButton.gameObject.SetActive(isSimulating);
            }
            else if (startSimButton != null)
            {
                startSimButton.gameObject.SetActive(true);
                if (startButtonText != null)
                {
                    startButtonText.text = isSimulating ? StopLabel : StartLabel;
                }
            }
        }
    }
}
