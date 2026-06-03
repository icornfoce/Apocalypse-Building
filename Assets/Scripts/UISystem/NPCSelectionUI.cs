using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Building;

namespace Simulation.NPC
{
    /// <summary>
    /// UI Panel เลือก NPC ตอนกดวาง PersonTarget
    /// แสดง NPC ทั้ง 6 ตัว ป้องกันวางตัวเดิมซ้ำ
    /// สร้าง UI แบบ Procedural (ผ่านโค้ด) ให้ใช้งานได้เลย
    /// </summary>
    public class NPCSelectionUI : MonoBehaviour
    {
        public static NPCSelectionUI Instance { get; private set; }

        [Header("References")]
        [Tooltip("Canvas ที่จะสร้าง UI ไว้ (ถ้าไม่กำหนดจะหาเอง)")]
        public Canvas parentCanvas;

        [Header("Settings")]
        [Tooltip("สีพื้นหลัง Panel")]
        public Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // ── Runtime ──
        private GameObject _panelRoot;
        private List<Button> _npcButtons = new List<Button>();
        private System.Action<NPCSkillData> _onSelected;
        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>
        /// เปิด Panel เลือก NPC — เรียกจาก BuildingSystem ตอนกดวาง PersonTarget
        /// </summary>
        public void Open(System.Action<NPCSkillData> onSelected)
        {
            _onSelected = onSelected;

            if (_panelRoot != null) Destroy(_panelRoot);
            CreatePanel();
            _isOpen = true;
        }

        /// <summary>
        /// ปิด Panel
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
                _panelRoot = null;
            }
            _npcButtons.Clear();
        }

        private void CreatePanel()
        {
            if (parentCanvas == null)
            {
                parentCanvas = FindAnyObjectByType<Canvas>();
                if (parentCanvas == null)
                {
                    Debug.LogError("[NPCSelectionUI] No Canvas found!");
                    return;
                }
            }

            var manager = NPCSkillManager.Instance;
            if (manager == null || manager.availableNPCs == null || manager.availableNPCs.Count == 0)
            {
                Debug.LogWarning("[NPCSelectionUI] No available NPCs in NPCSkillManager!");
                return;
            }

            // ── Panel Root ──
            _panelRoot = new GameObject("NPCSelectionPanel");
            _panelRoot.transform.SetParent(parentCanvas.transform, false);

            // Full-screen darken overlay
            var overlay = _panelRoot.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);
            var overlayRect = _panelRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // ── Center Panel ──
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_panelRoot.transform, false);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = panelColor;

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700, 500);
            panelRect.anchoredPosition = Vector2.zero;

            // ── Title ──
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "เลือกตัวละคร NPC";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(0, 50);
            titleRect.anchoredPosition = new Vector2(0, -25);

            // ── Grid Layout สำหรับ NPC Buttons ──
            GameObject grid = new GameObject("Grid");
            grid.transform.SetParent(panel.transform, false);

            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 130);
            gridLayout.spacing = new Vector2(15, 15);
            gridLayout.padding = new RectOffset(20, 20, 10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.offsetMin = new Vector2(10, 60);
            gridRect.offsetMax = new Vector2(-10, -60);

            // ── NPC Buttons ──
            _npcButtons.Clear();
            foreach (var npcData in manager.availableNPCs)
            {
                if (npcData == null) continue;
                CreateNPCButton(grid.transform, npcData);
            }

            // ── Close Button ──
            GameObject closeBtn = CreateButton(panel.transform, "✕", 36, new Color(0.8f, 0.2f, 0.2f));
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(40, 40);
            closeRect.anchoredPosition = new Vector2(-25, -25);
            closeBtn.GetComponent<Button>().onClick.AddListener(Close);
        }

        private void CreateNPCButton(Transform parent, NPCSkillData data)
        {
            bool isPlaced = NPCSkillManager.Instance.IsNPCPlaced(data.skillType);

            // ── Button Container ──
            GameObject btnObj = new GameObject($"Btn_{data.npcName}");
            btnObj.transform.SetParent(parent, false);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = isPlaced
                ? new Color(0.3f, 0.3f, 0.3f, 0.7f) // สีเทาถ้าวางแล้ว
                : GetSkillColor(data.skillType);

            var btn = btnObj.AddComponent<Button>();
            btn.interactable = !isPlaced;

            // ── Name ──
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform, false);
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = data.npcName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.UpperCenter;

            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(-5, -5);

            // ── Description ──
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform, false);
            var descText = descObj.AddComponent<Text>();
            descText.text = isPlaced ? "(วางแล้ว)" : GetSkillDescription(data.skillType);
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.color = isPlaced ? Color.gray : new Color(0.9f, 0.9f, 0.9f);
            descText.alignment = TextAnchor.UpperCenter;

            var descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.offsetMin = new Vector2(5, 5);
            descRect.offsetMax = new Vector2(-5, 0);

            // ── Click Handler ──
            NPCSkillData capturedData = data;
            btn.onClick.AddListener(() =>
            {
                _onSelected?.Invoke(capturedData);
                Close();
            });

            _npcButtons.Add(btn);
        }

        private GameObject CreateButton(Transform parent, string text, int fontSize, Color color)
        {
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.AddComponent<Image>();
            img.color = color;

            var btn = btnObj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var t = textObj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnObj;
        }

        private Color GetSkillColor(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer:  return new Color(0.2f, 0.5f, 0.2f, 0.85f); // เขียว
                case NPCSkillType.Builder:   return new Color(0.6f, 0.4f, 0.15f, 0.85f); // ส้ม
                case NPCSkillType.Economist: return new Color(0.15f, 0.35f, 0.6f, 0.85f); // น้ำเงิน
                case NPCSkillType.Architect: return new Color(0.5f, 0.2f, 0.6f, 0.85f); // ม่วง
                case NPCSkillType.Politician:return new Color(0.15f, 0.15f, 0.5f, 0.85f); // น้ำเงินเข้ม
                case NPCSkillType.Commander: return new Color(0.6f, 0.15f, 0.15f, 0.85f); // แดง
                default: return new Color(0.3f, 0.3f, 0.3f, 0.85f);
            }
        }

        private string GetSkillDescription(NPCSkillType type)
        {
            switch (type)
            {
                case NPCSkillType.Engineer:  return "แสดงแรงเค้นเป็นแถบสี";
                case NPCSkillType.Builder:   return "ซ่อมโครงสร้างเสียหาย";
                case NPCSkillType.Economist: return "ลดราคาก่อสร้าง 10%\n(Auto)";
                case NPCSkillType.Architect: return "บัฟสะท้อนดาเมจ 20%\n(ตามเงื่อนไข)";
                case NPCSkillType.Politician:return "ยกเลิกภาษี 100%\n(Auto)";
                case NPCSkillType.Commander: return "เรียกทหารมายิงซอมบี้";
                default: return "";
            }
        }
    }
}
