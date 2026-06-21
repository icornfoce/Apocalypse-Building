using UnityEngine;
using System.Collections.Generic;
using Simulation.Building;
using Simulation.Character;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Simulation.Physics
{
    /// <summary>
    /// Manager สำหรับคุมเวลากดเริ่ม/หยุด การจำลองฟิสิกส์ (เหมือนปุ่ม Play ใน Poly Bridge)
    /// เมื่อกด Stop จะย้อนกลับไปสถานะก่อน Start ทั้งหมด (Snapshot/Rewind)
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Material gridMaterial;

        private GameObject _proceduralGrid;
        private MeshFilter _gridMeshFilter;
        private MeshRenderer _gridMeshRenderer;

        [Header("State")]
        [SerializeField] private bool isSimulating = false;

        [Header("Character System")]
        [Tooltip("Prefab ของคนตัวจริง (ที่มี PersonAI) ที่จะเกิดมาตอนกด Start")]
        [SerializeField] private GameObject personAIPrefab;
        private List<GameObject> _spawnedCharacters = new List<GameObject>();

        public bool IsSimulating => isSimulating;

        // ────────────────────────────────────────────────────────────────
        // Snapshot System
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// เก็บสถานะก่อน Simulate ของแต่ละชิ้นส่วน
        /// </summary>
        private struct StructureSnapshot
        {
            public StructureUnit unit;
            public Vector3 position;
            public Quaternion rotation;
            public List<Rigidbody> connectedBodies; // เก็บ Joint ทุกตัวที่ต่ออยู่ (Main + Side joints)
            public bool wasActive;
        }

        private List<StructureSnapshot> _snapshots = new List<StructureSnapshot>();

        // ────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // แช่แข็งฟิสิกส์เริ่มต้น (ไม่ให้ชิ้นส่วนที่อยู่ในฉากแต่แรกร่วงลงมา)
            FreezeAllStructures();
        }

        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ใช้ฟังก์ชันนี้ใส่ในปุ่ม OnClick() เพื่อเริ่มการจำลอง
        /// </summary>
        public void StartSimulation()
        {
            if (isSimulating) return;
            isSimulating = true;

            // 1. ยกเลิกโหมดสร้างของทั้งหมดก่อน
            if (BuildingSystem.Instance != null)
            {
                BuildingSystem.Instance.ExitMode();
            }

            // 1.1 ซ่อน Grid เมื่อเริ่มเล่น
            SetGridVisibility(false);

            // 1.2 ตรวจสอบและซ่อมแซมจุดเชื่อมต่อทั้งหมดก่อนเริ่ม (Fix missing joints)
            if (BuildingSystem.Instance != null)
            {
                BuildingSystem.Instance.RefreshAllJoints();
            }

            // 2. บันทึก Snapshot ก่อนเริ่ม Simulate
            SaveSnapshots();

            // ปิด Agent ทั้งหมดชั่วคราวก่อนอบ เพื่อไม่ให้เกิด Error ว่าหา NavMesh ไม่เจอ
            NavMeshAgent[] existingAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            foreach (var agent in existingAgents)
            {
                agent.enabled = false;
            }

            // 2.2 Bake NavMesh สดๆ ก่อนให้คนเดิน
            RebuildNavMesh();

            // 2.5 เรียกคนออกมาเดิน
            SpawnCharacters();

            // 3. ปลดล็อค Kinematic เพื่อให้ฟิสิกส์ทำงาน และปลุก AI ที่วางไว้
            foreach (var snap in _snapshots)
            {
                if (snap.unit == null) continue;
                
                // ถ้าเป็นตัวละครที่เคยวางไว้ ให้เปิด AI
                PersonAI ai = snap.unit.GetComponent<PersonAI>();
                if (ai != null)
                {
                    ai.InitializeAgent();
                }

                Rigidbody rb = snap.unit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Ensure all mesh colliders are convex before making dynamic
                    foreach (var meshCol in snap.unit.GetComponentsInChildren<MeshCollider>())
                    {
                        meshCol.convex = true;
                    }

                    rb.isKinematic = false;
                    rb.WakeUp();
                }
            }

            Debug.Log("<color=green>▶ Start Simulation</color> - Physics started!");
        }

        /// <summary>
        /// คำนวณ NavMesh ใหม่แบบ Dynamic (เช่น เมื่อพื้นหรือกำแพงถล่ม)
        /// </summary>
        public void RebuildNavMesh()
        {
            NavMeshSurface surface = GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = gameObject.AddComponent<NavMeshSurface>();
                
                // ตั้งค่าพื้นผิวให้ใช้อบ
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; 
                
                // ตั้งค่า Agent Settings ให้เหมาะสมกับการเดินในตัวตึก
                // (สามารถปรับผ่าน Inspector ของ SimulationManager object ได้เลย)
            }
            
            // จำกัดการอบ NavMesh เฉพาะบนเลเยอร์ Ground และ Structure เท่านั้น
            surface.layerMask = LayerMask.GetMask("Ground", "Structure");
            
            surface.BuildNavMesh();
        }

        /// <summary>
        /// ใช้ฟังก์ชันนี้ใส่ในปุ่ม OnClick() เพื่อสลับเริ่ม/หยุด
        /// </summary>
        public void ToggleSimulation()
        {
            if (isSimulating) StopSimulation();
            else StartSimulation();
        }

        public void StopSimulation()
        {
            if (!isSimulating) return;
            isSimulating = false;

            // ล้างสถานะ NPC ก่อน (เพื่อให้ _placedSkillTypes ถูก reset สำหรับรอบถัดไป)
            if (Simulation.NPC.NPCSkillManager.Instance != null)
            {
                Simulation.NPC.NPCSkillManager.Instance.ClearAllNPCs();
            }

            // ลบตัวละครจริงทิ้ง
            ClearCharacters();

            // เปิด PersonTarget ที่ถูกปิดตอน StartFadeOut กลับมา (เพื่อให้เริ่มรอบใหม่ได้)
            ResetAllPersonTargets();

            // รีเซ็ตประตูทั้งหมดกลับสถานะปิด
            ResetAllDoors();

            // ลบ Break VFX เก่าทั้งหมดในฉาก
            var container = GameObject.Find("BreakVFXContainer_Runtime");
            if (container != null)
            {
                Destroy(container);
            }

            // ย้อนกลับไปสถานะก่อน Start (Rewind)
            RestoreSnapshots();

            // แสดง Grid กลับมาเมื่อหยุด
            SetGridVisibility(true);

            Debug.Log("<color=red>■ Stop Simulation</color> - Rewound to pre-simulation state.");
        }

        /// <summary>
        /// สั่งเปิด/ปิด Grid ได้จากภายนอก (เช่น จาก UI)
        /// </summary>
        public void SetGridVisibility(bool visible)
        {
            if (_proceduralGrid != null)
            {
                _proceduralGrid.SetActive(visible);
            }
        }

        public void SetGridHeight(float y)
        {
            if (_proceduralGrid != null)
            {
                Vector3 pos = _proceduralGrid.transform.position;
                _proceduralGrid.transform.position = new Vector3(pos.x, y, pos.z);
            }
        }

        /// <summary>
        /// อัปเดตขนาดของ Grid Visual ให้ตรงกับจำนวนช่องใน Logic
        /// โดยการสร้าง Mesh ของเส้น Grid ขึ้นมาใหม่ (Procedural)
        /// </summary>
        public void UpdateGridVisual(int cols, int rows, float gridSize)
        {
            if (_proceduralGrid == null)
            {
                _proceduralGrid = new GameObject("ProceduralGrid_Runtime");
                _proceduralGrid.transform.SetParent(null); // อยู่ที่ World Space เสมอเพื่อความแม่นยำ
                _proceduralGrid.transform.position = Vector3.zero;
                _proceduralGrid.transform.rotation = Quaternion.identity;
                _gridMeshFilter = _proceduralGrid.AddComponent<MeshFilter>();
                _gridMeshRenderer = _proceduralGrid.AddComponent<MeshRenderer>();

                if (gridMaterial != null)
                {
                    _gridMeshRenderer.sharedMaterial = gridMaterial;
                }
                else
                {
                    // Fallback material if not assigned
                    Shader shader = Shader.Find("Unlit/Color");
                    Material mat = new Material(shader != null ? shader : Shader.Find("Hidden/Internal-Colored"));
                    mat.color = new Color(0.3f, 0.6f, 1f, 0.5f);
                    _gridMeshRenderer.sharedMaterial = mat;
                }
            }

            // Create Grid Line Mesh
            Mesh mesh = new Mesh();
            mesh.name = "GridLineMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            float totalWidth = cols * gridSize;
            float totalDepth = rows * gridSize;
            float startX = -totalWidth * 0.5f;
            float startZ = -totalDepth * 0.5f;

            // Vertical lines (along Z)
            for (int x = 0; x <= cols; x++)
            {
                float xPos = startX + x * gridSize;
                int baseIdx = vertices.Count;
                vertices.Add(new Vector3(xPos, 0.05f, startZ));
                vertices.Add(new Vector3(xPos, 0.05f, startZ + totalDepth));
                indices.Add(baseIdx);
                indices.Add(baseIdx + 1);
            }

            // Horizontal lines (along X)
            for (int z = 0; z <= rows; z++)
            {
                float zPos = startZ + z * gridSize;
                int baseIdx = vertices.Count;
                vertices.Add(new Vector3(startX, 0.05f, zPos));
                vertices.Add(new Vector3(startX + totalWidth, 0.05f, zPos));
                indices.Add(baseIdx);
                indices.Add(baseIdx + 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            
            if (_gridMeshFilter.mesh != null) Destroy(_gridMeshFilter.mesh);
            _gridMeshFilter.mesh = mesh;
        }

        // ────────────────────────────────────────────────────────────────
        // Snapshot Save / Restore
        // ────────────────────────────────────────────────────────────────

        private void SaveSnapshots()
        {
            _snapshots.Clear();

            StructureUnit[] units = FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                var snap = new StructureSnapshot
                {
                    unit = unit,
                    position = unit.transform.position,
                    rotation = unit.transform.rotation,
                    wasActive = unit.gameObject.activeSelf,
                    connectedBodies = new List<Rigidbody>()
                };

                // บันทึก Joint connection ทุกตัว (ทั้ง Main และ Side)
                var joints = unit.GetComponents<Joint>();
                foreach (var joint in joints)
                {
                    snap.connectedBodies.Add(joint.connectedBody); // null = connected to world
                }

                _snapshots.Add(snap);
            }
        }

        private void RestoreSnapshots()
        {
            // ── Phase 1: คืนตำแหน่ง, ลบ Joint เก่า, รีเซ็ตสถานะ ──
            foreach (var snap in _snapshots)
            {
                if (snap.unit == null) continue; // ถูก Destroy จริงๆ (ไม่น่าเกิด)

                // 1. เปิด GameObject กลับมา (อาจถูกปิดจาก Break)
                snap.unit.gameObject.SetActive(snap.wasActive);

                // 2. คืนตำแหน่งและการหมุน
                snap.unit.transform.position = snap.position;
                snap.unit.transform.rotation = snap.rotation;

                // 3. รีเซ็ต Rigidbody → kinematic, หยุดนิ่ง
                Rigidbody rb = snap.unit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                    }
                }

                // 4. รีเซ็ต StructuralStress (HP, isBroken, colliders)
                var stress = snap.unit.GetComponent<StructuralStress>();
                if (stress != null)
                {
                    stress.ResetFull();
                }

                // 5. ลบ Joint เก่าทิ้งให้หมด (ต้องใช้ DestroyImmediate เพื่อให้หายไปทันที ไม่ค้างในเฟรม)
                Joint[] existingJoints = snap.unit.GetComponents<Joint>();
                foreach (var j in existingJoints) DestroyImmediate(j);
            }

            // ── บังคับอัปเดตฟิสิกส์ ──
            // สำคัญมาก! หลังจากย้ายตำแหน่งกลับมาแล้ว ต้องสั่งให้ Unity อัปเดตตำแหน่ง Collider ใหม่ทันที
            // ไม่งั้นตอนสร้าง Joint ใหม่ มันจะใช้ตำแหน่งเก่าที่เพิ่งพังร่วงลงไป ทำให้เกิดบั๊กระเบิดกระจาย
            UnityEngine.Physics.SyncTransforms();

            // ── Phase 2: สร้าง Joint ใหม่ตามตำแหน่งที่ถูกต้อง ──
            foreach (var snap in _snapshots)
            {
                if (snap.unit == null) continue;

                if (snap.connectedBodies != null)
                {
                    foreach (var connectedBody in snap.connectedBodies)
                    {
                        FixedJoint newJoint = snap.unit.gameObject.AddComponent<FixedJoint>();
                        newJoint.connectedBody = connectedBody;
                    }
                }
            }

            _snapshots.Clear();

        }

        // ────────────────────────────────────────────────────────────────
        // Character System
        // ────────────────────────────────────────────────────────────────

        private void SpawnCharacters()
        {
            ClearCharacters();

            PersonSpawner[] spawners = FindObjectsByType<PersonSpawner>(FindObjectsSortMode.None);
            PersonTarget[] targets = FindObjectsByType<PersonTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (spawners.Length > 0 && targets.Length > 0)
            {
                // สำหรับแต่ละ Target ให้เกิดคนที่ Spawner อันแรกสุดแล้วเดินไปหา
                foreach (var target in targets)
                {
                    // ข้าม PersonTarget ที่ถูกลบ/ขายไปแล้ว (SetActive(false))
                    if (target == null || !target.gameObject.activeInHierarchy) continue;

                    PersonSpawner spawner = spawners[0]; 
                    
                    // ป้องกันการเกิดนอก NavMesh (เช่น จุด Spawn จมดิน หรือลอย)
                    Vector3 spawnPos = spawner.transform.position;
                    if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        spawnPos = hit.position; 
                    }
                    else
                    {
                        spawnPos += Vector3.up * 0.5f; 
                    }

                    // ── NEW: ตรวจสอบว่าเป็น NPC หรือคนธรรมดา ──
                    GameObject charObj = null;
                    Simulation.Data.NPCSkillData matchingNPC = null;
                    var structureUnit = target.GetComponentInParent<StructureUnit>();
                    
                    if (structureUnit != null && structureUnit.Data != null)
                    {
                        string targetName = structureUnit.Data.structureName;
                        if (Simulation.NPC.NPCSkillManager.Instance != null)
                        {
                            foreach (var npc in Simulation.NPC.NPCSkillManager.Instance.availableNPCs)
                            {
                                if (npc.npcName == targetName)
                                {
                                    matchingNPC = npc;
                                    break;
                                }
                            }
                        }
                    }

                    if (matchingNPC != null)
                    {
                        // Spawn NPC แทนคนธรรมดา
                        var npcController = Simulation.NPC.NPCSkillManager.Instance.SpawnNPC(matchingNPC, spawnPos);
                        if (npcController != null)
                        {
                            charObj = npcController.gameObject;
                            npcController.MoveTo(target.transform.position, target); // สั่งเดินไปที่เป้าหมาย + ซ่อน PersonTarget ตอนถึง
                        }
                    }
                    else if (personAIPrefab != null)
                    {
                        // Spawn คนธรรมดา (ถ้าหา NPC ไม่เจอ หรือเป็นด่านปกติ)
                        charObj = Instantiate(personAIPrefab, spawnPos, Quaternion.identity);
                        PersonAI ai = charObj.GetComponent<PersonAI>();
                        if (ai != null)
                        {
                            ai.InitializeAgent();
                            ai.SetTarget(target.transform);
                        }
                    }

                    if (charObj != null)
                    {
                        _spawnedCharacters.Add(charObj);
                    }
                }
            }
        }

        private void ClearCharacters()
        {
            foreach (var charObj in _spawnedCharacters)
            {
                if (charObj != null) Destroy(charObj);
            }
            _spawnedCharacters.Clear();
        }

        // ────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// รีเซ็ตประตูทั้งหมดในฉากกลับสถานะปิด (เรียกตอน StopSimulation)
        /// </summary>
        private void ResetAllDoors()
        {
            DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                door.ResetDoor();
            }
        }

        /// <summary>
        /// เปิด PersonTarget ทั้งหมดกลับมา (ที่ถูกปิดจาก StartFadeOut ตอน Simulate รอบก่อน)
        /// </summary>
        private void ResetAllPersonTargets()
        {
            PersonTarget[] targets = FindObjectsByType<PersonTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in targets)
            {
                if (t != null) t.ResetTarget();
            }
        }

        private void FreezeAllStructures()
        {
            StructureUnit[] units = FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                Rigidbody rb = unit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // แช่แข็งโครงสร้าง
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                    }
                }
            }
        }
    }
}
