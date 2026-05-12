using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Digger Zombie — มาแบบ zombie ปกติ (ใช้ NavMesh เดินบนพื้น)
    /// - พอเจอกำแพงขวาง จะมุดดินลงไปขุดผ่าน
    /// - เมื่อพ้นกำแพง (ไม่มีพื้นทับ) จะโผล่ขึ้นมาเดินปกติ
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class DiggerZombieAI : MonoBehaviour
    {
        public enum DiggerState { Surface, Underground }

        [Header("Settings")]
        public float moveSpeed = 1.5f;
        public float digSpeed = 0.8f;
        public float attackDamage = 10f;
        public float attackInterval = 1.5f;
        public float attackRange = 1.5f;
        public float maxHealth = 60f;
        public float floorDigDamage = 15f;
        public float digInterval = 1.0f;

        [Header("Crush Damage")]
        public float damageImpactThreshold = 3f;

        [Header("Death")]
        public GameObject deathVFX;

        [Header("Visual")]
        [Tooltip("Y offset ตอนขุดอยู่ใต้ดิน (ค่าลบ = จมลงไป)")]
        public float undergroundYOffset = -0.8f;

        private float _currentHealth;
        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private PersonAI _targetPerson;
        private float _personAttackTimer;
        private float _digTimer;
        private bool _isDead = false;
        
        private DiggerState _currentState = DiggerState.Surface;
        private StructureUnit _currentFloorTarget;

        private float _findTargetCooldown;
        private const float FIND_TARGET_INTERVAL = 0.5f;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();

            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void Start()
        {
            if (_agent != null)
            {
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = attackRange * 0.8f;
                _agent.areaMask = NavMesh.AllAreas;
                _agent.radius = 0.55f; // เพิ่มขนาดตัวไม่ให้ลอดประตูได้

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

            if (_currentState == DiggerState.Surface)
            {
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
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        _agent.enabled = true;
                        _rb.isKinematic = true;
                        var col = GetComponent<CapsuleCollider>();
                        if (col != null) col.isTrigger = true;
                    }
                }

                if (_agent != null && !_agent.enabled) return; // กำลังร่วงอยู่
            }

            _findTargetCooldown -= Time.deltaTime;
            if (_findTargetCooldown <= 0f)
            {
                _findTargetCooldown = FIND_TARGET_INTERVAL;
                FindTarget();
            }

            if (_targetPerson == null)
            {
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            float dist = Vector3.Distance(transform.position, _targetPerson.transform.position);

            if (dist <= attackRange && _currentState == DiggerState.Surface)
            {
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                AttackPerson();
            }
            else
            {
                if (_currentState == DiggerState.Surface)
                {
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(_targetPerson.transform.position);
                    }
                    else
                    {
                        // Fallback movement if NavMesh fails
                        Vector3 dir = (_targetPerson.transform.position - transform.position).normalized;
                        transform.position += dir * moveSpeed * Time.deltaTime;
                        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
                    }

                    CheckForWallsToDig();
                }
                else if (_currentState == DiggerState.Underground)
                {
                    MoveUnderground();
                }
            }
        }

        private void FindTarget()
        {
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

        private void CheckForWallsToDig()
        {
            Vector3 direction = (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
                ? _agent.velocity.normalized
                : transform.forward;

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            if (UnityEngine.Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                
                // ข้ามการขุดถ้าเป็นบันไดหรือพื้น (เดินบนมันได้)
                if (unit != null && unit.Data != null)
                {
                    bool isStair = unit.Data.structureName.ToLower().Contains("stair") || (unit.Data.prefab != null && unit.Data.prefab.name.ToLower().Contains("stair"));
                    bool isFloor = unit.Data.structureType == Simulation.Data.StructureType.Floor;
                    
                    if (isStair || isFloor) return;
                }

                if (unit != null)
                {
                    // ชนกำแพง/ประตู หรือสิ่งกีดขวาง ให้หยุดเดินก่อนจะขุด (ถ้าจำเป็น)
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;

                    // เจอกำแพง! เปลี่ยนสถานะไปขุดดิน
                    _currentState = DiggerState.Underground;
                    if (_agent != null) _agent.enabled = false;
                    transform.position += new Vector3(0, undergroundYOffset, 0);
                    Debug.Log($"<color=orange>[DiggerZombie]</color> Hit wall: {unit.name}, digging underground!");
                }
            }
        }

        private void MoveUnderground()
        {
            if (_targetPerson == null) return;

            Vector3 targetPos = _targetPerson.transform.position;
            Vector3 moveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

            _currentFloorTarget = null;
            bool isUnderStructure = false;

            if (UnityEngine.Physics.Raycast(transform.position, Vector3.up, out RaycastHit hitUp, 2f, LayerMask.GetMask("Structure")))
            {
                isUnderStructure = true;
                StructureUnit unit = hitUp.collider.GetComponentInParent<StructureUnit>();
                if (unit != null && unit.Data != null && unit.Data.structureType == Simulation.Data.StructureType.Floor)
                {
                    _currentFloorTarget = unit;
                }
            }

            if (!isUnderStructure)
            {
                // ไม่เจออะไรขวางอยู่ข้างบนแล้ว แปลว่าพ้นกำแพง! ขึ้นมาบนดิน
                _currentState = DiggerState.Surface;
                transform.position -= new Vector3(0, undergroundYOffset, 0);
                
                if (_agent != null)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                    }
                    _agent.enabled = true;
                }
                
                Debug.Log($"<color=orange>[DiggerZombie]</color> Cleared obstacle, surfaced!");
                return;
            }

            // ขยับไปข้างหน้าใต้ดิน
            float currentSpeed = (_currentFloorTarget != null) ? digSpeed : moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, currentSpeed * Time.deltaTime);

            Vector3 dir = (moveTarget - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

            if (_currentFloorTarget != null)
            {
                DigFloor();
            }
        }

        private void DigFloor()
        {
            _digTimer += Time.deltaTime;
            if (_digTimer >= digInterval)
            {
                _digTimer = 0f;

                if (_currentFloorTarget == null) return;

                var stress = _currentFloorTarget.GetComponent<Simulation.Physics.StructuralStress>();
                if (stress != null)
                {
                    stress.ApplyMaxHPDamage(floorDigDamage);
                }
                else
                {
                    _currentFloorTarget.TakeMaxHPDamage(floorDigDamage);
                }

                Debug.Log($"<color=orange>[DiggerZombie]</color> Digging through floor: {_currentFloorTarget.name}");
            }
        }

        private void AttackPerson()
        {
            _personAttackTimer += Time.deltaTime;
            if (_personAttackTimer >= attackInterval)
            {
                _personAttackTimer = 0f;
                if (_targetPerson != null && !_targetPerson.IsDead)
                {
                    _targetPerson.TakeDamage(0f, true); // กัดทีเดียวตาย
                    Debug.Log($"<color=orange>[DiggerZombie]</color> Attacking person: {_targetPerson.name}");
                }
            }
        }

        // ── Crush Damage ────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;

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

        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0) Die();
        }

        private void Die()
        {
            _isDead = true;
            if (_agent != null) _agent.enabled = false;
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"<color=green>[DiggerZombie]</color> {name} died!");
            Destroy(gameObject, 0.5f);
        }
    }
}
