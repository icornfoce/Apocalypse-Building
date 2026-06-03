using UnityEngine;
using UnityEngine.UI;
using Simulation.Data;

namespace Simulation.NPC
{
    /// <summary>
    /// UI Panel แสดงข้อมูล NPC ที่เลือกอยู่ + ปุ่มใช้สกิล
    /// สร้างแบบ Procedural — จะแสดงเมื่อเลือก NPC ตอน Simulate
    /// </summary>
    public class NPCSkillPanelUI : MonoBehaviour
    {
        public static NPCSkillPanelUI Instance { get; private set; }

        [Header("References")]
        public Canvas parentCanvas;

        // ── Runtime ──
        private GameObject _panelRoot;
        private Text _nameText;
        private Text _healthText;
        private Text _skillText;
        private Text _statusText;
        private Button _skillButton;
        private Image _healthBar;
        private NPCController _currentNPC;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        private void Start()
        {
            // ลงทะเบียน Events
            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected += ShowPanel;
                NPCSkillManager.Instance.OnNPCDeselected += HidePanel;
            }
        }

        private void OnDestroy()
        {
            if (NPCSkillManager.Instance != null)
            {
                NPCSkillManager.Instance.OnNPCSelected -= ShowPanel;
                NPCSkillManager.Instance.OnNPCDeselected -= HidePanel;
            }
        }

        private void Update()
        {
            if (_currentNPC == null || _panelRoot == null || !_panelRoot.activeSelf) return;

            // อัพเดตข้อมูลแบบ Realtime
            if (_healthText != null)
            {
                _healthText.text = $"HP: {_currentNPC.CurrentHealth:F0}/{_currentNPC.Data.maxHealth:F0}";
            }

            if (_healthBar != null)
            {
                _healthBar.fillAmount = _currentNPC.HealthRatio;
                _healthBar.color = Color.Lerp(Color.red, Color.green, _currentNPC.HealthRatio);
            }

            if (_statusText != null)
            {
                _statusText.text = _currentNPC.SkillActive ? "สกิลทำงานอยู่..." : "พร้อมใช้งาน";
                _statusText.color = _currentNPC.SkillActive ? Color.yellow : Color.green;
            }
        }

        public void ShowPanel(NPCController npc)
        {
            _currentNPC = npc;
            if (_panelRoot != null) Destroy(_panelRoot);
            CreatePanel(npc);
        }

        public void HidePanel()
        {
            _currentNPC = null;
            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
                _panelRoot = null;
            }
        }

        private void CreatePanel(NPCController npc)
        {
            if (parentCanvas == null)
            {
                parentCanvas = FindAnyObjectByType<Canvas>();
                if (parentCanvas == null) return;
            }

            var data = npc.Data;
            if (data == null) return;

            // ── Panel Root ── (มุมล่างซ้าย)
            _panelRoot = new GameObject("NPCSkillPanel");
            _panelRoot.transform.SetParent(parentCanvas.transform, false);

            var panelImg = _panelRoot.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.sizeDelta = new Vector2(320, 200);
            panelRect.anchoredPosition = new Vector2(15, 15);

            // ── Vertical Layout ──
            var layout = _panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // ── NPC Name ──
            _nameText = CreateLabel(_panelRoot.transform, data.npcName, 22, FontStyle.Bold, GetSkillColor(data.skillType));
            var nameLayout = _nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 30;

            // ── Health Bar ──
            GameObject hpBarBg = new GameObject("HPBarBg");
            hpBarBg.transform.SetParent(_panelRoot.transform, false);
            var bgImg = hpBarBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            var bgLayout = hpBarBg.AddComponent<LayoutElement>();
            bgLayout.preferredHeight = 16;

            GameObject hpBarFill = new GameObject("HPBarFill");
            hpBarFill.transform.SetParent(hpBarBg.transform, false);
            _healthBar = hpBarFill.AddComponent<Image>();
            _healthBar.color = Color.green;
            _healthBar.type = Image.Type.Filled;
            _healthBar.fillMethod = Image.FillMethod.Horizontal;
            _healthBar.fillAmount = npc.HealthRatio;

            var fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // ── Health Text ──
            _healthText = CreateLabel(_panelRoot.transform, $"HP: {npc.CurrentHealth:F0}/{data.maxHealth:F0}", 14, FontStyle.Normal, Color.white);
            var hpLayout = _healthText.gameObject.AddComponent<LayoutElement>();
            hpLayout.preferredHeight = 20;

            // ── Skill Description ──
            string skillDesc = GetSkillName(data.skillType);
            _skillText = CreateLabel(_panelRoot.transform, $"สกิล: {skillDesc}", 14, FontStyle.Normal, new Color(0.8f, 0.8f, 1f));
            var skillLayout = _skillText.gameObject.AddComponent<LayoutElement>();
            skillLayout.preferredHeight = 20;

            // ── Status ──
            _statusText = CreateLabel(_panelRoot.transform, "พร้อมใช้งาน", 12, FontStyle.Italic, Color.green);
            var statusLayout = _statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 18;

            // ── Skill Button ──
            bool isPassive = data.skillType == NPCSkillType.Economist
                          || data.skillType == NPCSkillType.Architect
                          || data.skillType == NPCSkillType.Politician;

            string btnText = isPassive ? "สกิลอัตโนมัติ (Passive)" : "ใช้สกิล";
            Color btnColor = isPassive ? new Color(0.3f, 0.3f, 0.3f) : GetSkillColor(data.skillType);

            GameObject btnObj = new GameObject("SkillButton");
            btnObj.transform.SetParent(_panelRoot.transform, false);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = btnColor;

            _skillButton = btnObj.AddComponent<Button>();
            _skillButton.interactable = !isPassive;

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 35;

            var btnTextComp = CreateLabel(btnObj.transform, btnText, 16, FontStyle.Bold, Color.white);
            btnTextComp.alignment = TextAnchor.MiddleCenter;
            var btnTextRect = btnTextComp.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            // Click handler
            _skillButton.onClick.AddListener(() =>
            {
                if (NPCSkillManager.Instance != null)
                    NPCSkillManager.Instance.ActivateSelectedSkill();
            });
        }

        private Text CreateLabel(Transform parent, string text, int fontSize, FontStyle style, Color color)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
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
