using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Building;
using Simulation.Character;
using Simulation.Physics;
using Simulation.Mission;

namespace Simulation.NPC
{
    /// <summary>
    /// NPCController — แปะที่ตัว NPC แต่ละตัว
    /// จัดการ: เลือด, เดิน, สกิล, VFX/SFX, Animation, หนีซอมบี้, โดนของทับ
    /// สกิลทั้ง 6:
    ///   1. Engineer  — Toggle Stress Visualization
    ///   2. Builder   — เดินไปซ่อมโครงสร้าง
    ///   3. Economist — Auto ลดราคา 10% (จัดการที่ NPCSkillManager)
    ///   4. Architect — Auto Buff ออร่า (จัดการที่ NPCSkillManager)
    ///   5. Politician— Auto ยกเลิกภาษี (จัดการที่ NPCSkillManager)
    ///   6. Commander — เรียกทหารมายิง
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class NPCController : MonoBehaviour
    {
        [Header("Data (Runtime)")]
        [SerializeField] private NPCSkillData _data;

        [Header("State")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private bool _isDead = false;
        [SerializeField] private bool _isSelected = false;
        [SerializeField] private bool _skillActive = false;

        [Header("Mission Settings")]
        [Tooltip("ถ้านับเป็นคนในภารกิจ จะถูกนำไปคำนวณจำนวนคนรอดและดาว")]
        public bool countsTowardsPopulation = true;

        [Header("Flee Behavior")]
        [Tooltip("ระยะที่ NPC มองเห็นซอมบี้แล้วจะเริ่มวิ่งหนี")]
        public float detectionRange = 7f;
        [Tooltip("ความเร็วตอนวิ่งหนี")]
        public float fleeSpeed = 4f;
        [Tooltip("VFX ที่จะขึ้นบนหัวตอนตกใจหนี")]
        public GameObject panicVFXPrefab;

        [Header("Crush Damage")]
        [Tooltip("ความแรงจากการชนขั้นต่ำที่จะทำให้ลดเลือด")]
        public float damageImpactThreshold = 3f;

        // Components
        private NavMeshAgent _agent;
        private Animator _animator;
        private AudioSource _audioSource;
        private Rigidbody _rb;

        // รัศมีลำตัวให้ตรงกับที่ NavMesh ถูก Bake (agentRadius = 0.2)
        // กันไม่ให้ Collider จริง (เดิม 0.5) โผล่ทะลุ/ดันกำแพงจนพังตอนเดิน
        private const float NavBodyRadius = 0.2f;

        // Skill Runtime
        private float _skillCooldownTimer = 0f;
        private bool _isRepairing = false;
        private StructuralStress _repairTarget;
#pragma warning disable CS0414
        private bool _stressVisualsOn = false;
#pragma warning restore CS0414
        private Coroutine _soldierCoroutine;
        private List<GameObject> _spawnedSoldiers = new List<GameObject>();

        // Flee Runtime
        private GameObject _activePanicVFX;
        private float _panicTimer;
        private Vector3 _fleeDirection;
        private bool _hasManualDestination = false;
        private Vector3 _manualDestination;
        private PersonTarget _personTarget;
        private bool _hasReachedPersonTarget = false;

        // ── Public Properties ──
        public NPCSkillData Data => _data;
        public bool IsDead => _isDead;
        public bool IsSelected => _isSelected;
        public bool SkillActive => _skillActive;
        public float CurrentHealth => _currentHealth;
        public float HealthRatio => _data != null && _data.maxHealth > 0 ? Mathf.Clamp01(_currentHealth / _data.maxHealth) : 0f;
        public bool IsFleeing => _panicTimer > 0;

        // ────────────────────────────────────────────────────────────────
        // Initialization
        // ────────────────────────────────────────────────────────────────

        public void Initialize(NPCSkillData data)
        {
            _data = data;
            _currentHealth = data.maxHealth;

            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.speed = data.moveSpeed;
            _agent.stoppingDistance = 0.5f;
            // รัศมีต้องตรงกับ NavMesh bake (0.2) ไม่งั้น Agent จะเดินจ่อกำแพงในระยะที่ตัว Collider จริงทะลุเข้าไป
            _agent.radius = NavBodyRadius;
            _agent.height = 1.8f;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            // ตรวจสอบ Collider ให้เป็น Trigger (เพื่อตัดแรงฟิสิกส์ทั้งหมด ไม่ให้ดันกำแพง/ประตูพัง)
            // และย่อรัศมีให้พอดีกับ NavMesh
            CapsuleCollider[] capsules = GetComponentsInChildren<CapsuleCollider>(true);
            foreach (var cap in capsules)
            {
                if (cap != null)
                {
                    cap.isTrigger = true;
                    cap.radius = NavBodyRadius;
                }
            }

            // ContinuousSpeculative กันการ "พุ่งทะลุ" กำแพง/บานประตูบางๆ (Discrete เดิมทำให้ลอดผ่านได้)
            if (_rb != null) _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            _animator = GetComponentInChildren<Animator>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.playOnAwake = false;

            // เช็คว่าอยู่บน NavMesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                _agent.enabled = true;
                _rb.isKinematic = true;


            }
            else
            {
                _agent.enabled = false;
                _rb.isKinematic = false;

            }
        }

        // ────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void Update()
        {
            if (_isDead) return;

            // Cooldown
            if (_skillCooldownTimer > 0f) _skillCooldownTimer -= Time.deltaTime;

            // Builder: ซ่อมอัตโนมัติเมื่อถึงเป้าหมาย
            if (_isRepairing && _repairTarget != null)
            {
                HandleRepairTick();
            }

            // ── Floor check: ถ้าไม่มีพื้นรองรับ ให้ตก ──
            int floorMask = LayerMask.GetMask("Ground", "Structure");
            float rayDist = 0.8f;
            bool hasFloor = UnityEngine.Physics.Raycast(
                transform.position + Vector3.up * 0.1f, Vector3.down,
                out RaycastHit floorHit, rayDist, floorMask, QueryTriggerInteraction.Collide
            );

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && hasFloor)
            {
                if (_hasManualDestination && !_agent.pathPending)
                {
                    if (_agent.remainingDistance <= _agent.stoppingDistance)
                    {
                        _hasManualDestination = false;

                        // ถึง PersonTarget แล้ว → ซ่อน Parent StructureUnit หรือตัวมันเอง
                        if (!_hasReachedPersonTarget && _personTarget != null)
                        {
                            _hasReachedPersonTarget = true;
                            StructureUnit unit = _personTarget.GetComponent<StructureUnit>();
                            if (unit == null) unit = _personTarget.GetComponentInParent<StructureUnit>();
                            if (unit != null)
                            {
                                unit.gameObject.SetActive(false);
                            }
                            else
                            {
                                _personTarget.gameObject.SetActive(false);
                            }
                            _personTarget = null;
                        }
                    }
                }

                // ── ระบบตรวจจับซอมบี้และวิ่งหนี ──
                HandleFleeBehavior();

                if (IsFleeing && !_hasManualDestination)
                {
                    _agent.isStopped = false;
                    _agent.speed = fleeSpeed;

                    Vector3 fleePos = transform.position + _fleeDirection * 5f;
                    if (NavMesh.SamplePosition(fleePos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        _agent.SetDestination(hit.position);
                    }
                }
                else
                {
                    // คืนค่า speed ปกติเมื่อไม่หนี (หรือตอนที่มี manual destination)
                    if (_data != null) _agent.speed = _data.moveSpeed;
                }
            }
            else if (_agent != null && _agent.enabled && (!_agent.isOnNavMesh || !hasFloor))
            {
                // ร่วงหรือไม่มีพื้น — ปิด Agent ให้ตกลงมา
                StopFleeing();
                _agent.enabled = false;
                if (_rb != null) _rb.isKinematic = false;
            }

            // ป้องกันโครงสร้างข้างๆ พังจากการเบียดชนของตัวละคร
            var nearbyStructures = UnityEngine.Physics.OverlapSphere(transform.position, 1.2f, LayerMask.GetMask("Structure"));
            foreach (var hitCol in nearbyStructures)
            {
                if (hitCol != null)
                {
                    var stress = hitCol.GetComponentInParent<StructuralStress>();
                    if (stress != null)
                    {
                        stress.NotifyCharacterContact();
                    }
                }
            }

            // Animation: เดิน
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            bool isMoving = _agent != null && _agent.enabled && _agent.hasPath
                            && _agent.remainingDistance > _agent.stoppingDistance
                            && _agent.velocity.sqrMagnitude > 0.01f;

            _animator.SetBool("IsMoving", isMoving);
        }

        // ────────────────────────────────────────────────────────────────
        // Flee from Zombie
        // ────────────────────────────────────────────────────────────────

        private void HandleFleeBehavior()
        {
            // ค้นหาซอมบี้ใกล้ๆ
            ZombieAI nearbyZombie = FindVisibleZombie();

            if (nearbyZombie != null)
            {
                // พบซอมบี้! เริ่มตกใจและวิ่งหนี
                _panicTimer = 3f;
                _fleeDirection = (transform.position - nearbyZombie.transform.position).normalized;
                _fleeDirection.y = 0;

                ShowPanicVFX(true);
            }
            else
            {
                // ไม่พบซอมบี้ ค่อยๆ ลดเวลาตกใจ
                if (_panicTimer > 0)
                {
                    _panicTimer -= Time.deltaTime;
                    if (_panicTimer <= 0)
                    {
                        StopFleeing();
                    }
                }
            }
        }

        private ZombieAI FindVisibleZombie()
        {
            if (detectionRange <= 0) return null;

            // ตรวจสอบในรัศมีรอบตัว
            Collider[] hits = UnityEngine.Physics.OverlapSphere(transform.position, detectionRange);

            foreach (var hit in hits)
            {
                // ข้ามถ้าเป็นตัวเอง
                if (hit.transform.root == transform.root) continue;

                var zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie != null && !zombie.IsDead)
                {
                    // เช็ค LOS
                    Vector3 start = transform.position + Vector3.up * 1.0f;
                    Vector3 end = hit.bounds.center;
                    Vector3 dir = (end - start).normalized;
                    float dist = Vector3.Distance(start, end);

                    // ถ้าเป้าหมายอยู่ใกล้มาก ให้ถือว่าเห็นเลย
                    if (dist < 0.5f) return zombie;

                    RaycastHit[] allHits = UnityEngine.Physics.RaycastAll(start, dir, dist + 0.5f, ~0, QueryTriggerInteraction.Collide);
                    System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

                    bool foundZombie = false;
                    foreach (var hitInfo in allHits)
                    {
                        if (hitInfo.collider.transform.root == transform.root) continue;

                        if (hitInfo.collider.gameObject == zombie.gameObject || hitInfo.collider.transform.IsChildOf(zombie.transform))
                        {
                            foundZombie = true;
                            break;
                        }
                        else
                        {
                            return null; // ถูกบัง
                        }
                    }

                    if (foundZombie) return zombie;

                    // ถ้า Raycast ไม่โดนอะไรเลย แต่ OverlapSphere เจอ
                    if (allHits.Length == 0)
                    {
                        return zombie;
                    }
                }
            }
            return null;
        }

        private void ShowPanicVFX(bool show)
        {
            if (show)
            {
                if (_activePanicVFX == null && panicVFXPrefab != null)
                {
                    _activePanicVFX = Instantiate(panicVFXPrefab, transform);
                    _activePanicVFX.transform.localPosition = Vector3.up * 2.2f;
                }
            }
            else
            {
                if (_activePanicVFX != null)
                {
                    Destroy(_activePanicVFX);
                    _activePanicVFX = null;
                }
            }
        }

        private void StopFleeing()
        {
            _panicTimer = 0;
            ShowPanicVFX(false);
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Selection
        // ────────────────────────────────────────────────────────────────

        public void OnSelected()
        {
            _isSelected = true;
        }

        public void OnDeselected()
        {
            _isSelected = false;
        }

        // ────────────────────────────────────────────────────────────────
        // Movement
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// สั่ง NPC เดินไปที่จุดที่กำหนด (NavMesh)
        /// </summary>
        public void MoveTo(Vector3 worldPosition)
        {
            MoveTo(worldPosition, null);
        }

        /// <summary>
        /// สั่ง NPC เดินไปที่จุดที่กำหนด พร้อมเก็บ PersonTarget ไว้ซ่อนตอนถึง
        /// </summary>
        public void MoveTo(Vector3 worldPosition, PersonTarget personTarget)
        {
            if (_isDead || _agent == null || !_agent.enabled) return;

            // ยกเลิกการซ่อมถ้ากำลังทำอยู่
            if (_isRepairing) StopRepairing();

            // หากมี PersonTarget เดิมอยู่แล้ว แต่ถูกสั่งให้เดินไปที่อื่น (personTarget ใหม่เป็น null หรือคนละอัน)
            // ให้ซ่อน/ปิดการทำงานของ Parent StructureUnit หรือตัวมันเองทันที
            if (_personTarget != null && _personTarget != personTarget)
            {
                StructureUnit unit = _personTarget.GetComponent<StructureUnit>();
                if (unit == null) unit = _personTarget.GetComponentInParent<StructureUnit>();
                if (unit != null)
                {
                    unit.gameObject.SetActive(false);
                }
                else
                {
                    _personTarget.gameObject.SetActive(false);
                }
            }

            // เก็บ PersonTarget ไว้เพื่อซ่อนตอนถึง
            _personTarget = personTarget;
            _hasReachedPersonTarget = false;

            // หาจุดที่ใกล้ที่สุดบน NavMesh
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                Vector3 targetPos = hit.position;

                // ตรวจสอบความสมบูรณ์ของเส้นทาง หากเส้นทางบางส่วนขาด (PathPartial) ให้เดินไปเท่าที่เดินได้ (จุดหักมุมสุดท้าย)
                NavMeshPath path = new NavMeshPath();
                if (_agent.isOnNavMesh && _agent.CalculatePath(targetPos, path))
                {
                    if (path.status == NavMeshPathStatus.PathPartial && path.corners.Length > 0)
                    {
                        targetPos = path.corners[path.corners.Length - 1];
                    }
                }

                _agent.isStopped = false;
                _agent.SetDestination(targetPos);
                _hasManualDestination = true;
                _manualDestination = targetPos;

                // SFX เดิน
                if (_data != null && _data.walkSFX != null)
                {
                    _audioSource.clip = _data.walkSFX;
                    _audioSource.loop = true;
                    _audioSource.Play();
                }
            }

            Debug.Log($"<color=cyan>[NPC]</color> {name} moving to {worldPosition}");
        }

        // ────────────────────────────────────────────────────────────────
        // Skill System
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// เปิดใช้สกิลของ NPC นี้
        /// </summary>
        public void ActivateSkill()
        {
            if (_isDead || _data == null) return;
            if (_skillCooldownTimer > 0f)
            {
                Debug.Log($"<color=yellow>[NPC]</color> Skill on cooldown: {_skillCooldownTimer:F1}s");
                return;
            }

            // VFX/SFX ตอนเปิดสกิล
            PlaySkillActivateEffects();

            switch (_data.skillType)
            {
                case NPCSkillType.Engineer:
                    SkillEngineer();
                    break;
                case NPCSkillType.Builder:
                    SkillBuilder();
                    break;
                case NPCSkillType.Economist:
                    // Auto (Passive) — แค่แจ้ง
                    Debug.Log("<color=green>[Economist]</color> Passive skill active: 10% discount on all building costs.");
                    break;
                case NPCSkillType.Architect:
                    // Auto (Passive) — แค่แจ้ง
                    Debug.Log("<color=magenta>[Architect]</color> Passive skill: checking conditions for 20% damage reflection buff.");
                    break;
                case NPCSkillType.Politician:
                    // Auto (Passive) — แค่แจ้ง
                    Debug.Log("<color=blue>[Politician]</color> Passive skill active: tax removed!");
                    break;
                case NPCSkillType.Commander:
                    SkillCommander();
                    break;
            }

            // ตั้ง Cooldown
            if (_data.skillCooldown > 0f)
                _skillCooldownTimer = _data.skillCooldown;
        }

        // ── 1. วิศวกร: รายงานความเสียหาย (Passive) ──
        // ผลของสกิลเป็นแบบ Passive: ถ้ามี Engineer "วางอยู่" ในด่าน พอจบด่านจะมี VFX เล็กๆ
        // ขึ้นตรงทุกจุดที่โครงสร้างพัง (สรุปความเสียหาย) — จัดการจริงที่ MissionManager.ShowEngineerDamageReport()
        // (การแสดงค่า Stress ระหว่างเล่นยังเปิด/ปิดได้จากปุ่ม UI ของ BuildUIController ตามเดิม)
        private void SkillEngineer()
        {
            // Passive: ไม่มีผลตอนกด — แค่เล่นอนิเมชัน ส่วนผลจริงไปโชว์ตอนจบด่าน
            if (_animator != null) _animator.SetTrigger("UseSkill");
            Debug.Log("<color=green>[Engineer]</color> Passive: broken-spot damage report will appear at mission end.");
        }

        // ── 2. ช่างก่อสร้าง: เลือกซ่อมโครงสร้าง ──

        private void SkillBuilder()
        {
            // เข้าโหมดเลือกเป้าหมายซ่อม
            StartCoroutine(BuilderSelectTarget());
        }

        private IEnumerator BuilderSelectTarget()
        {
            Debug.Log("<color=orange>[Builder]</color> Click on a damaged structure to repair...");
            _skillActive = true;

            // รอผู้เล่นคลิกเป้าหมาย
            while (_skillActive)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // ตรวจสอบว่าไม่ได้กดบน UI
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        yield return null;
                        continue;
                    }

                    Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        var stress = hit.collider.GetComponentInParent<StructuralStress>();
                        if (stress != null && stress.HealthRatio < 1f)
                        {
                            // เจอเป้าหมาย! เดินไปซ่อม
                            _repairTarget = stress;
                            _isRepairing = true;
                            MoveTo(stress.transform.position);

                            // VFX ที่เป้าหมาย
                            if (_data.skillEffectVFX != null)
                                Instantiate(_data.skillEffectVFX, stress.transform.position, Quaternion.identity);

                            Debug.Log($"<color=orange>[Builder]</color> Repairing: {stress.name}");
                            _skillActive = false;
                            yield break;
                        }
                    }
                }

                // Right Click หรือ Escape = ยกเลิก
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    _skillActive = false;
                    Debug.Log("<color=orange>[Builder]</color> Repair cancelled.");
                    yield break;
                }

                yield return null;
            }
        }

        private void HandleRepairTick()
        {
            if (_repairTarget == null || _repairTarget.IsBroken)
            {
                StopRepairing();
                return;
            }

            // เช็คว่าถึงเป้าหมายแล้ว
            float dist = Vector3.Distance(transform.position, _repairTarget.transform.position);
            if (dist <= 2.5f)
            {
                // หยุดเดิน
                if (_agent != null && _agent.enabled) _agent.isStopped = true;

                // หันหน้าไปทางเป้าหมาย
                Vector3 lookDir = (_repairTarget.transform.position - transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

                // Animation ซ่อม
                if (_animator != null) _animator.SetBool("IsRepairing", true);

                // ซ่อม HP
                float repairAmount = (_data != null ? _data.repairPerSecond : 20f) * Time.deltaTime;

                if (_repairTarget.HealthRatio < 1f)
                {
                    _repairTarget.RepairFull();

                    // SFX ซ่อม
                    if (_data != null && _data.skillEffectSFX != null && !_audioSource.isPlaying)
                    {
                        _audioSource.PlayOneShot(_data.skillEffectSFX);
                    }
                }

                // เสร็จแล้ว!
                if (_repairTarget.HealthRatio >= 1f)
                {
                    Debug.Log($"<color=green>[Builder]</color> Repair complete: {_repairTarget.name}");
                    StopRepairing();
                }
            }
        }

        private void StopRepairing()
        {
            _isRepairing = false;
            _repairTarget = null;
            if (_animator != null) _animator.SetBool("IsRepairing", false);
        }

        // ── 6. ทหาร: เรียกทหารมายิง ──

        private void SkillCommander()
        {
            if (_soldierCoroutine != null)
            {
                // ถ้าทหารกำลังยิงอยู่ ให้หยุด
                StopCoroutine(_soldierCoroutine);
                DespawnSoldiers();
                _skillActive = false;
                Debug.Log("<color=red>[Commander]</color> Soldiers recalled.");
                return;
            }

            _skillActive = true;
            _soldierCoroutine = StartCoroutine(CommanderFireSupport());
        }

        private IEnumerator CommanderFireSupport()
        {
            if (_data == null) yield break;

            float range = _data.soldierRange;
            float damage = _data.soldierDamage;
            float fireRate = _data.soldierFireRate;
            int count = _data.soldierCount;
            float duration = _data.skillDuration > 0 ? _data.skillDuration : 10f;

            // Animation
            if (_animator != null) _animator.SetTrigger("UseSkill");

            Debug.Log($"<color=red>[Commander]</color> Calling {count} soldiers for {duration}s!");

            // Spawn ทหาร (Procedural cubes ที่ยิงได้)
            SpawnSoldiers(count);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += fireRate;
                yield return new WaitForSeconds(fireRate);

                // หาซอมบี้ในระยะ
                var zombies = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
                foreach (var zombie in zombies)
                {
                    if (zombie == null || zombie.IsDead) continue;
                    float dist = Vector3.Distance(transform.position, zombie.transform.position);
                    if (dist <= range)
                    {
                        // ยิง!
                        zombie.TakeDamage(damage);

                        // VFX: เส้นยิง (Tracer) จากทหารไปซอมบี้
                        foreach (var soldier in _spawnedSoldiers)
                        {
                            if (soldier != null)
                            {
                                CreateBulletTracer(soldier.transform.position + Vector3.up * 1.5f, zombie.transform.position + Vector3.up * 0.5f);
                            }
                        }

                        // SFX ยิงปืน
                        if (_data.skillEffectSFX != null)
                        {
                            float vol = Simulation.UI.GameSettings.LoadSFXVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                            AudioSource.PlayClipAtPoint(_data.skillEffectSFX, transform.position, vol);
                        }

                        break; // ยิงทีละตัว
                    }
                }
            }

            DespawnSoldiers();
            _skillActive = false;
            _soldierCoroutine = null;
            Debug.Log("<color=red>[Commander]</color> Fire support ended.");
        }

        private void SpawnSoldiers(int count)
        {
            DespawnSoldiers();

            for (int i = 0; i < count; i++)
            {
                // วางทหารรอบตัว Commander
                float angle = (float)i / count * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 2f, 0f, Mathf.Sin(angle) * 2f);
                Vector3 pos = transform.position + offset;

                // Snap to NavMesh
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }

                // สร้างทหาร (Procedural Capsule)
                GameObject soldier = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                soldier.name = $"Soldier_{i + 1}";
                soldier.transform.position = pos;
                soldier.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

                // สี
                var renderer = soldier.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.2f, 0.35f, 0.2f); // สีเขียวทหาร
                    renderer.material = mat;
                }

                // ลบ Collider ไม่ให้ขวาง
                var col = soldier.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _spawnedSoldiers.Add(soldier);
            }
        }

        private void DespawnSoldiers()
        {
            foreach (var s in _spawnedSoldiers)
            {
                if (s != null) Destroy(s);
            }
            _spawnedSoldiers.Clear();
        }

        private void CreateBulletTracer(Vector3 start, Vector3 end)
        {
            GameObject tracer = new GameObject("BulletTracer");
            LineRenderer lr = tracer.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.9f, 0.2f, 0.9f); // สีเหลืองทอง
            lr.material = mat;

            Destroy(tracer, 0.15f);
        }

        // ────────────────────────────────────────────────────────────────
        // Effects
        // ────────────────────────────────────────────────────────────────

        private void PlaySkillActivateEffects()
        {
            if (_data == null) return;

            // VFX
            if (_data.skillActivateVFX != null)
            {
                Instantiate(_data.skillActivateVFX, transform.position + Vector3.up, Quaternion.identity);
            }

            // SFX
            if (_data.skillActivateSFX != null)
            {
                float vol = Simulation.UI.GameSettings.LoadSFXVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                AudioSource.PlayClipAtPoint(_data.skillActivateSFX, transform.position, vol);
            }

            // Animation
            if (_animator != null) _animator.SetTrigger("UseSkill");
        }

        // ────────────────────────────────────────────────────────────────
        // Collision / Crush Damage
        // ────────────────────────────────────────────────────────────────




        /// <summary>ความเร็วขั้นต่ำที่ถือว่าวัตถุ "กำลังตก/ปลิว" มาทับ (ต่ำกว่านี้ = ของตั้งนิ่ง)</summary>
        private const float FallingDebrisSpeed = 2.5f;

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;

            // ตอบสนองเฉพาะของที่ตก/ปลิวมาทับจริง (เพดานถล่ม ฯลฯ)
            Rigidbody otherRb = other.attachedRigidbody;
            if (otherRb == null || otherRb.isKinematic) return;
            float fallSpeed = otherRb.linearVelocity.magnitude;
            if (fallSpeed < FallingDebrisSpeed) return;

            float massFactor = Mathf.Clamp(otherRb.mass, 1f, 500f);
            ApplyBounce(otherRb, fallSpeed);
            TakeDamage(fallSpeed * massFactor * 2f);
        }

        /// <summary>
        /// ทำให้วัตถุที่ตกลงมาโดนกระเด้งออกไป
        /// </summary>
        private void ApplyBounce(Rigidbody otherRb, float impact)
        {
            Vector3 bounceDir = (otherRb.transform.position - transform.position).normalized;
            bounceDir.y = Mathf.Abs(bounceDir.y) + 0.8f;
            bounceDir = bounceDir.normalized;

            float bounceForce = Mathf.Clamp(impact * 2f, 3f, 20f);
            otherRb.AddForce(bounceDir * bounceForce, ForceMode.VelocityChange);
        }

        // ────────────────────────────────────────────────────────────────
        // Health & Death
        // ────────────────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// รับดาเมจจากซอมบี้ — กัดทีเดียวตาย + กลายเป็นซอมบี้
        /// </summary>
        public void TakeDamage(float amount, bool isZombieBite)
        {
            if (_isDead) return;

            if (isZombieBite) _currentHealth = 0;
            else _currentHealth -= amount;

            if (_currentHealth <= 0) Die(isZombieBite);
        }

        private void Die(bool turnIntoZombie = false)
        {
            _isDead = true;
            StopFleeing();
            if (_agent != null) _agent.enabled = false;
            DespawnSoldiers();

            if (turnIntoZombie)
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.SpawnNormalZombie(transform.position);
                }
            }

            // VFX
            if (_data != null && _data.deathVFX != null)
                Instantiate(_data.deathVFX, transform.position, Quaternion.identity);

            // SFX
            if (_data != null && _data.deathSFX != null)
            {
                float vol = Simulation.UI.GameSettings.LoadSFXVolume() * Simulation.UI.GameSettings.LoadMasterVolume();
                AudioSource.PlayClipAtPoint(_data.deathSFX, transform.position, vol);
            }

            // Animation
            if (_animator != null) _animator.SetTrigger("Die");

            // แจ้ง Manager
            if (NPCSkillManager.Instance != null)
                NPCSkillManager.Instance.DespawnNPC(this);
            else
                Destroy(gameObject, 1f);
        }

        private void OnDestroy()
        {
            DespawnSoldiers();
        }
    }
}
