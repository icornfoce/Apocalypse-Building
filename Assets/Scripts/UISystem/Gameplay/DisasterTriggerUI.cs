using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Simulation.Mission;

namespace Simulation.UI
{
    /// <summary>
    /// สคริปต์ควบคุม UI สำหรับเลือกและเสกภัยพิบัติ + ซอมบี้ด้วยตัวเอง
    /// แสดงเฉพาะในโหมด sandbox เท่านั้น
    /// </summary>
    public class DisasterTriggerUI : MonoBehaviour
    {
        [Header("Disaster UI")]
        [SerializeField] private TMP_Dropdown disasterDropdown;
        [SerializeField] private Button triggerButton;
        [SerializeField] private Button stopAllButton;

        [Header("Disaster Assets")]
        [Tooltip("ลากไฟล์ DisasterData ที่ต้องการให้เลือกใน Dropdown มาใส่ที่นี่")]
        [SerializeField] private List<DisasterData> disasterList = new List<DisasterData>();

        [Header("Zombie UI")]
        [Tooltip("Dropdown เลือกชนิดซอมบี้ที่จะเสก")]
        [SerializeField] private TMP_Dropdown zombieDropdown;
        [Tooltip("ปุ่มเสกซอมบี้ (กด 1 ครั้ง = เกิด 1 ตัว)")]
        [SerializeField] private Button spawnZombieButton;

        [Header("Zombie Prefabs (ลากจาก Assets/Scripts/AI System/Zombie)")]
        [SerializeField] private GameObject normalZombiePrefab;
        [SerializeField] private GameObject diggerZombiePrefab;
        [SerializeField] private GameObject balloonZombiePrefab;

        [Header("Sandbox Only")]
        [Tooltip("แผงที่จะซ่อนเมื่อไม่ได้อยู่ในซีน sandbox (เว้นว่าง = ใช้ GameObject ของสคริปต์นี้)")]
        [SerializeField] private GameObject panelRoot;

        // ตัวเลือกซอมบี้ที่ใช้งานได้จริง (เฉพาะชนิดที่ใส่ prefab ไว้)
        private struct ZombieOption
        {
            public string label;
            public GameObject prefab;
            public ZombieKind kind;
            public ZombieOption(string l, GameObject p, ZombieKind k) { label = l; prefab = p; kind = k; }
        }
        private readonly List<ZombieOption> _zombieOptions = new List<ZombieOption>();

        // CanvasGroup ของแผง — ใช้ซ่อน/แสดงแบบไม่ปิด GameObject (เพื่อให้ Update ยังทำงานตรวจเฟสได้)
        private CanvasGroup _panelGroup;

        private void Start()
        {
            // ── แสดงเฉพาะโหมด sandbox ──
            bool isSandbox = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("sandbox");
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (!isSandbox)
            {
                root.SetActive(false);
                return;
            }

            // เตรียม CanvasGroup ไว้ซ่อน/แสดงแผง โดยไม่ปิด GameObject (Update จะได้ทำงานตรวจเฟสต่อได้)
            _panelGroup = root.GetComponent<CanvasGroup>();
            if (_panelGroup == null) _panelGroup = root.AddComponent<CanvasGroup>();

            InitializeDisasterDropdown();
            InitializeZombieDropdown();

            if (triggerButton != null) triggerButton.onClick.AddListener(OnTriggerClick);
            if (stopAllButton != null) stopAllButton.onClick.AddListener(OnStopAllClick);
            if (spawnZombieButton != null) spawnZombieButton.onClick.AddListener(OnSpawnZombieClick);

            // ซ่อนแผงไว้ก่อน จะแสดงเฉพาะตอนกดเริ่ม (เริ่มการจำลอง) เท่านั้น
            SetPanelVisible(false);
        }

        private void Update()
        {
            // ── แผง Disaster Trigger จะแสดงเฉพาะตอนกดเริ่ม (เริ่มการจำลอง) เท่านั้น ──
            // ก่อนกดเริ่ม (เฟสสร้าง) แผงจะถูกซ่อนไว้ และจะกลับมาซ่อนเมื่อหยุดจำลอง
            bool isSimulating = Simulation.Physics.SimulationManager.Instance != null
                                && Simulation.Physics.SimulationManager.Instance.IsSimulating;

            SetPanelVisible(isSimulating);
        }

        /// <summary>
        /// ซ่อน/แสดงแผงด้วย CanvasGroup (ไม่ปิด GameObject เพื่อให้ Update ยังทำงาน)
        /// </summary>
        private void SetPanelVisible(bool visible)
        {
            if (_panelGroup == null) return;
            _panelGroup.alpha = visible ? 1f : 0f;
            _panelGroup.interactable = visible;
            _panelGroup.blocksRaycasts = visible;
        }

        // ────────────────────────────────────────────────────────────────
        // Disaster
        // ────────────────────────────────────────────────────────────────

        private void InitializeDisasterDropdown()
        {
            if (disasterDropdown == null) return;

            disasterDropdown.ClearOptions();
            List<string> options = new List<string>();

            if (disasterList == null || disasterList.Count == 0)
            {
                options.Add("No Disasters Loaded");
                disasterDropdown.AddOptions(options);
                if (triggerButton != null) triggerButton.interactable = false;
                return;
            }

            foreach (var disaster in disasterList)
            {
                if (disaster != null) options.Add(disaster.disasterName);
            }

            disasterDropdown.AddOptions(options);
            if (triggerButton != null) triggerButton.interactable = true;
        }

        private void OnTriggerClick()
        {
            if (disasterDropdown == null || MissionManager.Instance == null) return;
            if (disasterList == null || disasterList.Count == 0) return;

            int selectedIndex = disasterDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < disasterList.Count)
            {
                DisasterData selectedDisaster = disasterList[selectedIndex];
                if (selectedDisaster != null)
                {
                    MissionManager.Instance.TriggerDisasterDirectly(selectedDisaster);
                }
            }
        }

        private void OnStopAllClick()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.StopAllDisastersPublic();
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Zombie
        // ────────────────────────────────────────────────────────────────

        private void InitializeZombieDropdown()
        {
            _zombieOptions.Clear();

            // สร้างตัวเลือกเฉพาะชนิดที่ใส่ prefab ไว้
            if (normalZombiePrefab != null) _zombieOptions.Add(new ZombieOption("Zombie ธรรมดา", normalZombiePrefab, ZombieKind.Normal));
            if (diggerZombiePrefab != null) _zombieOptions.Add(new ZombieOption("Digger Zombie", diggerZombiePrefab, ZombieKind.Digger));
            if (balloonZombiePrefab != null) _zombieOptions.Add(new ZombieOption("Balloon Zombie", balloonZombiePrefab, ZombieKind.Balloon));

            if (zombieDropdown != null)
            {
                zombieDropdown.ClearOptions();
                List<string> options = new List<string>();
                foreach (var o in _zombieOptions) options.Add(o.label);
                if (options.Count == 0) options.Add("No Zombie Prefabs");
                zombieDropdown.AddOptions(options);
            }

            if (spawnZombieButton != null) spawnZombieButton.interactable = _zombieOptions.Count > 0;
        }

        private void OnSpawnZombieClick()
        {
            if (MissionManager.Instance == null || _zombieOptions.Count == 0) return;

            int idx = zombieDropdown != null ? zombieDropdown.value : 0;
            if (idx < 0 || idx >= _zombieOptions.Count) return;

            ZombieOption opt = _zombieOptions[idx];
            MissionManager.Instance.SpawnZombieDirectly(opt.prefab, opt.kind);
        }
    }
}
