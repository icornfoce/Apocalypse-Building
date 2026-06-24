using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Simulation.Building;
using Simulation.Character;
using Simulation.Physics;
using Simulation.NPC;
using Simulation.Data;

namespace Simulation.Mission
{
    /// <summary>ชนิดซอมบี้สำหรับเสกเองจากปุ่ม (โหมด sandbox)</summary>
    public enum ZombieKind { Normal, Digger, Balloon }

    /// <summary>
    /// Mission Manager — ควบคุมระบบด่าน
    /// 1. ตรวจสอบเงื่อนไขก่อนเริ่ม (จำนวนชั้น, พื้นที่, คน)
    /// 2. เรียกภัยพิบัติตามเวลาที่กำหนด
    /// 3. ประเมินผลเป็น 3 ดาว (คนรอด, ตึกไม่พัง, เงิน+)
    /// 
    /// ใช้งาน: แปะ Component นี้ที่ GameObject เดียวกับ SimulationManager
    /// หรือ GameObject ใดก็ได้ แล้ว Assign MissionData ใน Inspector
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Current Mission")]
        [SerializeField] private MissionData currentMission;

        [Header("State (Read Only)")]
        [SerializeField] private bool isMissionActive = false;
        [SerializeField] private int lastStarRating = 0;
        [SerializeField] private float simulationTimer = 0f;

        // ── Runtime State ──
        private List<DisasterBase> _activeDisasters = new List<DisasterBase>();
        private List<Coroutine> _disasterCoroutines = new List<Coroutine>();

        // Tracking สำหรับประเมินผล
        private int _initialPeopleCount;
        private int _initialStructureCount;
        private float _budgetBeforeSimulation;
        private bool _hasZombieDisaster;
        private bool _zombieSpawned;

        // ── สกิล Engineer: VFX สรุปจุดที่พังตอนจบด่าน ──
        private const float EngineerReportVFXScale = 0.6f;     // ย่อขนาด VFX ให้เล็กลง (1 = ขนาดเต็มของ prefab)
        private const float EngineerReportVFXLifetime = 5f;    // เวลาที่ VFX อยู่ก่อนหายไป (วินาที)

        // ── Events ──
        /// <summary>เรียกเมื่อ Mission เริ่ม</summary>
        public event System.Action OnMissionStarted;

        /// <summary>เรียกเมื่อ Mission ถูกหยุดกลางคัน</summary>
        public event System.Action OnMissionStopped;

        /// <summary>เรียกเมื่อ Mission จบ พร้อมจำนวนดาว</summary>
        public event System.Action<int> OnMissionCompleted;

        /// <summary>เรียกเมื่อเงื่อนไข Pre-check ไม่ผ่าน พร้อมข้อความ</summary>
        public event System.Action<string> OnValidationFailed;

        /// <summary>เรียกเมื่อภัยพิบัติเริ่ม</summary>
        public event System.Action<DisasterData> OnDisasterStarted;

        // ── Public Properties ──
        public MissionData CurrentMission => currentMission;
        public bool IsMissionActive => isMissionActive;
        public int LastStarRating => lastStarRating;
        public float SimulationTimer => simulationTimer;
        public float SimulationTimeRemaining => currentMission != null ? Mathf.Max(0, currentMission.simulationDuration - simulationTimer) : 0f;

        // ────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (currentMission != null)
            {
                SetMission(currentMission);
            }
        }

        private void Update()
        {
            if (!isMissionActive) return;

            simulationTimer += Time.deltaTime;

            // เช็คว่าหมดเวลาแล้วหรือยัง
            if (currentMission != null && simulationTimer >= currentMission.simulationDuration)
            {
                EndMission(true);
            }
            else if (isMissionActive && simulationTimer > 2f)
            {
                // 1. เช็คว่าคนตายหมดหรือยัง (ทั้ง PersonAI และ NPCController)
                bool anyAlive = false;

                var people = GameObject.FindObjectsByType<PersonAI>(FindObjectsSortMode.None);
                foreach (var p in people)
                {
                    if (p != null && !p.IsDead && p.countsTowardsPopulation)
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                {
                    var npcs = GameObject.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
                    foreach (var npc in npcs)
                    {
                        if (npc != null && !npc.IsDead && npc.countsTowardsPopulation)
                        {
                            anyAlive = true;
                            break;
                        }
                    }
                }

                // ถ้าตอนเริ่มมีคน แต่ตอนนี้ตายหมดแล้ว ให้จบด่านเลย
                if (!anyAlive && _initialPeopleCount > 0)
                {
                    Debug.Log("<color=red>☠ All people died!</color>");
                    EndMission(true);
                    return;
                }

                // 2. เช็คว่า Zombie ตายหมดแล้วหรือยัง (ถ้ามี)
                if (_hasZombieDisaster)
                {
                    var normalZombies = GameObject.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
                    var diggerZombies = GameObject.FindObjectsByType<DiggerZombieAI>(FindObjectsSortMode.None);
                    var balloonZombies = GameObject.FindObjectsByType<BalloonZombieAI>(FindObjectsSortMode.None);

                    int totalZombies = normalZombies.Length + diggerZombies.Length + balloonZombies.Length;
                    
                    if (totalZombies > 0)
                    {
                        _zombieSpawned = true; // เคยมี Zombie เกิดขึ้นแล้ว
                    }
                    else if (_zombieSpawned)
                    {
                        // Zombie เคยเกิดแล้ว แต่ตอนนี้ตายหมด → จบด่าน
                        Debug.Log("<color=green>★ All zombies eliminated!</color>");
                        EndMission(true);
                    }
                }
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// เสกซอมบี้ธรรมดา ณ ตำแหน่งที่กำหนด (ใช้ตอน NPC โดนกัดตายแล้วกลายร่าง)
        /// </summary>
        public void SpawnNormalZombie(Vector3 position)
        {
            if (currentMission != null && currentMission.disasters != null)
            {
                foreach (var entry in currentMission.disasters)
                {
                    if (entry.disasterData != null && entry.disasterData.disasterType == DisasterType.Zombie)
                    {
                        var prefab = entry.disasterData.zombiePrefab;
                        if (prefab != null)
                        {
                            GameObject zombie = Instantiate(prefab, position, Quaternion.identity);
                            zombie.name = "InfectedZombie";
                            if (zombie.GetComponent<ZombieAI>() == null)
                            {
                                zombie.AddComponent<ZombieAI>();
                            }
                            return; // พอเสกได้ตัวนึงก็จบ
                        }
                    }
                }
            }
        }

        /// <summary>
        /// เสกซอมบี้ 1 ตัวตามชนิดที่เลือก (ใช้จากปุ่มในโหมด sandbox)
        /// จะเริ่ม Simulation ให้อัตโนมัติถ้ายังไม่เริ่ม (เพื่อให้มี NavMesh) แล้วหาจุดเกิดนอกเขตก่อสร้าง
        /// </summary>
        public void SpawnZombieDirectly(GameObject prefab, ZombieKind kind)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[MissionManager] SpawnZombieDirectly: prefab is null.");
                return;
            }

            // ให้แน่ใจว่ามีการจำลองทำงานอยู่ (มี NavMesh) — เหมือน TriggerDisasterDirectly
            if (Simulation.Physics.SimulationManager.Instance != null && !Simulation.Physics.SimulationManager.Instance.IsSimulating)
            {
                Simulation.Physics.SimulationManager.Instance.StartSimulation();
            }
            if (!isMissionActive)
            {
                isMissionActive = true;
                _initialPeopleCount = CountPlacedPeople();
                _initialStructureCount = CountIntactStructures();
                _budgetBeforeSimulation = BuildingSystem.Instance != null ? BuildingSystem.Instance.CurrentBudget : 0f;
                _zombieSpawned = false;
                StructuralStress.ClearBreakHistory();
            }

            System.Type aiType = kind == ZombieKind.Digger ? typeof(DiggerZombieAI)
                               : kind == ZombieKind.Balloon ? typeof(BalloonZombieAI)
                               : typeof(ZombieAI);

            Vector3 pos = GetManualZombieSpawnPosition(kind == ZombieKind.Normal);
            GameObject zombie = Instantiate(prefab, pos, Quaternion.identity);
            zombie.name = prefab.name + "_Sandbox";

            // เผื่อ prefab ไม่ได้ติด AI มา ให้เพิ่มให้
            if (zombie.GetComponent(aiType) == null) zombie.AddComponent(aiType);

            Debug.Log($"<color=red>[Sandbox]</color> Spawned {kind} zombie at {pos}");
        }

        /// <summary>
        /// หาจุดเกิดซอมบี้นอกเขต Grid ก่อสร้าง บนพื้น Ground (และ Snap NavMesh สำหรับซอมบี้เดินดิน)
        /// </summary>
        private Vector3 GetManualZombieSpawnPosition(bool sampleNavMesh)
        {
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask == 0) groundMask = LayerMask.GetMask("Default");
            int structureMask = LayerMask.GetMask("Structure");

            float gridSize = BuildingSystem.Instance != null ? BuildingSystem.Instance.GetGridSize : 1f;
            float limitX = BuildingSystem.Instance != null ? BuildingSystem.Instance.GridColumns * gridSize * 0.5f : 10f;
            float limitZ = BuildingSystem.Instance != null ? BuildingSystem.Instance.GridRows * gridSize * 0.5f : 10f;
            float ring = Mathf.Max(limitX, limitZ);

            Vector3 found = new Vector3(limitX + 3f, 0f, limitZ + 3f); // fallback เริ่มต้น (มุมนอก Grid)

            for (int i = 0; i < 40; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero) dir = Vector2.right;
                Vector2 c = dir * Random.Range(ring + 2f, ring + 18f);
                Vector3 probe = new Vector3(c.x, 50f, c.y);

                if (UnityEngine.Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 100f, groundMask))
                {
                    Vector3 p = hit.point;
                    // ข้ามถ้าตกในเขต Grid (พื้นที่ก่อสร้าง)
                    if (p.x > -limitX && p.x < limitX && p.z > -limitZ && p.z < limitZ) continue;
                    // ข้ามถ้าทับโครงสร้าง
                    if (UnityEngine.Physics.CheckBox(p + Vector3.up, new Vector3(0.5f, 1f, 0.5f), Quaternion.identity, structureMask)) continue;
                    found = p;
                    break;
                }
            }

            if (sampleNavMesh &&
                UnityEngine.AI.NavMesh.SamplePosition(found, out UnityEngine.AI.NavMeshHit nav, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                found = nav.position;
            }
            return found;
        }

        /// <summary>
        /// สั่งให้เกิดภัยพิบัติทันทีแบบกำหนดเอง (เช่น จากปุ่ม Debug UI)
        /// </summary>
        public void TriggerDisasterDirectly(DisasterData data)
        {
            if (data == null) return;

            // ตรวจสอบว่ามีการจำลอง (Simulation) ทำงานอยู่หรือไม่
            // ถ้าไม่ทำงาน ให้เริ่มการจำลองเพื่อให้ฟิสิกส์ทำงานด้วย
            if (Simulation.Physics.SimulationManager.Instance != null && !Simulation.Physics.SimulationManager.Instance.IsSimulating)
            {
                Simulation.Physics.SimulationManager.Instance.StartSimulation();
            }

            // ถ้าไม่มีการเปิดด่าน (isMissionActive = false) ให้ตั้งเป็น true ชั่วคราวเพื่อให้ภัยพิบัติทำงานสมบูรณ์
            if (!isMissionActive)
            {
                isMissionActive = true;
                _initialPeopleCount = CountPlacedPeople();
                _initialStructureCount = CountIntactStructures();
                _budgetBeforeSimulation = BuildingSystem.Instance != null ? BuildingSystem.Instance.CurrentBudget : 0f;
                _zombieSpawned = false;
                StructuralStress.ClearBreakHistory(); // เริ่มเก็บจุดพังของรอบนี้ใหม่
            }

            DisasterBase disaster = CreateDisaster(data);
            if (disaster != null)
            {
                _activeDisasters.Add(disaster);
                OnDisasterStarted?.Invoke(data);
                Debug.Log($"<color=orange>⚠ Manual Triggered Disaster: {data.disasterName}</color>");
                disaster.Start();
            }
        }

        /// <summary>
        /// หยุดภัยพิบัติทั้งหมดที่กำลังทำงานอยู่แบบกำหนดเอง
        /// </summary>
        public void StopAllDisastersPublic()
        {
            StopAllDisasters();
            Debug.Log("<color=yellow>■ Stopped all disasters manually.</color>");
        }


        /// <summary>
        /// กำหนด Mission ที่จะเล่น
        /// </summary>
        public void SetMission(MissionData mission)
        {
            currentMission = mission;

            // ตั้ง budget ตาม mission
            if (BuildingSystem.Instance != null && mission != null)
            {
                BuildingSystem.Instance.SetBudget(mission.startingBudget);
                
                // ตั้งค่า Grid (ถ้ากำหนดมาใน MissionData)
                if (mission.gridColumns > 0 || mission.gridRows > 0)
                {
                    // เราต้องการเข้าถึง field gridColumns/gridRows ใน BuildingSystem
                    // แต่เนื่องจากมันเป็น private serialized field เราอาจจะต้องเพิ่ม public setter หรือ method
                    BuildingSystem.Instance.SetGridDimensions(mission.gridColumns, mission.gridRows);
                }
            }
        }

        /// <summary>
        /// เริ่ม Mission — จะเรียก SimulationManager.StartSimulation() ด้วย
        /// ถ้ามี Mission จะตรวจเงื่อนไขก่อน ถ้าไม่มีจะเริ่มแบบ Free-play
        /// </summary>
        public void StartMission()
        {
            if (isMissionActive) return;

            // 1. ถ้ามี Mission ให้ตรวจ Pre-check
            if (currentMission != null)
            {
                string validationError = ValidatePreConditions();
                if (validationError != null)
                {
                    Debug.LogWarning($"[MissionManager] Validation failed: {validationError}");
                    OnValidationFailed?.Invoke(validationError);
                    return;
                }
            }

            isMissionActive = true;
            simulationTimer = 0f;

            // บันทึกสถานะก่อนเริ่ม
            _initialPeopleCount = CountPlacedPeople();
            _initialStructureCount = CountIntactStructures();
            _budgetBeforeSimulation = BuildingSystem.Instance != null ? BuildingSystem.Instance.CurrentBudget : 0f;
            _zombieSpawned = false;

            // Cache ว่าด่านนี้มี Zombie ไหม
            _hasZombieDisaster = false;
            if (currentMission != null && currentMission.disasters != null)
            {
                foreach (var entry in currentMission.disasters)
                {
                    if (entry.disasterData != null && entry.disasterData.disasterType == DisasterType.Zombie)
                    {
                        _hasZombieDisaster = true;
                        break;
                    }
                }
            }

            // ล้างประวัติจุดพังของด่านก่อนหน้า เพื่อให้สกิล Engineer สรุปเฉพาะความเสียหายของด่านนี้
            StructuralStress.ClearBreakHistory();

            // เริ่ม Simulation (ฟิสิกส์ + NavMesh + NPC)
            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.StartSimulation();
            }

            // Schedule ภัยพิบัติ (ถ้ามี)
            if (currentMission != null)
            {
                ScheduleDisasters();
                Debug.Log($"<color=cyan>★ Mission Started: {currentMission.missionName}</color>");
            }
            else
            {
                Debug.Log("<color=cyan>▶ Free-play Simulation Started</color>");
            }

            OnMissionStarted?.Invoke();
        }

        /// <summary>
        /// คืนค่าสถิติปัจจุบัน (ชั้น, พื้นที่, คน) เพื่อเอาไปแสดงใน UI
        /// </summary>
        public (int floors, int area, int people) GetCurrentStats()
        {
            return (CountFloors(), CountTotalArea(), CountPlacedPeople());
        }

        /// <summary>
        /// จบ Mission — หยุด Simulation, ประเมินผล, แสดงดาว
        /// </summary>
        public void EndMission(bool isTimeUp = true)
        {
            if (!isMissionActive) return;
            isMissionActive = false;

            // หยุดภัยพิบัติทั้งหมด
            StopAllDisasters();

            if (isTimeUp)
            {
                // ประเมินผล
                lastStarRating = EvaluateResult();

                // ── เซฟดาว + ปลดล็อกด่านถัดไป (ถาวรด้วย PlayerPrefs) ──
                // ข้ามโหมด sandbox เพราะใช้ MissionData ร่วมกับ Lv.5 (กันดาว sandbox ไปปนกับด่านจริง)
                bool isSandboxScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("sandbox");
                if (!isSandboxScene && currentMission != null)
                {
                    Simulation.Core.ProgressSave.SetStars(currentMission.missionName, lastStarRating);
                    if (lastStarRating >= 1 && currentMission.nextMission != null)
                        Simulation.Core.ProgressSave.SetUnlocked(currentMission.nextMission.missionName, true);
                }

                // แสดงผล
                Debug.Log($"<color=yellow>★ Mission Complete: {currentMission.missionName} — {lastStarRating} Star(s)!</color>");

                OnMissionCompleted?.Invoke(lastStarRating);

                // ★ สกิล Engineer (passive): ถ้ามี Engineer วางอยู่ในด่าน
                // โชว์ VFX เล็กๆ ตรงจุดที่โครงสร้างพัง (สรุปความเสียหาย) — ต้องทำก่อน StopSimulation รีเซ็ตของที่พัง
                ShowEngineerDamageReport();
            }
            else
            {
                Debug.Log("<color=red>■ Mission Stopped Early</color>");
                ResetAllPersonTargets();
                OnMissionStopped?.Invoke();
            }

            // หยุด Simulation
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.StopSimulation();
            }

            ClearAllZombies();
        }

        private void ClearAllZombies()
        {
            var normalZombies = FindObjectsByType<ZombieAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var z in normalZombies) if (z != null && z.gameObject != null) Destroy(z.gameObject);

            var diggerZombies = FindObjectsByType<DiggerZombieAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var z in diggerZombies) if (z != null && z.gameObject != null) Destroy(z.gameObject);

            var balloonZombies = FindObjectsByType<BalloonZombieAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var z in balloonZombies) if (z != null && z.gameObject != null) Destroy(z.gameObject);
        }

        public void ResetAllPersonTargets()
        {
            var targets = FindObjectsByType<PersonTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in targets)
            {
                if (t != null) t.ResetTarget();
            }
        }

        // ────────────────────────────────────────────────────────────────
        // สกิล Engineer — Damage Report (VFX จุดที่พัง ตอนจบด่าน)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ★ สกิล Engineer (passive): ถ้ามี NPC Engineer วางอยู่ในด่าน
        /// พอจบด่านจะโชว์ VFX เล็กๆ ตรงทุกจุดที่โครงสร้างพังระหว่างด่าน (สรุปความเสียหาย)
        /// ใช้ช่อง skillEffectVFX ของ Engineer เป็นตัว VFX (ถ้าไม่ได้ใส่จะสร้างเอฟเฟกต์ฝุ่นเล็กๆ ในโค้ดแทน)
        /// </summary>
        private void ShowEngineerDamageReport()
        {
            // 1. หา Engineer ที่วางอยู่ในด่าน (ไม่ต้องกดใช้สกิล — แค่มีตัวอยู่ก็พอ)
            NPCController engineer = null;
            var npcs = FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc == null || npc.Data == null) continue;
                if (npc.Data.skillType == NPCSkillType.Engineer)
                {
                    engineer = npc;
                    break;
                }
            }
            if (engineer == null) return; // ไม่มี Engineer → ไม่ต้องโชว์

            // 2. ดึงจุดที่พังระหว่างด่าน
            var positions = StructuralStress.RecentBreakPositions;
            if (positions == null || positions.Count == 0) return; // ไม่มีอะไรพัง

            GameObject vfxPrefab = engineer.Data.skillEffectVFX; // ตัว VFX จากช่อง skillEffectVFX ของ Engineer
            GameObject container = new GameObject("EngineerDamageReportVFX");

            int spawned = 0;
            foreach (var pos in positions)
            {
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, pos, Quaternion.identity, container.transform);
                    vfx.transform.localScale *= EngineerReportVFXScale; // ย่อให้เล็ก
                    Destroy(vfx, EngineerReportVFXLifetime);
                }
                else
                {
                    SpawnFallbackBreakVFX(pos, container.transform); // ไม่ได้ใส่ prefab → สร้างฝุ่นเล็กๆ เอง
                }
                spawned++;
            }

            Destroy(container, EngineerReportVFXLifetime + 1f);
            Debug.Log($"<color=cyan>[Engineer]</color> Damage report: marked {spawned} broken spot(s).");

            // เก็บกวาดประวัติ เพื่อไม่ให้ค้างไปด่านถัดไป
            StructuralStress.ClearBreakHistory();
        }

        /// <summary>
        /// เอฟเฟกต์ฝุ่นเล็กๆ แบบสร้างในโค้ด (ใช้เมื่อ Engineer ไม่ได้ใส่ skillEffectVFX)
        /// </summary>
        private void SpawnFallbackBreakVFX(Vector3 position, Transform parent)
        {
            GameObject go = new GameObject("BreakSpotVFX");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.2f;
            main.startSize = 0.18f;                                   // เล็กๆ
            main.startColor = new Color(0.85f, 0.8f, 0.7f, 0.9f);     // สีฝุ่น
            main.gravityModifier = 0.3f;
            main.playOnAwake = false;
            main.maxParticles = 24;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)12) }); // พ่นทีเดียว

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 0.8f, 0.7f), 0f), new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            // กันขึ้นเป็นสีชมพู (material หาย): ใช้ shader พื้นฐานของ particle
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Shader sh = Shader.Find("Particles/Standard Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                if (sh != null) renderer.material = new Material(sh);
            }

            ps.Play();
            Destroy(go, 2f);
        }

        // ────────────────────────────────────────────────────────────────
        // Pre-Condition Validation
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ตรวจสอบว่าผู้เล่นสร้างตามเงื่อนไขครบหรือยัง
        /// คืน null ถ้าผ่าน หรือ string บอกเหตุผลถ้าไม่ผ่าน
        /// </summary>
        private string ValidatePreConditions()
        {
            if (currentMission == null) return "No mission assigned.";

            // 1. Check Floor Count
            if (currentMission.requiredFloors > 0)
            {
                int floors = CountFloors();
                if (floors < currentMission.requiredFloors)
                {
                    return $"Not enough floors! Need {currentMission.requiredFloors} (Current: {floors})";
                }
            }

            // 2. Check Total Area
            if (currentMission.requiredAreaPerFloor > 0)
            {
                int totalArea = CountTotalArea();
                if (totalArea < currentMission.requiredAreaPerFloor)
                {
                    return $"Not enough total area! Need {currentMission.requiredAreaPerFloor} m² (Current: {totalArea} m²)";
                }
            }

            // 3. Check Population
            if (currentMission.requiredPopulation > 0)
            {
                int people = CountPlacedPeople();
                if (people < currentMission.requiredPopulation)
                {
                    return $"Not enough population! Need at least {currentMission.requiredPopulation} (Current: {people})";
                }
            }

            return null; // All passed
        }

        // ────────────────────────────────────────────────────────────────
        // Counting Helpers
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// นับจำนวนชั้นจากตำแหน่ง Y ของ Structure ทั้งหมด
        /// ใช้ heightStep จาก BuildingSystem เป็นตัวแบ่งชั้น
        /// </summary>
        private int CountFloors()
        {
            StructureUnit[] units = FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            if (units.Length == 0) return 0;

            float heightStep = BuildingSystem.Instance != null ? BuildingSystem.Instance.HeightStep : 3f;
            HashSet<int> floorLevels = new HashSet<int>();

            foreach (var unit in units)
            {
                if (unit == null || !unit.gameObject.activeSelf) continue;
                if (unit.Data == null) continue;

                // กรอง: นับเฉพาะ Floor หรือ Normal (เสา) เป็นตัวระบุชั้น
                // ไม่นับกำแพงหรือประตูเป็นชั้น เพื่อไม่ให้ใช้กำแพงแทนเสาในการผ่านเงื่อนไขด่าน
                if (unit.Data.structureType != Simulation.Data.StructureType.Floor && 
                    unit.Data.structureType != Simulation.Data.StructureType.Normal) continue;
                
                float y = unit.transform.position.y;
                // ถ้า Pivot อยู่ตรงกลาง ให้ขยับลงมาที่ฐานก่อนคำนวณชั้น
                if (unit.Data.pivotAtCenter)
                {
                    y -= unit.Data.size.y * 0.5f;
                }

                // ใช้ FloorToInt + epsilon เพื่อเลี่ยงปัญหา Bankers Rounding ที่ทำให้ชั้นกระโดด
                int floor = Mathf.FloorToInt(y / heightStep + 0.1f) + 1;
                floorLevels.Add(floor);
            }

            return floorLevels.Count;
        }

        /// <summary>
        /// นับพื้นที่รวมของตึกทั้งหมด (ทุกชั้น)
        /// คืนค่าเป็น ตารางเมตร (m²) โดยคิดจาก (จำนวนช่อง Grid * gridSize^2)
        /// </summary>
        private int CountTotalArea()
        {
            StructureUnit[] units = FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            if (units.Length == 0) return 0;

            float heightStep = BuildingSystem.Instance != null ? BuildingSystem.Instance.HeightStep : 3f;
            float gridSize = BuildingSystem.Instance != null ? BuildingSystem.Instance.GetGridSize : 1f;

            // นับพื้นที่ต่อชั้น
            Dictionary<int, HashSet<Vector2Int>> floorAreas = new Dictionary<int, HashSet<Vector2Int>>();

            foreach (var unit in units)
            {
                if (unit == null || !unit.gameObject.activeSelf) continue;
                if (unit.Data == null) continue;
                // นับเฉพาะ Floor structures
                if (unit.Data.structureType != Simulation.Data.StructureType.Floor) continue;

                float y = unit.transform.position.y;
                if (unit.Data.pivotAtCenter)
                {
                    y -= unit.Data.size.y * 0.5f;
                }

                int floor = Mathf.FloorToInt(y / heightStep + 0.1f) + 1;
                if (!floorAreas.ContainsKey(floor))
                {
                    floorAreas[floor] = new HashSet<Vector2Int>();
                }

                // คำนวณช่อง Grid ที่ Structure นี้ครอบครอง (ตาม size)
                int sizeX = Mathf.Max(1, Mathf.RoundToInt(unit.Data.size.x));
                int sizeZ = Mathf.Max(1, Mathf.RoundToInt(unit.Data.size.z));

                // คำนวณตำแหน่ง Grid เริ่มต้น (มุมซ้ายล่างของโครงสร้าง)
                // เนื่องจากตำแหน่งของ Structure คือจุดศูนย์กลาง (SnapToCellCenter)
                // เราต้องถอยกลับไปที่ขอบซ้ายล่างเพื่อเริ่มนับช่อง Grid
                float worldX = unit.transform.position.x;
                float worldZ = unit.transform.position.z;

                float spanX = sizeX * gridSize;
                float spanZ = sizeZ * gridSize;

                // หาขอบซ้ายล่างในหน่วยโลก แล้วแปลงเป็น Grid Index
                int startGridX = Mathf.RoundToInt((worldX - spanX * 0.5f) / gridSize);
                int startGridZ = Mathf.RoundToInt((worldZ - spanZ * 0.5f) / gridSize);

                for (int dx = 0; dx < sizeX; dx++)
                {
                    for (int dz = 0; dz < sizeZ; dz++)
                    {
                        floorAreas[floor].Add(new Vector2Int(startGridX + dx, startGridZ + dz));
                    }
                }
            }

            // รวมพื้นที่จากทุกชั้น
            int totalCells = 0;
            foreach (var kvp in floorAreas)
            {
                totalCells += kvp.Value.Count;
            }

            // คำนวณเป็นตารางเมตร: x m² = จำนวนช่อง * (gridSize * gridSize)
            float areaPerCell = gridSize * gridSize;
            return Mathf.RoundToInt(totalCells * areaPerCell);
        }

        /// <summary>
        /// นับจำนวน PersonTarget ที่ผู้เล่นวางไว้ในฉาก
        /// </summary>
        private int CountPlacedPeople()
        {
            PersonTarget[] targets = FindObjectsByType<PersonTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var t in targets)
            {
                if (t == null || t.gameObject.name.Contains("Ghost")) continue;
                if (t.countsTowardsPopulation) count++;
            }
            return count;
        }

        /// <summary>
        /// นับจำนวนคนที่ยังมีชีวิตอยู่ (PersonAI + NPCController ที่ยังอยู่ในฉาก)
        /// </summary>
        private int CountAlivePeople()
        {
            int alive = 0;

            PersonAI[] people = FindObjectsByType<PersonAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in people)
            {
                if (p == null || p.gameObject.name.Contains("Ghost")) continue;
                if (p.countsTowardsPopulation) alive++;
            }

            NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc == null || npc.gameObject.name.Contains("Ghost")) continue;
                if (npc.countsTowardsPopulation && !npc.IsDead) alive++;
            }

            return alive;
        }

        /// <summary>
        /// นับจำนวน Structure ที่ยังไม่พัง
        /// </summary>
        private int CountIntactStructures()
        {
            StructureUnit[] units = FindObjectsByType<StructureUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int intact = 0;
            foreach (var unit in units)
            {
                if (unit == null || unit.gameObject.name.Contains("Ghost")) continue;
                if (!unit.gameObject.activeSelf) continue;

                var stress = unit.GetComponent<StructuralStress>();
                if (stress == null || !stress.IsBroken)
                {
                    intact++;
                }
            }
            return intact;
        }

        // ────────────────────────────────────────────────────────────────
        // Disaster Scheduling
        // ────────────────────────────────────────────────────────────────

        private void ScheduleDisasters()
        {
            _activeDisasters.Clear();
            _disasterCoroutines.Clear();

            if (currentMission.disasters == null) return;

            foreach (var entry in currentMission.disasters)
            {
                if (entry.disasterData == null) continue;
                Coroutine co = StartCoroutine(DisasterScheduleCoroutine(entry));
                _disasterCoroutines.Add(co);
            }
        }

        private IEnumerator DisasterScheduleCoroutine(DisasterEntry entry)
        {
            // รอจนถึงเวลาเริ่ม
            yield return new WaitForSeconds(entry.startTime);

            if (!isMissionActive) yield break;

            // สร้าง Disaster ตาม Type
            DisasterBase disaster = CreateDisaster(entry.disasterData);
            if (disaster == null) yield break;

            _activeDisasters.Add(disaster);
            OnDisasterStarted?.Invoke(entry.disasterData);

            Debug.Log($"<color=red>⚠ Disaster: {entry.disasterData.disasterName}</color>");

            // เริ่มภัยพิบัติ
            disaster.Start();
        }

        private DisasterBase CreateDisaster(DisasterData data)
        {
            switch (data.disasterType)
            {
                case DisasterType.Earthquake:     return new EarthquakeDisaster(data, this);
                case DisasterType.Flood:          return new FloodDisaster(data, this);
                case DisasterType.StrongWind:     return new StrongWindDisaster(data, this);
                case DisasterType.UFOAbduction:   return new UFOAbductionDisaster(data, this);
                case DisasterType.DragonFire:     return new DragonFireDisaster(data, this);
                case DisasterType.AcidRain:       return new AcidRainDisaster(data, this);
                case DisasterType.Tornado:        return new TornadoDisaster(data, this);
                case DisasterType.Zombie:         return new ZombieDisaster(data, this);
                default:
                    Debug.LogWarning($"[MissionManager] Unknown disaster type: {data.disasterType}");
                    return null;
            }
        }

        private void StopAllDisasters()
        {
            // หยุด Coroutine ทั้งหมด
            foreach (var co in _disasterCoroutines)
            {
                if (co != null) StopCoroutine(co);
            }
            _disasterCoroutines.Clear();

            // หยุด Disaster ที่กำลังทำงาน
            foreach (var disaster in _activeDisasters)
            {
                if (disaster.IsRunning) disaster.Stop();
            }
            _activeDisasters.Clear();
        }

        // ────────────────────────────────────────────────────────────────
        // Star Rating Evaluation
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluate Mission result → 0-3 stars
        /// ★ Star 1: At least 1 survivor
        /// ★ Star 2: Budget is not negative
        /// ★ Star 3: All survivors survive
        /// </summary>
        private int EvaluateResult()
        {
            int stars = 0;

            int aliveCount = CountAlivePeople();
            int placedCount = _initialPeopleCount;

            // ★ Star 1: At least 1 survivor
            if (aliveCount >= 1)
            {
                stars++;
                Debug.Log("  ★ Star 1: At least one person survived! ✓");
            }
            else
            {
                Debug.Log("  ☆ Star 1: FAILED (Everyone died) - 0 stars.");
                return 0; // ต้องมีคนรอดอย่างน้อย 1 คนถึงจะได้ดาว
            }

            // ★ Star 2: Budget is not negative
            float currentBudget = BuildingSystem.Instance != null ? BuildingSystem.Instance.CurrentBudget : 0f;
            if (currentBudget >= 0f)
            {
                stars++;
                Debug.Log($"  ★ Star 2: Budget is positive ({currentBudget:F0}) ✓");
            }
            else
            {
                Debug.Log($"  ☆ Star 2: Budget is negative ({currentBudget:F0}) ✗");
            }

            // ★ Star 3: Everyone survived
            if (aliveCount >= placedCount && placedCount > 0)
            {
                stars++;
                Debug.Log("  ★ Star 3: Everyone survived! ✓");
            }
            else
            {
                Debug.Log($"  ☆ Star 3: Some people died ({aliveCount}/{placedCount}) ✗");
            }

            return stars;
        }

        // ────────────────────────────────────────────────────────────────
        // Debug / Inspector
        // ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Debug: Validate Pre-Conditions")]
        private void DebugValidate()
        {
            string result = ValidatePreConditions();
            if (result == null)
                Debug.Log("<color=green>[MissionManager] All pre-conditions passed! ✓</color>");
            else
                Debug.LogWarning($"[MissionManager] {result}");
        }

        [ContextMenu("Debug: Count Stats")]
        private void DebugCountStats()
        {
            Debug.Log($"Floors: {CountFloors()}, Total Area: {CountTotalArea()} m², People: {CountPlacedPeople()}, Alive: {CountAlivePeople()}, Structures: {CountIntactStructures()}");
        }
#endif
    }
}
