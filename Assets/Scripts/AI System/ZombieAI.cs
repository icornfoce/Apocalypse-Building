using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Zombie AI — เดินหาคนและกัดกำแพงถ้าขวางทาง
    /// โดนของทับตายได้เหมือน NPC
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class ZombieAI : MonoBehaviour
    {
        [Header("Settings")]
        public float moveSpeed = 1.5f;
        public float attackDamage = 10f;
        public float attackInterval = 1.5f;
        public float attackRange = 1.5f;
        public float maxHealth = 50f;

        [Header("Crush Damage")]
        [Tooltip("ความแรงจากการชนขั้นต่ำที่จะทำให้ลดเลือด")]
        public float damageImpactThreshold = 3f;

        [Header("Death")]
        public GameObject deathVFX;

        private float _currentHealth;
        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private PersonAI _targetPerson;
        private float _personAttackTimer;
        private float _wallAttackTimer;
        private bool _isDead = false;

        // Cache สำหรับ FindTarget (ไม่ต้องค้นทุกเฟรม)
        private float _findTargetCooldown;
        private const float FIND_TARGET_INTERVAL = 0.5f;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            // ตั้งค่า Agent
            if (_agent != null)
            {
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = attackRange * 0.8f;
                _agent.areaMask = NavMesh.AllAreas;
                _agent.radius = 0.55f; // เพิ่มขนาดตัวไม่ให้ลอดประตูได้

                // เช็คว่าอยู่บน NavMesh
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _agent.enabled = true;
                    
                    if (_rb != null) _rb.isKinematic = true;
                    var col = GetComponent<CapsuleCollider>();
                    if (col != null) col.isTrigger = true;
                }
            }
        }

        private void Update()
        {
            if (_isDead) return;

            // เช็คพื้นรองรับ เพื่อให้ตกลงมาถ้าพื้นพัง
            float rayDist = 0.8f;
            bool hasFloor = UnityEngine.Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, rayDist, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            if (!hasFloor && _agent != null && _agent.enabled)
            {
                _agent.enabled = false;
                if (_rb != null)
                {
                    _rb.isKinematic = false;
                    _rb.useGravity = true;
                }
                var col = GetComponent<CapsuleCollider>();
                if (col != null) col.isTrigger = false;
            }
            else if (hasFloor && _agent != null && !_agent.enabled && _rb != null && _rb.linearVelocity.sqrMagnitude < 0.1f)
            {
                // กลับมาบนพื้นแล้ว เปิด Agent คืน
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _agent.enabled = true;
                    _rb.isKinematic = true;
                    var col = GetComponent<CapsuleCollider>();
                    if (col != null) col.isTrigger = true;
                }
            }

            if (_agent != null && !_agent.enabled) return; // กำลังร่วงอยู่ ไม่ต้องเดินหรือกัด

            // หาเป้าหมายใหม่ทุกๆ 0.5 วินาที (ลดภาระ)
            _findTargetCooldown -= Time.deltaTime;
            if (_findTargetCooldown <= 0f)
            {
                _findTargetCooldown = FIND_TARGET_INTERVAL;
                FindTarget();
            }

            if (_targetPerson != null)
            {
                float dist = Vector3.Distance(transform.position, _targetPerson.transform.position);

                if (dist <= attackRange)
                {
                    // ถึงตัวแล้ว — โจมตี
                    if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                    AttackPerson();
                }
                else
                {
                    // ยังไม่ถึง — เดินไปหา
                    if (_agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(_targetPerson.transform.position);
                    }

                    // เช็คกำแพงขวางทาง (ใช้ timer แยก)
                    CheckForWalls();
                }
            }
            else
            {
                // ไม่มีเป้าหมาย — ยืนเฉยๆ
                if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        protected virtual void FindTarget()
        {
            // ถ้าเป้าหมายปัจจุบันยังมีชีวิต ไม่ต้องหาใหม่
            if (_targetPerson != null && !_targetPerson.IsDead) return;

            PersonAI[] allPeople = Object.FindObjectsByType<PersonAI>(FindObjectsSortMode.None);
            float minDist = float.MaxValue;
            _targetPerson = null;

            foreach (var p in allPeople)
            {
                if (p == null || p.IsDead) continue;
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    _targetPerson = p;
                }
            }
        }

        protected PersonAI GetTarget() => _targetPerson;
        protected void SetTargetPerson(PersonAI person) => _targetPerson = person;

        private void AttackPerson()
        {
            _personAttackTimer += Time.deltaTime;
            if (_personAttackTimer >= attackInterval)
            {
                _personAttackTimer = 0f;
                if (_targetPerson != null && !_targetPerson.IsDead)
                {
                    _targetPerson.TakeDamage(0f, true); // กัดทีเดียวตาย
                    Debug.Log($"<color=red>[Zombie]</color> Attacking person: {_targetPerson.name}");
                }
            }
        }

        private void CheckForWalls()
        {
            // ใช้ Agent velocity เป็นทิศทาง (ถ้ามี) เพื่อให้กัดกำแพงที่ขวางจริงๆ
            Vector3 direction = (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
                ? _agent.velocity.normalized
                : transform.forward;

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            if (UnityEngine.Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                
                // ข้ามการโจมตีถ้าเป็นบันไดหรือพื้น (เพื่อให้เดินขึ้น/เดินผ่านได้)
                if (unit != null && unit.Data != null)
                {
                    bool isStair = unit.Data.structureName.ToLower().Contains("stair") || (unit.Data.prefab != null && unit.Data.prefab.name.ToLower().Contains("stair"));
                    bool isFloor = unit.Data.structureType == Simulation.Data.StructureType.Floor;
                    
                    if (isStair || isFloor)
                    {
                        _wallAttackTimer = 0f;
                        return; // ไม่นับว่าติดกำแพง ปล่อยให้ NavMesh พาขึ้นไป
                    }
                }

                if (unit != null)
                {
                    // ชนกำแพง/ประตู ให้หยุดเดินแล้วโจมตี
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;

                    _wallAttackTimer += Time.deltaTime;
                    if (_wallAttackTimer >= attackInterval)
                    {
                        _wallAttackTimer = 0f;

                        // ใช้ ApplyMaxHPDamage เพื่อให้ลด Max HP และขีดจำกัดทางกายภาพถาวร
                        var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                        if (stress != null)
                        {
                            stress.ApplyMaxHPDamage(attackDamage);
                        }
                        else
                        {
                            unit.TakeMaxHPDamage(attackDamage);
                        }

                        Debug.Log($"<color=red>[Zombie]</color> Biting wall: {unit.name}");
                    }
                }
            }
            else
            {
                // ไม่เจอกำแพง — รีเซ็ต timer
                _wallAttackTimer = 0f;
            }
        }

        // ── Crush Damage (เหมือน PersonAI) ─────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;

            // ขณะที่ collider เป็น trigger ให้รับความเสียหายจากสิ่งของที่ร่วงลงมาโดน
            Rigidbody otherRb = other.attachedRigidbody;
            if (otherRb != null)
            {
                float impact = otherRb.linearVelocity.magnitude;
                if (impact > damageImpactThreshold)
                {
                    float massFactor = Mathf.Clamp(otherRb.mass, 1f, 500f);
                    TakeDamage(impact * massFactor * 2f);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDead) return;

            // รับความเสียหายเมื่อฟิสิกส์ทำงานปกติ (เช่น ตกจากที่สูง หรือมีของหล่นมาทับ)
            if (collision.relativeVelocity.magnitude > damageImpactThreshold)
            {
                float massFactor = 1f;
                if (collision.rigidbody != null)
                {
                    massFactor = Mathf.Clamp(collision.rigidbody.mass, 1f, 500f);
                }
                TakeDamage(collision.relativeVelocity.magnitude * massFactor * 2f);
            }
        }

        // ── HP ──────────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0) Die();
        }

        protected virtual void Die()
        {
            _isDead = true;
            if (_agent != null) _agent.enabled = false;
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"<color=green>[Zombie]</color> {name} died!");
            Destroy(gameObject, 0.5f);
        }
    }
}
