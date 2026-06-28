using UnityEngine;
using Simulation.Data;
using Simulation.Building;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Simulation.UI
{
    [System.Serializable]
    public class StructureMaterialSlot
    {
        public Button button;
        public StructureData structureData;
        public MaterialData materialData;
    }

    /// <summary>
    /// UI Controller สำหรับระบบ Building
    /// ลากสคริปต์นี้ใส่ปุ่ม แล้วเลือกฟังก์ชันที่ต้องการใน OnClick()
    /// 
    /// ฟังก์ชันที่ใช้ได้:
    ///   - StartBuilding()  → เริ่มโหมดสร้าง (ต้องใส่ StructureData ในช่อง structureToBuild)
    ///   - StartMoving()    → เริ่มโหมดเลื่อนของ (คลิกของในฉากเพื่อหยิบ)
    ///   - StartDeleting()  → เริ่มโหมดลบ/ขาย (คลิกของในฉากเพื่อลบ)
    ///   - Cancel()         → ยกเลิกโหมดปัจจุบัน กลับ Idle
    /// </summary>
    public class BuildUIController : MonoBehaviour
    {
        [Header("UI Audio")]
        [Tooltip("เสียงที่จะเล่นเมื่อกดปุ่ม (สามารถเอา AudioSource มาแปะใส่ปุ่มเองได้ แต่ใส่ตรงนี้สะดวกกว่า)")]
        [SerializeField] private AudioClip buttonClickSound;

        [Header("สำหรับโหมดสร้างเท่านั้น")]
        [Tooltip("ลากไฟล์ StructureData มาใส่ที่นี่ (ใช้เฉพาะตอนกด StartBuilding)")]
        public StructureData structureToBuild;

        [Tooltip("ลากไฟล์ GadgetData มาใส่ที่นี่ (ใช้เฉพาะตอนกด StartBuildingGadget)")]
        public GadgetData gadgetToBuild;

        [Header("สำหรับเปลี่ยน Material")]
        [Tooltip("ลากไฟล์ MaterialData มาใส่ที่นี่ (ใช้กับ SelectMaterial)")]
        public MaterialData materialToSelect;

        [Header("สำหรับปุ่มรวม Structure + Material")]
        [Tooltip("ลาก StructureData มาใส่ (ใช้เฉพาะตอนกด StartBuildingWithMaterial)")]
        public StructureData structureWithMaterial;
        [Tooltip("ลาก MaterialData มาใส่ (ใช้เฉพาะตอนกด StartBuildingWithMaterial)")]
        public MaterialData materialForStructure;

        [Header("Structure + Material Slots")]
        [SerializeField] private List<StructureMaterialSlot> structureMaterialSlots = new List<StructureMaterialSlot>();

        [Header("Panels")]
        [Tooltip("ลาก Panel สิ่งก่อสร้าง (Structure) มาใส่ที่นี่")]
        [SerializeField] private GameObject structurePanel;
        [Tooltip("ลาก Panel อุปกรณ์ (Gadget) มาใส่ที่นี่")]
        [SerializeField] private GameObject gadgetPanel;

        [Header("Delete Button Setup")]
        [Tooltip("ลากปุ่ม Delete มาใส่ที่นี่ เพื่อให้ระบบกดค้างทำงานได้โดยอัตโนมัติ (หากปล่อยว่างระบบจะพยายามค้นหาในซีนเอง)")]
        [SerializeField] private Button deleteButton;

        [Tooltip("ปุ่ม/Panel ที่จะ 'ซ่อน' อัตโนมัติตอนเริ่มจำลอง (กด Start) แล้วโชว์กลับตอนหยุด — ลากปุ่ม build/debug ทั้งหมดมาใส่ที่นี่")]
        [SerializeField] private GameObject[] hideDuringSimulation;

        private bool _wasSimulating = false;

        private void Start()
        {
            SetupStructureMaterialSlots();

            // ค้นหาและตั้งค่า EventTrigger สำหรับปุ่ม Delete อัตโนมัติที่ Runtime
            if (deleteButton == null)
            {
                Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
                foreach (var btn in allButtons)
                {
                    if (btn.name.ToLower().Contains("delete") || btn.name.ToLower().Contains("remove"))
                    {
                        deleteButton = btn;
                        break;
                    }
                }
            }

            if (deleteButton != null)
            {
                SetupDeleteButtonTriggers(deleteButton);
            }
        }

        private void SetupDeleteButtonTriggers(Button btn)
        {
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            // ลบ onClick เดิมออกเพื่อไม่ให้ซ้ำซ้อน เพราะเราคุมทั้งคลิกสั้นและกดค้างผ่าน PointerDown/Up แล้ว
            btn.onClick.RemoveAllListeners();

            // ล้าง triggers เก่าที่อาจจะซ้ำ
            trigger.triggers.Clear();

            // สร้าง PointerDown
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnDeleteButtonDown(); });
            trigger.triggers.Add(pointerDownEntry);

            // สร้าง PointerUp
            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { OnDeleteButtonUp(); });
            trigger.triggers.Add(pointerUpEntry);
        }

        private void PlayClickSound()
        {
            if (buttonClickSound != null && UnityEngine.Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(buttonClickSound, UnityEngine.Camera.main.transform.position);
            }
        }

        /// <summary>
        /// เริ่มโหมดสร้าง — ต้องกำหนด structureToBuild ก่อน
        /// ใช้ลากใส่ OnClick() ของปุ่ม "สร้าง"
        /// </summary>
        public void StartBuilding()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || structureToBuild == null) return;
            BuildingSystem.Instance.SelectStructure(structureToBuild);
        }

        /// <summary>
        /// เริ่มโหมดสร้างพร้อมระบุโครงสร้างและวัสดุพร้อมกันจากตัวแปรใน Inspector (ปุ่มเดียวจบ)
        /// </summary>
        public void StartBuildingWithMaterial()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || structureWithMaterial == null) return;

            if (materialForStructure != null) BuildingSystem.Instance.SelectMaterial(materialForStructure);
            else BuildingSystem.Instance.ClearMaterial();

            BuildingSystem.Instance.SelectStructure(structureWithMaterial);
        }

        /// <summary>
        /// เริ่มโหมดสร้างพร้อมระบุโครงสร้างและวัสดุผ่าน Parameter
        /// </summary>
        public void StartBuildingWithMaterialData(StructureData structure, MaterialData material)
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || structure == null) return;

            if (material != null) BuildingSystem.Instance.SelectMaterial(material);
            else BuildingSystem.Instance.ClearMaterial();

            BuildingSystem.Instance.SelectStructure(structure);
        }

        private void SetupStructureMaterialSlots()
        {
            foreach (var slot in structureMaterialSlots)
            {
                if (slot == null || slot.button == null || slot.structureData == null) continue;

                StructureData structure = slot.structureData;
                MaterialData material = slot.materialData;
                slot.button.onClick.AddListener(() => StartBuildingWithMaterialData(structure, material));
            }
        }

        public void StartBuildingWithMaterialSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= structureMaterialSlots.Count)
            {
                Debug.LogWarning($"[BuildUIController] Structure/material slot index out of range: {slotIndex}");
                return;
            }

            StructureMaterialSlot slot = structureMaterialSlots[slotIndex];
            if (slot == null || slot.structureData == null)
            {
                Debug.LogWarning($"[BuildUIController] Structure/material slot {slotIndex} is missing structure data.");
                return;
            }

            StartBuildingWithMaterialData(slot.structureData, slot.materialData);
        }

        /// <summary>
        /// เริ่มโหมดสร้าง — รับข้อมูลผ่าน Parameter (ใช้ในระบบ Inventory/Slot ได้)
        /// </summary>
        public void StartBuildingWithData(UnityEngine.Object data)
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || data == null) return;
            
            if (data is StructureData structureData)
            {
                // เช็คว่าเป็น NPC หรือไม่ (ดูจากประเภทหรือชื่อ)
                // ถ้าใช่ ให้เปิดหน้าต่างเลือกแทนการวางแบบเดิม
                if (structureData.prefab != null && structureData.prefab.GetComponent<Simulation.Character.PersonTarget>() != null)
                {
                    OpenNPCSelection(structureData);
                    return;
                }

                BuildingSystem.Instance.SelectStructure(structureData);
            }
            else if (data is GadgetData gadgetData)
            {
                BuildingSystem.Instance.SelectFurniture(gadgetData);
            }
            else
            {
                Debug.LogWarning($"[BuildUIController] StartBuildingWithData received unsupported object type: {data.GetType()}");
            }
        }

        /// <summary>
        /// เปิดหน้าต่างเลือกตัวละคร NPC
        /// </summary>
        public void OpenNPCSelection(StructureData basePersonData)
        {
            PlayClickSound();
            if (Simulation.NPC.NPCSelectionUI.Instance != null)
            {
                Simulation.NPC.NPCSelectionUI.Instance.Open((selectedNPCData) => 
                {
                    // สร้าง proxy StructureData ที่รวมข้อมูล basePersonData กับ selectedNPCData
                    StructureData proxy = ScriptableObject.CreateInstance<StructureData>();
                    proxy.structureName = selectedNPCData.npcName;
                    proxy.basePrice = selectedNPCData.placementPrice;
                    proxy.baseMass = basePersonData.baseMass;
                    proxy.baseHP = basePersonData.baseHP;
                    proxy.size = basePersonData.size;
                    
                    // ใช้ prefab ตัวคนใสๆ เป็นเป้าหมายเหมือนเดิม
                    proxy.prefab = basePersonData.prefab; 
                    proxy.defaultMaterial = basePersonData.defaultMaterial;
                    proxy.allowOverlap = basePersonData.allowOverlap;
                    proxy.structureType = basePersonData.structureType;
                    proxy.placeOnStructureOnly = basePersonData.placeOnStructureOnly;
                    
                    // เก็บ NPCSkillData ไว้ในชื่อหรือหาที่ส่งต่อ
                    // แต่ระบบเก่า SimulationManager ใช้ PersonTarget จากฉากเพื่อ Spawn
                    // ดังนั้นเราต้องให้ PersonTarget รู้ว่ามันคืออาชีพอะไร
                    // วิธีที่ง่ายที่สุดคือสร้าง PersonTarget พิเศษ หรือแก้ไข PersonTarget นิดหน่อย
                    
                    BuildingSystem.Instance.SelectStructure(proxy);
                    
                    // หมายเหตุ: การเชื่อมต่อตัว NPC ที่เลือกเข้ากับ PersonTarget 
                    // จะถูกจัดการใน BuildingSystem หรือ PersonTarget ตอนวางลงไป
                });
            }
        }

        /// <summary>
        /// เริ่มโหมดสร้าง Gadget — ต้องกำหนด furnitureToBuild ก่อน
        /// </summary>
        public void StartBuildingGadget()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || gadgetToBuild == null) return;
            BuildingSystem.Instance.SelectFurniture(gadgetToBuild);
        }

        /// <summary>
        /// เริ่มโหมดสร้าง Gadget — รับข้อมูลผ่าน Parameter
        /// </summary>
        public void StartBuildingGadgetWithData(GadgetData data)
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || data == null) return;
            BuildingSystem.Instance.SelectFurniture(data);
        }

        /// <summary>
        /// เลือก Material ที่จะใช้สร้าง (ใช้ลากใส่ OnClick ของปุ่มเลือกวัสดุ)
        /// เปลี่ยน Material ทันทีและจำค่าไว้ข้ามโหมด
        /// </summary>
        public void SelectMaterial()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || materialToSelect == null) return;
            BuildingSystem.Instance.SelectMaterial(materialToSelect);
        }

        /// <summary>
        /// เลือก Material ที่จะใช้สร้าง — รับข้อมูลผ่าน Parameter
        /// </summary>
        public void SelectMaterialWithData(MaterialData data)
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null || data == null) return;
            BuildingSystem.Instance.SelectMaterial(data);
        }

        /// <summary>
        /// ล้าง Material ที่เลือกไว้ กลับไปใช้ค่าเริ่มต้นของ StructureData
        /// </summary>
        public void ClearMaterial()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null) return;
            BuildingSystem.Instance.ClearMaterial();
        }

        /// <summary>
        /// เริ่มโหมดเลื่อนของ — คลิกที่ของในฉากเพื่อหยิบขึ้นมาย้าย (Toggle)
        /// </summary>
        public void StartMoving()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null) return;

            // Toggle: ถ้าอยู่ในโหมดนี้อยู่แล้ว ให้ยกเลิกกลับสู่ Idle
            if (BuildingSystem.Instance.CurrentMode == BuildingSystem.BuildMode.Moving)
                BuildingSystem.Instance.ExitMode();
            else
                BuildingSystem.Instance.EnterMoveMode();
        }

        /// <summary>
        /// เริ่มโหมดลบ/ขาย — คลิกที่ของในฉากเพื่อลบและได้เงินคืน (Toggle)
        /// กดค้าง 2 วิ = ลบทั้งหมด (Delete All)
        /// 
        /// วิธีใช้ใน UI:
        ///   - ใส่ EventTrigger บนปุ่ม Delete
        ///   - PointerDown  → OnDeleteButtonDown()
        ///   - PointerUp    → OnDeleteButtonUp()
        /// </summary>

        [Header("Delete Hold Settings")]
        [Tooltip("ระยะเวลากดค้างเพื่อลบทั้งหมด (วินาที)")]
        [SerializeField] private float deleteHoldDuration = 2f;

        private bool _isHoldingDeleteButton = false;
        private float _deleteHoldTimer = 0f;
        private bool _deleteAllTriggered = false;

        private void Update()
        {
            // ── ซ่อนปุ่ม build ตอนเริ่มจำลอง (Play) แล้วโชว์กลับตอนหยุด ──
            bool sim = Simulation.Physics.SimulationManager.Instance != null
                       && Simulation.Physics.SimulationManager.Instance.IsSimulating;
            if (sim != _wasSimulating)
            {
                _wasSimulating = sim;
                if (hideDuringSimulation != null)
                {
                    foreach (var go in hideDuringSimulation)
                        if (go != null) go.SetActive(!sim);
                }
            }

            // นับเวลากดค้างปุ่ม Delete
            if (_isHoldingDeleteButton && !_deleteAllTriggered)
            {
                _deleteHoldTimer += Time.unscaledDeltaTime;

                if (_deleteHoldTimer >= deleteHoldDuration)
                {
                    _deleteAllTriggered = true;
                    PlayClickSound();

                    if (BuildingSystem.Instance != null)
                    {
                        BuildingSystem.Instance.DeleteAllStructures();
                    }

                    Debug.Log("<color=orange>🗑 Hold complete — Deleted ALL!</color>");
                }
            }
        }

        /// <summary>
        /// เรียกจาก EventTrigger → PointerDown บนปุ่ม Delete
        /// </summary>
        public void OnDeleteButtonDown()
        {
            _isHoldingDeleteButton = true;
            _deleteHoldTimer = 0f;
            _deleteAllTriggered = false;
        }

        /// <summary>
        /// เรียกจาก EventTrigger → PointerUp บนปุ่ม Delete
        /// </summary>
        public void OnDeleteButtonUp()
        {
            _isHoldingDeleteButton = false;

            // ถ้ากดสั้นๆ (ไม่ถึง 2 วิ) → Toggle โหมด Delete ตามปกติ
            if (!_deleteAllTriggered)
            {
                StartDeleting();
            }

            _deleteHoldTimer = 0f;
            _deleteAllTriggered = false;
        }

        /// <summary>
        /// Toggle โหมดลบ/ขาย (ใช้เมื่อกดสั้นๆ)
        /// </summary>
        public void StartDeleting()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null) return;

            // Toggle: ถ้าอยู่ในโหมดนี้อยู่แล้ว ให้ยกเลิกกลับสู่ Idle
            if (BuildingSystem.Instance.CurrentMode == BuildingSystem.BuildMode.Deleting)
                BuildingSystem.Instance.ExitMode();
            else
                BuildingSystem.Instance.EnterDeleteMode();
        }

        /// <summary>
        /// เริ่มโหมดระบายสี/เปลี่ยนวัสดุ — คลิกที่ของในฉากเพื่อเปลี่ยนวัสดุ (Toggle)
        /// </summary>
        public void StartPainting()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null) return;

            // Toggle
            if (BuildingSystem.Instance.CurrentMode == BuildingSystem.BuildMode.Painting)
                BuildingSystem.Instance.ExitMode();
            else
                BuildingSystem.Instance.EnterPaintMode();
        }

        /// <summary>
        /// ยกเลิกโหมดปัจจุบัน กลับสู่ Idle
        /// </summary>
        public void Cancel()
        {
            PlayClickSound();
            if (BuildingSystem.Instance == null) return;
            BuildingSystem.Instance.ExitMode();
        }

        // --------------------------------------------------------------------------------
        // Panel Switching (Structure vs Gadget)
        // --------------------------------------------------------------------------------

        /// <summary>
        /// สลับไปแสดง Panel สิ่งก่อสร้าง (Structure) และปิด Panel อุปกรณ์ (Gadget)
        /// </summary>
        public void SwitchToStructurePanel()
        {
            PlayClickSound();
            if (structurePanel != null) structurePanel.SetActive(true);
            if (gadgetPanel != null) gadgetPanel.SetActive(false);
        }

        public void SwitchToGadgetPanel()
        {
            PlayClickSound();
            if (gadgetPanel != null) gadgetPanel.SetActive(true);
            if (structurePanel != null) structurePanel.SetActive(false);
        }

        // --------------------------------------------------------------------------------
        // Simulation Controls
        // --------------------------------------------------------------------------------

        /// <summary>
        /// เริ่มการจำลองฟิสิกส์ (Play)
        /// </summary>
        public void StartSimulation()
        {
            if (Simulation.Physics.SimulationManager.Instance != null)
                Simulation.Physics.SimulationManager.Instance.StartSimulation();
                
            ResetTimeSpeed();
        }

        /// <summary>
        /// หยุดการจำลองฟิสิกส์ (Stop)
        /// </summary>
        public void StopSimulation()
        {
            if (Simulation.Physics.SimulationManager.Instance != null)
                Simulation.Physics.SimulationManager.Instance.StopSimulation();
                
            ResetTimeSpeed();
        }

        private bool _isGridVisible = true; // Default state of grid visualizer in scene
        private bool _isStressVisible = false; // Default state of stress visuals

        /// <summary>
        /// ฟังก์ชันหลักสำหรับปุ่ม UI GridShow (กดสลับ เปิด/ปิด Grid)
        /// </summary>
        public void ToggleGrid()
        {
            PlayClickSound();
            if (Simulation.Physics.SimulationManager.Instance != null)
            {
                _isGridVisible = !Simulation.Physics.SimulationManager.Instance.IsGridVisible;
                Simulation.Physics.SimulationManager.Instance.SetGridVisibility(_isGridVisible);
            }
        }

        /// <summary>
        /// ฟังก์ชันหลักสำหรับปุ่ม UI WeightShow (กดสลับ เปิด/ปิด สีแสดงแรงกดทับ)
        /// </summary>
        public void ToggleStressVisuals()
        {
            PlayClickSound();
            _isStressVisible = !Simulation.Physics.StructuralStress.ShowHPVisualsGlobal;
            Simulation.Physics.StructuralStress.SetVisualStatus(_isStressVisible);
        }

        // --- Undo / Redo Wrappers ---
        public void UndoAction()
        {
            PlayClickSound();
            if (BuildingSystem.Instance != null) BuildingSystem.Instance.Undo();
        }

        public void RedoAction()
        {
            PlayClickSound();
            if (BuildingSystem.Instance != null) BuildingSystem.Instance.Redo();
        }

        // --------------------------------------------------------------------------------
        // Time Controls
        // --------------------------------------------------------------------------------

        private float[] _timeScaleSteps = { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f };
        private int _currentTimeStepIndex = 2; // เริ่มต้นที่ 1.0x (Index 2)

        /// <summary>
        /// รีเซ็ตความเร็วกลับเป็น 1.0x
        /// </summary>
        public void ResetTimeSpeed()
        {
            _currentTimeStepIndex = 2;
            ApplyCurrentTimeScale();
        }

        /// <summary>
        /// เพิ่มความเร็วเวลา (เลื่อนขึ้นทีละสเต็ป)
        /// </summary>
        public void IncreaseTimeSpeed()
        {
            if (_currentTimeStepIndex < _timeScaleSteps.Length - 1)
            {
                _currentTimeStepIndex++;
                ApplyCurrentTimeScale();
            }
        }

        /// <summary>
        /// ลดความเร็วเวลา (เลื่อนลงทีละสเต็ป)
        /// </summary>
        public void DecreaseTimeSpeed()
        {
            if (_currentTimeStepIndex > 0)
            {
                _currentTimeStepIndex--;
                ApplyCurrentTimeScale();
            }
        }

        private void ApplyCurrentTimeScale()
        {
            float newScale = _timeScaleSteps[_currentTimeStepIndex];
            Time.timeScale = newScale;
            Debug.Log($"<color=cyan>⏱ Time Scale set to {newScale}x</color>");
        }
    }
}
