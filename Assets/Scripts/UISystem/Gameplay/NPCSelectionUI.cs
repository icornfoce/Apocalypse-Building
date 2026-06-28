using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Simulation.Data;
using TMPro;

namespace Simulation.NPC
{
    [System.Serializable]
    public class NPCSlot
    {
        public Button button;
        public NPCSkillData npcData;
        public Text nameText;
        public Text descText;
        public Image iconImage;
    }

    public class NPCSelectionUI : MonoBehaviour
    {
        public static NPCSelectionUI Instance { get; private set; }

        [Header("Selection Panel")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private bool hideSelectionPanelOnStart = false;
        [SerializeField] private List<NPCSlot> npcSlots = new List<NPCSlot>();
        [SerializeField] private StructureData basePersonData;

        [Header("Skill Panel")]
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private TextMeshProUGUI skillNPCNameText;
        [SerializeField] private TextMeshProUGUI skillNPCHealthText;
        [SerializeField] private TextMeshProUGUI skillNPCSkillText;
        [SerializeField] private TextMeshProUGUI skillNPCStatusText;
        [SerializeField] private Button skillNPCUseButton;
        [SerializeField] private RawImage skillNPCHealthBar;



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
            if (selectionPanel != null && hideSelectionPanelOnStart) selectionPanel.SetActive(false);
            if (skillPanel != null) skillPanel.SetActive(false);

            SetupSlots();

            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected += ShowSkillPanel;
                NPCSkillManager.Instance.OnNPCDeselected += HideSkillPanel;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected -= ShowSkillPanel;
                NPCSkillManager.Instance.OnNPCDeselected -= HideSkillPanel;
            }
        }

        private void Update()
        {
            if (_currentNPC == null || skillPanel == null || !skillPanel.activeSelf) return;

            if (skillNPCHealthText != null)
            {
                skillNPCHealthText.text = $"HP: {_currentNPC.CurrentHealth:F0}/{_currentNPC.Data.maxHealth:F0}";
            }

            if (skillNPCHealthBar != null)
            {
                Vector3 scale = skillNPCHealthBar.rectTransform.localScale;
                scale.x = _currentNPC.HealthRatio;
                skillNPCHealthBar.rectTransform.localScale = scale;
                skillNPCHealthBar.color = Color.Lerp(Color.red, Color.green, _currentNPC.HealthRatio);
            }

            if (skillNPCStatusText != null)
            {
                skillNPCStatusText.text = _currentNPC.SkillActive ? "Skill Active..." : "Ready";
                skillNPCStatusText.color = _currentNPC.SkillActive ? Color.yellow : Color.green;
            }
        }

        public void Open(System.Action<NPCSkillData> onSelected)
        {
            _onSelected = onSelected;
            _isOpen = true;

            if (selectionPanel != null) selectionPanel.SetActive(true);
            SetupSlots();
        }

        public void Close()
        {
            _isOpen = false;
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        private void SetupSlots()
        {
            foreach (var slot in npcSlots)
            {
                if (slot == null || slot.button == null || slot.npcData == null) continue;

                var data = slot.npcData;
                bool isPlaced = NPCSkillManager.Instance != null && NPCSkillManager.Instance.IsNPCPlaced(data.skillType);

                if (slot.nameText != null) slot.nameText.text = data.npcName;
                if (slot.descText != null) slot.descText.text = isPlaced ? "(วางแล้ว)" : data.description;
                if (slot.iconImage != null) slot.iconImage.sprite = data.icon;

                var image = slot.button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isPlaced ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : Color.white;
                }

                slot.button.interactable = !isPlaced;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => SelectNPC(data));
            }
        }

        public void SelectNPC(NPCSkillData data)
        {
            if (data == null) return;

            var callback = _onSelected;
            _onSelected = null;

            if (callback != null)
            {
                callback.Invoke(data);
            }
            else if (basePersonData != null && Simulation.Building.BuildingSystem.Instance != null)
            {
                StructureData proxy = CreateNPCProxy(data, basePersonData);
                Simulation.Building.BuildingSystem.Instance.SelectStructure(proxy);
            }
            else
            {
                Debug.LogWarning("[NPCSelectionUI] No callback or basePersonData assigned for selection!");
            }

            Close();
            SetupSlots();
        }

        private StructureData CreateNPCProxy(NPCSkillData data, StructureData baseData)
        {
            StructureData proxy = ScriptableObject.CreateInstance<StructureData>();
            proxy.structureName = data.npcName;
            proxy.basePrice = data.placementPrice;
            proxy.baseMass = baseData.baseMass;
            proxy.baseHP = baseData.baseHP;
            proxy.size = baseData.size;
            proxy.prefab = baseData.prefab;
            proxy.defaultMaterial = baseData.defaultMaterial;
            proxy.allowOverlap = baseData.allowOverlap;
            proxy.structureType = baseData.structureType;
            proxy.placeOnStructureOnly = baseData.placeOnStructureOnly;
            return proxy;
        }

        public void ShowSkillPanel(NPCController npc)
        {
            _currentNPC = npc;
            if (skillPanel != null)
            {
                skillPanel.SetActive(true);
            }
            if (npc == null || npc.Data == null) return;

            if (skillNPCNameText != null)
            {
                skillNPCNameText.text = npc.Data.npcName;
                skillNPCNameText.color = GetSkillColor(npc.Data.skillType);
            }

            if (skillNPCSkillText != null)
            {
                skillNPCSkillText.text = $"Skill: {GetSkillName(npc.Data.skillType)}";
            }

            bool isPassive = npc.Data.skillType == NPCSkillType.Economist
                          || npc.Data.skillType == NPCSkillType.Architect
                          || npc.Data.skillType == NPCSkillType.Politician;

            if (skillNPCUseButton == null) return;

            skillNPCUseButton.interactable = !isPassive;

            var btnTextComp = skillNPCUseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTextComp != null)
            {
                btnTextComp.text = isPassive ? "Passive" : "Use Skill";
            }
            else
            {
                var legacyText = skillNPCUseButton.GetComponentInChildren<Text>();
                if (legacyText != null) legacyText.text = isPassive ? "Passive" : "Use Skill";
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

        public void HideSkillPanel()
        {
            _currentNPC = null;
            if (skillPanel != null) skillPanel.SetActive(false);
        }

        private Color GetSkillColor(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer: return new Color(0.3f, 0.8f, 0.3f);
                case NPCSkillType.Builder: return new Color(0.9f, 0.6f, 0.2f);
                case NPCSkillType.Economist: return new Color(0.3f, 0.5f, 0.9f);
                case NPCSkillType.Architect: return new Color(0.7f, 0.3f, 0.9f);
                case NPCSkillType.Politician: return new Color(0.2f, 0.2f, 0.8f);
                case NPCSkillType.Commander: return new Color(0.9f, 0.2f, 0.2f);
                case NPCSkillType.None: return Color.gray;
                default: return Color.white;
            }
        }

        private string GetSkillName(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer: return "-";
                case NPCSkillType.Builder: return "Repair Structures";
                case NPCSkillType.Economist: return "10% Discount (Auto)";
                case NPCSkillType.Architect: return "20% Reflect Damage";
                case NPCSkillType.Politician: return "No Tax (Auto)";
                case NPCSkillType.Commander: return "Call Soldier Support";
                case NPCSkillType.None: return "No Skill";
                default: return "Unknown";
            }
        }
    }
}
