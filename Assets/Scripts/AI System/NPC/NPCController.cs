using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Building;
using Simulation.Physics;
using Simulation.Mission;

namespace Simulation.NPC
{
    /// <summary>
    /// NPCController — แปะที่ตัว NPC แต่ละตัว
    /// จัดการ: เลือด, เดิน, สกิล, VFX/SFX, Animation
    /// สกิลทั้ง 6:
    ///   1. Engineer  — Toggle Stress Visualization
    ///   2. Builder   — เดินไปซ่อมโครงสร้าง
    ///   3. Economist — Auto ลดราคา 10% (จัดการที่ NPCSkillManager)
    ///   4. Architect — Auto Buff ออร่า (จัดการที่ NPCSkillManager)
    ///   5. Politician— Auto ยกเลิกภาษี (จัดการที่ NPCSkillManager)
    ///   6. Commander — เรียกทหารมายิง
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour
    {
        [Header("Data (Runtime)")]
        [SerializeField] private NPCSkillData _data;

        [Header("State")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private bool _isDead = false;
        [SerializeField] private bool _isSelected = false;
        [SerializeField] private bool _skillActive = false;

        // Components
        private NavMeshAgent _agent;
        private Animator _animator;
        private AudioSource _audioSource;

        // Skill Runtime
        private float _skillCooldownTimer = 0f;
        private bool _isRepairing = false;
        private StructuralStress _repairTarget;
        private bool _stressVisualsOn = false;
        private Coroutine _soldierCoroutine;
        private List<GameObject> _spawnedSoldiers = new List<GameObject>();

        // ── Public Properties ──
        public NPCSkillData Data => _data;
        public bool IsDead => _isDead;
        public bool IsSelected => _isSelected;
        public bool SkillActive => _skillActive;
        public float CurrentHealth => _currentHealth;
        public float HealthRatio => _data != null && _data.maxHealth > 0 ? Mathf.Clamp01(_currentHealth / _data.maxHealth) : 0f;

        // ────────────────────────────────────────────────────────────────
        // Initialization
        // ────────────────────────────────────────────────────────────────

        public void Initialize(NPCSkillData data)
        {
            _data = data;
            _currentHealth = data.maxHealth;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.speed = data.moveSpeed;
            _agent.stoppingDistance = 0.5f;
            _agent.radius = 0.35f;
            _agent.height = 1.8f;

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
            }

            // Rigidbody setup
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Collider setup
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        // ────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ────────────────────────────────────────────────────────────────

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
            if (_isDead || _agent == null || !_agent.enabled) return;

            // ยกเลิกการซ่อมถ้ากำลังทำอยู่
            if (_isRepairing) StopRepairing();

            // หาจุดที่ใกล้ที่สุดบน NavMesh
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);

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

        // ── 1. วิศวกร: Toggle Stress Visualization ──

        private void SkillEngineer()
        {
            _stressVisualsOn = !_stressVisualsOn;
            StructuralStress.SetVisualStatus(_stressVisualsOn);

            _skillActive = _stressVisualsOn;

            // Animation
            if (_animator != null) _animator.SetTrigger("UseSkill");

            Debug.Log($"<color=green>[Engineer]</color> Stress visuals: {(_stressVisualsOn ? "ON" : "OFF")}");
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

                // เรียก RepairFull เพื่อคืน HP (ในกรณีนี้เราค่อยๆ ซ่อม)
                // StructuralStress ไม่มี RepairPartial ดังนั้นเราจะเรียก RepairFull เมื่อถึง 100%
                // แต่เราสามารถ fake ได้โดยตรวจสอบ HealthRatio
                if (_repairTarget.HealthRatio < 1f)
                {
                    _repairTarget.RepairFull(); // ซ่อมเต็ม (ใช้ RepairFull ที่มีอยู่แล้ว)

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
                            AudioSource.PlayClipAtPoint(_data.skillEffectSFX, transform.position);
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
                AudioSource.PlayClipAtPoint(_data.skillActivateSFX, transform.position);
            }

            // Animation
            if (_animator != null) _animator.SetTrigger("UseSkill");
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

        private void Die()
        {
            _isDead = true;
            if (_agent != null) _agent.enabled = false;
            DespawnSoldiers();

            // VFX
            if (_data != null && _data.deathVFX != null)
                Instantiate(_data.deathVFX, transform.position, Quaternion.identity);

            // SFX
            if (_data != null && _data.deathSFX != null)
                AudioSource.PlayClipAtPoint(_data.deathSFX, transform.position);

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
