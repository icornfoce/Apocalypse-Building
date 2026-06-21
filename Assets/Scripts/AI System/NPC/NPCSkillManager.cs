using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Building;
using Simulation.Physics;
using Simulation.Character;

namespace Simulation.NPC
{
    /// <summary>
    /// ระบบกลางจัดการ NPC ทั้งหมด
    /// - ติดตาม NPC ที่วางอยู่ในฉาก
    /// - จัดการสกิล Passive (Economist ลดราคา, Politician ยกเลิกภาษี)
    /// - จัดการสกิล Architect (เช็คเงื่อนไข + ให้บัฟ)
    /// - เลือก NPC + สั่งเดิน + ใช้สกิล
    /// </summary>
    public class NPCSkillManager : MonoBehaviour
    {
        public static NPCSkillManager Instance { get; private set; }

        [Header("NPC Roster")]
        [Tooltip("รายชื่อ NPC ทั้ง 6 ตัวที่สามารถเลือกวางได้")]
        public List<NPCSkillData> availableNPCs = new List<NPCSkillData>();

        [Header("Selection")]
        [Tooltip("NPC ที่ถูกเลือกอยู่ในขณะนี้ (Runtime)")]
        [SerializeField] private NPCController _selectedNPC;

        [Header("VFX Defaults")]
        [Tooltip("VFX วงแหวนเลือก (ใช้เมื่อ NPC ไม่มี VFX ของตัวเอง)")]
        public GameObject defaultSelectionVFX;

        // ── Runtime State ──
        private List<NPCController> _spawnedNPCs = new List<NPCController>();
        private HashSet<NPCSkillType> _placedSkillTypes = new HashSet<NPCSkillType>();
        private GameObject _activeSelectionVFX;
        private GameObject _activeMoveMarker;

        // ── Passive Skill State ──
        private bool _economistActive = false;
        private bool _politicianActive = false;
        private bool _architectBuffActive = false;

        // ── Events ──
        public event System.Action<NPCController> OnNPCSelected;
        public event System.Action OnNPCDeselected;
        public event System.Action<NPCSkillType> OnSkillActivated;

        // ── Public Properties ──
        public NPCController SelectedNPC => _selectedNPC;
        public bool EconomistActive => _economistActive;
        public bool PoliticianActive => _politicianActive;
        public bool ArchitectBuffActive => _architectBuffActive;
        public IReadOnlyList<NPCController> SpawnedNPCs => _spawnedNPCs;

        /// <summary>
        /// เช็คว่า NPC ของ SkillType นี้ถูกวางไปแล้วหรือยัง (ป้องกันวางซ้ำ)
        /// </summary>
        public bool IsNPCPlaced(NPCSkillType type) => _placedSkillTypes.Contains(type);

        /// <summary>
        /// คืนค่า discount rate ปัจจุบัน (0.0 = ไม่ลด, 0.1 = ลด 10%)
        /// </summary>
        public float GetDiscountRate() => _economistActive ? 0.1f : 0f;

        /// <summary>
        /// คืนค่า tax rate ปัจจุบัน (ถ้า Politician active → 0)
        /// </summary>
        public float GetEffectiveTaxRate()
        {
            if (_politicianActive) return 0f;
            // ดึง tax rate จาก MissionData
            var mm = Mission.MissionManager.Instance;
            if (mm != null && mm.CurrentMission != null)
            {
                return mm.CurrentMission.taxRate;
            }
            return 0f;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating) return;

            // ── Handle NPC Selection & Commands ──
            HandleSelection();

            // ── Update Passive Skills ──
            UpdatePassiveSkills();
        }

        // ────────────────────────────────────────────────────────────────
        // NPC Spawning
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// วาง NPC ใหม่จาก NPCSkillData ที่ตำแหน่งที่กำหนด
        /// </summary>
        public NPCController SpawnNPC(NPCSkillData data, Vector3 position)
        {
            if (data == null || data.prefab == null) return null;
            if (_placedSkillTypes.Contains(data.skillType))
            {
                Debug.LogWarning($"[NPCSkillManager] NPC with skill {data.skillType} already placed!");
                return null;
            }

            GameObject obj = Instantiate(data.prefab, position, Quaternion.identity);
            obj.name = $"NPC_{data.npcName}";

            // เพิ่ม NPCController ถ้ายังไม่มี
            NPCController controller = obj.GetComponent<NPCController>();
            if (controller == null) controller = obj.AddComponent<NPCController>();
            controller.Initialize(data);

            _spawnedNPCs.Add(controller);
            _placedSkillTypes.Add(data.skillType);

            // อัปเดต Passive Skills
            RefreshPassiveSkills();

            Debug.Log($"<color=cyan>[NPCSkillManager]</color> Spawned NPC: {data.npcName} ({data.skillType})");
            return controller;
        }

        /// <summary>
        /// ลบ NPC ออกจากฉาก
        /// </summary>
        public void DespawnNPC(NPCController controller)
        {
            if (controller == null) return;
            if (_selectedNPC == controller) DeselectNPC();
            _spawnedNPCs.Remove(controller);
            if (controller.Data != null) _placedSkillTypes.Remove(controller.Data.skillType);
            Destroy(controller.gameObject);
            RefreshPassiveSkills();
        }

        /// <summary>
        /// ลบ NPC ทั้งหมดออก (ตอน StopSimulation)
        /// </summary>
        public void ClearAllNPCs()
        {
            DeselectNPC();
            foreach (var npc in _spawnedNPCs)
            {
                if (npc != null) Destroy(npc.gameObject);
            }
            _spawnedNPCs.Clear();
            _placedSkillTypes.Clear();
            _economistActive = false;
            _politicianActive = false;
            _architectBuffActive = false;
        }

        // ────────────────────────────────────────────────────────────────
        // NPC Selection & Movement
        // ────────────────────────────────────────────────────────────────

        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // เช็คว่ากดที่ UI หรือไม่
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                    if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        // เช็คว่าคลิกที่ NPC หรือไม่
                        NPCController clickedNPC = hit.collider.GetComponentInParent<NPCController>();
                        if (clickedNPC != null)
                        {
                            SelectNPC(clickedNPC);
                            return;
                        }
                    }
                }
            }

            // ── Right Click / Click Ground: สั่ง NPC เดิน ──
            if (_selectedNPC != null && Input.GetMouseButtonDown(1))
            {
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                    int groundMask = LayerMask.GetMask("Ground", "Structure");
                    if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
                    {
                        // ส่งคำสั่งเดินไปยัง NPC
                        _selectedNPC.MoveTo(hit.point);

                        // VFX ที่จุดกดพื้น (waypoint marker)
                        SpawnMoveVFX(hit.point);
                    }
                }
            }
        }

        public void SelectNPC(NPCController npc)
        {
            if (npc == null) return;
            if (_selectedNPC == npc) return; // เลือกตัวเดิมอยู่แล้ว

            // ยกเลิก Selection เก่า
            DeselectNPC();

            _selectedNPC = npc;
            npc.OnSelected();

            // VFX Selection
            ShowSelectionVFX(npc);

            // SFX Selection
            if (npc.Data != null && npc.Data.selectionSFX != null)
            {
                AudioSource.PlayClipAtPoint(npc.Data.selectionSFX, npc.transform.position);
            }

            OnNPCSelected?.Invoke(npc);
            Debug.Log($"<color=yellow>[NPCSkillManager]</color> Selected: {npc.name}");
        }

        public void DeselectNPC()
        {
            if (_selectedNPC != null)
            {
                _selectedNPC.OnDeselected();
                _selectedNPC = null;
            }

            HideSelectionVFX();
            OnNPCDeselected?.Invoke();
        }

        private void ShowSelectionVFX(NPCController npc)
        {
            HideSelectionVFX();

            GameObject vfxPrefab = npc.Data?.selectionVFX ?? defaultSelectionVFX;
            if (vfxPrefab != null)
            {
                _activeSelectionVFX = Instantiate(vfxPrefab, npc.transform);
                _activeSelectionVFX.transform.localPosition = Vector3.up * 0.05f;
            }
            else
            {
                // Procedural selection ring (ถ้าไม่มี VFX)
                _activeSelectionVFX = CreateProceduralSelectionRing(npc.transform);
            }
        }

        private void HideSelectionVFX()
        {
            if (_activeSelectionVFX != null)
            {
                Destroy(_activeSelectionVFX);
                _activeSelectionVFX = null;
            }
        }

        private GameObject CreateProceduralSelectionRing(Transform parent)
        {
            GameObject ring = new GameObject("SelectionRing");
            ring.transform.SetParent(parent);
            ring.transform.localPosition = Vector3.up * 0.05f;
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // สร้างวงแหวนจาก LineRenderer
            LineRenderer lr = ring.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0f, 1f, 0.5f, 0.8f); // สีเขียวมิ้นท์
            lr.material = mat;

            int segments = 32;
            float radius = 0.8f;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            // เพิ่ม Spin Animation
            ring.AddComponent<SpinEffect>();

            return ring;
        }

        private void SpawnMoveVFX(Vector3 position)
        {
            if (_activeMoveMarker != null)
            {
                Destroy(_activeMoveMarker);
            }

            // สร้าง VFX จุดหมายปลายทาง (วงกลมที่พื้นค่อยๆ จาง)
            GameObject marker = new GameObject("MoveMarker");
            _activeMoveMarker = marker;
            
            marker.transform.position = position + Vector3.up * 0.05f;
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            LineRenderer lr = marker.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 1f, 0f, 0.8f); // สีเหลือง
            lr.material = mat;

            int segments = 24;
            float radius = 0.5f;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, position + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
            }

            Destroy(marker, 1.5f);
        }

        // ────────────────────────────────────────────────────────────────
        // Skill Activation
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// เรียกใช้สกิลของ NPC ที่เลือกอยู่
        /// </summary>
        public void ActivateSelectedSkill()
        {
            if (_selectedNPC == null) return;
            _selectedNPC.ActivateSkill();
            OnSkillActivated?.Invoke(_selectedNPC.Data.skillType);
        }

        // ────────────────────────────────────────────────────────────────
        // Passive Skills
        // ────────────────────────────────────────────────────────────────

        private void RefreshPassiveSkills()
        {
            _economistActive = _placedSkillTypes.Contains(NPCSkillType.Economist);
            _politicianActive = _placedSkillTypes.Contains(NPCSkillType.Politician);

            // Architect: เช็คเงื่อนไขทุกครั้งที่มีการเปลี่ยนแปลง
            CheckArchitectConditions();
        }

        private void UpdatePassiveSkills()
        {
            // Architect: เช็คเงื่อนไขเรื่อยๆ (ทุก 1 วินาที)
            if (_placedSkillTypes.Contains(NPCSkillType.Architect))
            {
                if (Time.frameCount % 60 == 0)
                {
                    CheckArchitectConditions();
                }
            }
        }

        private void CheckArchitectConditions()
        {
            bool wasActive = _architectBuffActive;
            _architectBuffActive = false;

            if (!_placedSkillTypes.Contains(NPCSkillType.Architect)) return;

            // หา NPCSkillData ของสถาปนิก
            NPCSkillData architectData = null;
            foreach (var npc in _spawnedNPCs)
            {
                if (npc != null && npc.Data != null && npc.Data.skillType == NPCSkillType.Architect)
                {
                    architectData = npc.Data;
                    break;
                }
            }

            if (architectData == null) return;

            bool allConditionsMet = true;

            // เงื่อนไข 1: ห้ามมีเสาเกิน
            if (architectData.maxPillarCondition > 0)
            {
                int pillarCount = CountStructuresByName("pillar", "column", "เสา");
                if (pillarCount > architectData.maxPillarCondition)
                {
                    allConditionsMet = false;
                }
            }

            // เงื่อนไข 2: ห้ามพื้นที่เกิน
            if (architectData.maxAreaCondition > 0)
            {
                int totalArea = CountTotalFloorArea();
                if (totalArea > architectData.maxAreaCondition)
                {
                    allConditionsMet = false;
                }
            }

            // เงื่อนไข 3: ต้องมีอย่างน้อยกี่ชั้น
            if (architectData.minFloorCondition > 0)
            {
                int floors = CountFloors();
                if (floors < architectData.minFloorCondition)
                {
                    allConditionsMet = false;
                }
            }

            _architectBuffActive = allConditionsMet;

            // เมื่อ Buff เปิด/ปิด → อัพเดต Visual
            if (wasActive != _architectBuffActive)
            {
                UpdateArchitectAura();
            }
        }

        private void UpdateArchitectAura()
        {
            // เปิด/ปิดออร่าบนสิ่งก่อสร้างทั้งหมด
            var allStress = Object.FindObjectsByType<StructuralStress>(FindObjectsSortMode.None);
            foreach (var stress in allStress)
            {
                if (stress == null) continue;
                var aura = stress.GetComponent<ArchitectAura>();
                if (_architectBuffActive)
                {
                    if (aura == null)
                    {
                        aura = stress.gameObject.AddComponent<ArchitectAura>();
                    }
                    aura.SetActive(true);
                }
                else
                {
                    if (aura != null) aura.SetActive(false);
                }
            }

            if (_architectBuffActive)
                Debug.Log("<color=magenta>[Architect]</color> ★ Buff ACTIVATED — Damage Reflection 20%!");
            else
                Debug.Log("<color=gray>[Architect]</color> Buff deactivated.");
        }

        // ── Helper: นับโครงสร้าง ──

        private int CountStructuresByName(params string[] keywords)
        {
            var units = Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var unit in units)
            {
                if (unit == null || unit.Data == null) continue;
                string name = unit.Data.structureName.ToLower();
                foreach (var kw in keywords)
                {
                    if (name.Contains(kw.ToLower()))
                    {
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        private int CountTotalFloorArea()
        {
            var units = Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var unit in units)
            {
                if (unit == null || unit.Data == null) continue;
                if (unit.Data.structureType == StructureType.Floor)
                {
                    count += Mathf.Max(1, Mathf.RoundToInt(unit.Data.size.x * unit.Data.size.z));
                }
            }
            return count;
        }

        private int CountFloors()
        {
            var units = Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            HashSet<int> floorLevels = new HashSet<int>();
            float heightStep = BuildingSystem.Instance != null ? BuildingSystem.Instance.HeightStep : 3f;
            foreach (var unit in units)
            {
                if (unit == null || !unit.gameObject.activeSelf) continue;
                int floor = Mathf.FloorToInt(unit.transform.position.y / heightStep + 0.1f) + 1;
                floorLevels.Add(floor);
            }
            return floorLevels.Count;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Helper Components
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// เอฟเฟกต์หมุนช้าๆ (ใช้กับ Selection Ring)
    /// </summary>
    public class SpinEffect : MonoBehaviour
    {
        public float speed = 30f;
        void Update()
        {
            transform.Rotate(0f, 0f, speed * Time.deltaTime);
        }
    }

    /// <summary>
    /// ออร่าสถาปนิก — สะท้อนดาเมจ 20% กลับไปยังซอมบี้ที่โจมตี
    /// แปะบนโครงสร้างที่มี StructuralStress
    /// </summary>
    public class ArchitectAura : MonoBehaviour
    {
        [Header("Settings")]
        public float reflectPercent = 0.2f; // 20%
        public Color auraColor = new Color(0.3f, 0.6f, 1f, 0.3f);

        private bool _isActive = false;
        private GameObject _auraVisual;

        public bool IsActive => _isActive;

        public void SetActive(bool active)
        {
            _isActive = active;
            if (active)
            {
                ShowAuraVisual();
            }
            else
            {
                HideAuraVisual();
            }
        }

        private void ShowAuraVisual()
        {
            if (_auraVisual != null) return;

            // สร้างออร่าเรืองแสงรอบโครงสร้าง
            _auraVisual = new GameObject("AuraVisual");
            _auraVisual.transform.SetParent(transform);
            _auraVisual.transform.localPosition = Vector3.zero;

            // ใช้ Particle System สำหรับ Aura
            var ps = _auraVisual.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.3f;
            main.startSize = 0.15f;
            main.startColor = auraColor;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 10;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 1f, 1f);

            // ทำให้อนุภาคค่อยๆ จาง
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(auraColor, 0f), new GradientColorKey(auraColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = g;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = auraColor;
        }

        private void HideAuraVisual()
        {
            if (_auraVisual != null)
            {
                Destroy(_auraVisual);
                _auraVisual = null;
            }
        }

        private void OnDestroy()
        {
            HideAuraVisual();
        }
    }
}
