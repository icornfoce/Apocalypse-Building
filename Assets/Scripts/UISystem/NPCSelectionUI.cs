using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Simulation.Data;

namespace Simulation.NPC
{
    [System.Serializable]
    public class NPCSlot
    {
        [Tooltip("ปุ่มของช่องนี้")]
        public Button button;
        [Tooltip("ข้อมูล NPC ที่จะใส่ในช่องนี้")]
        public NPCSkillData npcData;
        [Tooltip("Text แสดงชื่อ NPC (ถ้ามี)")]
        public Text nameText;
        [Tooltip("Text แสดงรายละเอียดสกิล (ถ้ามี)")]
        public Text descText;
        [Tooltip("Image แสดงไอคอน NPC (ถ้ามี)")]
        public Image iconImage;
    }

    /// <summary>
    /// UI Panel ควบคุมทั้งการเลือก NPC (Selection) และการแสดงข้อมูล/ใช้สกิล (NPCSkillPanel)
    /// ออกแบบมาให้จัดวางและออกแบบเองใน Unity Editor ได้อย่างสมบูรณ์แบบ
    /// </summary>
    public class NPCSelectionUI : MonoBehaviour
    {
        public static NPCSelectionUI Instance { get; private set; }

        [Header("Selection Panel (ตอนสร้าง/วาง PersonTarget)")]
        [Tooltip("ตัว Panel การเลือก NPC ที่ออกแบบเองใน Canvas")]
        [SerializeField] private GameObject selectionPanel;
        
        [Tooltip("ช่องใส่ NPC แต่ละตัวที่สร้างเองใน Inspector")]
        [SerializeField] private List<NPCSlot> npcSlots = new List<NPCSlot>();

        [Tooltip("ข้อมูลต้นแบบของโครงสร้างคน (PersonTarget) สำหรับใช้ตอนเปิดปุ่มตรงๆ")]
        [SerializeField] private StructureData basePersonData;

        [Header("Skill Panel (ตอนจำลอง/คลิก NPC)")]
        [Tooltip("ตัว Panel แสดงสกิลและสถานะ NPC ในช่วงจำลอง")]
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private Text skillNPCNameText;
        [SerializeField] private Text skillNPCHealthText;
        [SerializeField] private Text skillNPCSkillText;
        [SerializeField] private Text skillNPCStatusText;
        [SerializeField] private Button skillNPCUseButton;
        [SerializeField] private Image skillNPCHealthBar;

        // ── Runtime State ──
        private System.Action<NPCSkillData> _onSelected;
        private NPCController _currentNPC;
        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // ซ่อน Panel เริ่มต้น
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (skillPanel != null) skillPanel.SetActive(false);

            // ลงทะเบียน Events สำหรับโหมดจำลอง (Skill Panel)
            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected += ShowSkillPanel;
                NPCSkillManager.Instance.OnNPCDeselected += HideSkillPanel;
            }
        }

        private void OnDestroy()
        {
            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected -= ShowSkillPanel;
                NPCSkillManager.Instance.OnNPCDeselected -= HideSkillPanel;
            }
        }

        private void Update()
        {
            // อัปเดตข้อมูล NPC ใน Skill Panel แบบ Realtime
            if (_currentNPC != null && skillPanel != null && skillPanel.activeSelf)
            {
                if (skillNPCHealthText != null)
                {
                    skillNPCHealthText.text = $"HP: {_currentNPC.CurrentHealth:F0}/{_currentNPC.Data.maxHealth:F0}";
                }

                if (skillNPCHealthBar != null)
                {
                    skillNPCHealthBar.fillAmount = _currentNPC.HealthRatio;
                    skillNPCHealthBar.color = Color.Lerp(Color.red, Color.green, _currentNPC.HealthRatio);
                }

                if (skillNPCStatusText != null)
                {
                    skillNPCStatusText.text = _currentNPC.SkillActive ? "สกิลทำงานอยู่..." : "พร้อมใช้งาน";
                    skillNPCStatusText.color = _currentNPC.SkillActive ? Color.yellow : Color.green;
                }
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Selection Panel Logic (ตอนสร้าง)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// เปิด Panel เลือก NPC — เรียกจาก BuildUIController ตอนกดวาง PersonTarget
        /// </summary>
        public void Open(System.Action<NPCSkillData> onSelected)
        {
            _onSelected = onSelected;
            _isOpen = true;

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            SetupSlots();
        }

        /// <summary>
        /// เปิดหน้าต่างเลือก NPC โดยตรง (ไม่มี callback สำหรับเรียกใช้ผ่านปุ่ม UI ตรงๆ)
        /// </summary>
        public void OpenSelectionPanel()
        {
            _onSelected = null;
            _isOpen = true;

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            SetupSlots();
        }

        /// <summary>
        /// ปิด Panel เลือก NPC
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }

        private void SetupSlots()
        {
            foreach (var slot in npcSlots)
            {
                if (slot == null || slot.button == null || slot.npcData == null) continue;

                var data = slot.npcData;
                bool isPlaced = NPCSkillManager.Instance != null && NPCSkillManager.Instance.IsNPCPlaced(data.skillType);

                // ตั้งค่าหน้าตาของช่อง (ถ้ากำหนด Component ไว้)
                if (slot.nameText != null) slot.nameText.text = data.npcName;
                if (slot.descText != null) slot.descText.text = isPlaced ? "(วางแล้ว)" : data.description;
                if (slot.iconImage != null) slot.iconImage.sprite = data.icon;

                // เปลี่ยนความโปร่งใสหรือสีของปุ่มตามสถานะการวาง
                var image = slot.button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isPlaced ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : Color.white;
                }

                slot.button.interactable = !isPlaced;

                // ผูก Event ปุ่มกด
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() =>
                {
                    if (_onSelected != null)
                    {
                        _onSelected.Invoke(data);
                    }
                    else if (basePersonData != null && Simulation.Building.BuildingSystem.Instance != null)
                    {
                        // พฤติกรรมเริ่มต้น: สร้าง proxy StructureData และเลือกใน BuildingSystem
                        StructureData proxy = ScriptableObject.CreateInstance<StructureData>();
                        proxy.structureName = data.npcName;
                        proxy.basePrice = data.placementPrice;
                        proxy.baseMass = basePersonData.baseMass;
                        proxy.baseHP = basePersonData.baseHP;
                        proxy.size = basePersonData.size;
                        proxy.prefab = basePersonData.prefab;
                        proxy.defaultMaterial = basePersonData.defaultMaterial;
                        proxy.allowOverlap = basePersonData.allowOverlap;
                        proxy.structureType = basePersonData.structureType;
                        proxy.placeOnStructureOnly = basePersonData.placeOnStructureOnly;
                        
                        Simulation.Building.BuildingSystem.Instance.SelectStructure(proxy);
                    }
                    else
                    {
                        Debug.LogWarning("[NPCSelectionUI] No callback or basePersonData assigned for selection!");
                    }
                    Close();
                });
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Skill Panel Logic (ตอนจำลอง)
        // ────────────────────────────────────────────────────────────────

        public void ShowSkillPanel(NPCController npc)
        {
            _currentNPC = npc;
            if (skillPanel != null)
            {
                skillPanel.SetActive(true);
            }

            if (npc == null || npc.Data == null) return;

            // ตั้งข้อมูลเริ่มต้นในแผงควบคุมสกิล
            if (skillNPCNameText != null)
            {
                skillNPCNameText.text = npc.Data.npcName;
                skillNPCNameText.color = GetSkillColor(npc.Data.skillType);
            }

            if (skillNPCSkillText != null)
            {
                skillNPCSkillText.text = $"สกิล: {GetSkillName(npc.Data.skillType)}";
            }

            bool isPassive = npc.Data.skillType == NPCSkillType.Economist
                          || npc.Data.skillType == NPCSkillType.Architect
                          || npc.Data.skillType == NPCSkillType.Politician;

            if (skillNPCUseButton != null)
            {
                skillNPCUseButton.interactable = !isPassive;
                
                var btnTextComp = skillNPCUseButton.GetComponentInChildren<Text>();
                if (btnTextComp != null)
                {
                    btnTextComp.text = isPassive ? "สกิลอัตโนมัติ (Passive)" : "ใช้สกิล";
                }

                skillNPCUseButton.onClick.RemoveAllListeners();
                if (!isPassive)
                {
                    skillNPCUseButton.onClick.AddListener(() =>
                    {
                        if (NPCSkillManager.Instance != null)
                        {
                            NPCSkillManager.Instance.ActivateSelectedSkill();
                        }
                    });
                }
            }
        }

        public void HideSkillPanel()
        {
            _currentNPC = null;
            if (skillPanel != null)
            {
                skillPanel.SetActive(false);
            }
        }

        private Color GetSkillColor(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer:  return new Color(0.3f, 0.8f, 0.3f);
                case NPCSkillType.Builder:   return new Color(0.9f, 0.6f, 0.2f);
                case NPCSkillType.Economist: return new Color(0.3f, 0.5f, 0.9f);
                case NPCSkillType.Architect: return new Color(0.7f, 0.3f, 0.9f);
                case NPCSkillType.Politician:return new Color(0.2f, 0.2f, 0.8f);
                case NPCSkillType.Commander: return new Color(0.9f, 0.2f, 0.2f);
                default: return Color.white;
            }
        }

        private string GetSkillName(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer:  return "แสดงแรงเค้น";
                case NPCSkillType.Builder:   return "ซ่อมโครงสร้าง";
                case NPCSkillType.Economist: return "ลดราคา 10% (Auto)";
                case NPCSkillType.Architect: return "สะท้อนดาเมจ 20%";
                case NPCSkillType.Politician:return "ยกเลิกภาษี (Auto)";
                case NPCSkillType.Commander: return "เรียกทหารยิง";
                default: return "ไม่ทราบ";
            }
        }
    }
}
