using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Balloon Zombie — บินไปหา NPC โดยตรง
    /// - บินเหนือกำแพง ไม่สนกำแพง
    /// - ถ้าถึงตัว NPC แต่มีพื้นกั้นอยู่ข้างล่าง จะกินพื้นเพื่อลงไปหา
    /// - โดนของทับตายได้เหมือน NPC
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class BalloonZombieAI : MonoBehaviour
    {
        [Header("Settings")]
        public float flySpeed = 2.0f;
        public float moveSpeed = 1.5f;
        public float descendSpeed = 1.0f;
        public float attackDamage = 10f;
        public float attackInterval = 1.5f;
        public float attackRange = 1.5f;
        public float maxHealth = 40f;
        public float flyHeight = 8f;           // ความสูงที่บิน

        [Header("Floor Eating")]
        public float floorEatDamage = 20f;
        public float floorEatInterval = 0.8f;

        [Header("Crush Damage")]
        public float damageImpactThreshold = 3f;

        [Header("Death")]
        public GameObject deathVFX;

        [Header("Balloon Visuals")]
        public GameObject balloonObject;
        public GameObject balloonBurstVFX;

        private float _currentHealth;
        private Rigidbody _rb;
        private PersonAI _targetPerson;
        private float _personAttackTimer;
        private float _floorEatTimer;
        private float _wallAttackTimer;
        private bool _isDead = false;
        private bool _hasLanded = false;

        private NavMeshAgent _agent;

        // Cache
        private float _findTargetCooldown;
        private const float FIND_TARGET_INTERVAL = 0.5f;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.enabled = false; // ยังไม่เปิดจนกว่าจะลงจอด

            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            _rb.useGravity = false;
            _rb.isKinematic = true; // บินด้วย kinematic
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // ตั้ง collider เป็น trigger เพื่อให้บินผ่านสิ่งก่อสร้างได้
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void Start()
        {
            if (balloonObject == null)
            {
                Transform t = transform.Find("Balloon");
                if (t != null) balloonObject = t.gameObject;
            }

            // ยกขึ้นบินเลย
            transform.position += new Vector3(0, flyHeight, 0);
        }

        private void Update()
        {
            if (_isDead) return;

            // หาเป้าหมาย
            _findTargetCooldown -= Time.deltaTime;
            if (_findTargetCooldown <= 0f)
            {
                _findTargetCooldown = FIND_TARGET_INTERVAL;
                FindTarget();
            }

            if (_targetPerson == null) return;

            if (_hasLanded)
            {
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

                // ลงพื้นแล้ว — โจมตีเหมือน Zombie ปกติ
                float dist = Vector3.Distance(transform.position, _targetPerson.transform.position);

                if (dist <= attackRange)
                {
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                    AttackPerson();
                }
                else
                {
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(_targetPerson.transform.position);
                    }
                    CheckForWalls();
                }
            }
            else
            {
                // ยังบินอยู่ — บินไปเหนือเป้าหมายก่อน แล้วค่อยลง
                FlyTowardsTarget();
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

        private void FlyTowardsTarget()
        {
            Vector3 targetPos = _targetPerson.transform.position;

            // Phase 1: บินไปเหนือเป้าหมาย (XZ)
            Vector3 aboveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            float horizontalDist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(targetPos.x, 0, targetPos.z)
            );

            if (horizontalDist > 1.0f)
            {
                // ยังไม่ถึง — บินไปก่อน
                transform.position = Vector3.MoveTowards(transform.position, aboveTarget, flySpeed * Time.deltaTime);
                Vector3 dir = (aboveTarget - transform.position).normalized;
                if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
            }
            else
            {
                // อยู่เหนือเป้าหมายแล้ว — เริ่มลง
                TryDescend();
            }
        }

        private void TryDescend()
        {
            // เช็คว่ามีพื้นขวางข้างล่างไหม
            bool hasFloorBelow = false;
            StructureUnit floorUnit = null;

            if (UnityEngine.Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.0f, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                if (unit != null && unit.Data != null && unit.Data.structureType == Simulation.Data.StructureType.Floor)
                {
                    hasFloorBelow = true;
                    floorUnit = unit;
                }
            }

            if (hasFloorBelow && floorUnit != null)
            {
                // มีพื้นขวาง — กินพื้นเพื่อลงไป
                EatFloor(floorUnit);
            }
            else
            {
                // ไม่มีพื้นขวาง — ลงได้เลย
                float targetY = _targetPerson.transform.position.y;
                Vector3 descendTarget = new Vector3(transform.position.x, targetY, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, descendTarget, descendSpeed * Time.deltaTime);

                // เช็คว่าลงถึงพื้นแล้วหรือยัง
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), descendSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - targetY) < 0.3f)
                {
                    OnLanded();
                }
            }
        }

        private void OnLanded()
        {
            if (_isDead) return;

            _hasLanded = true;
            
            if (balloonObject != null)
            {
                balloonObject.SetActive(false);
            }
            if (balloonBurstVFX != null)
            {
                Instantiate(balloonBurstVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
            }

            if (_agent != null)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _agent.enabled = true;
                    _agent.speed = moveSpeed;
                    _agent.stoppingDistance = attackRange * 0.8f;
                    _agent.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                    _agent.radius = 0.55f; // เพิ่มขนาดตัวไม่ให้ลอดประตูได้
                    
                    if (_rb != null) _rb.isKinematic = true;
                    var col = GetComponent<CapsuleCollider>();
                    if (col != null) col.isTrigger = true;
                }
            }

            Debug.Log($"<color=magenta>[BalloonZombie]</color> Landed and starting ground movement!");
        }

        private void EatFloor(StructureUnit floor)
        {
            _floorEatTimer += Time.deltaTime;
            if (_floorEatTimer >= floorEatInterval)
            {
                _floorEatTimer = 0f;
                var stress = floor.GetComponent<Simulation.Physics.StructuralStress>();
                if (stress != null) stress.ApplyExternalDamage(floorEatDamage);
                else floor.TakeDamage(floorEatDamage);
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
                    Debug.Log($"<color=magenta>[BalloonZombie]</color> Attacking person: {_targetPerson.name}");
                }
            }
        }

        private void CheckForWalls()
        {
            Vector3 direction = (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
                ? _agent.velocity.normalized
                : transform.forward;

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            if (UnityEngine.Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                
                if (unit != null && unit.Data != null)
                {
                    bool isStair = unit.Data.structureName.ToLower().Contains("stair") || (unit.Data.prefab != null && unit.Data.prefab.name.ToLower().Contains("stair"));
                    bool isFloor = unit.Data.structureType == Simulation.Data.StructureType.Floor;
                    
                    if (isStair || isFloor) return;
                }

                if (unit != null)
                {
                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;

                    _wallAttackTimer += Time.deltaTime;
                    if (_wallAttackTimer >= attackInterval)
                    {
                        _wallAttackTimer = 0f;
                        var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                        if (stress != null) stress.ApplyMaxHPDamage(attackDamage);
                        else unit.TakeMaxHPDamage(attackDamage);

                        Debug.Log($"<color=magenta>[BalloonZombie]</color> Biting wall: {unit.name}");
                    }
                }
            }
            else
            {
                _wallAttackTimer = 0f;
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

        // ── HP ──────────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0) Die();
        }

        private void Die()
        {
            _isDead = true;
            _rb.isKinematic = false;
            _rb.useGravity = true;
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"<color=green>[BalloonZombie]</color> {name} died!");
            Destroy(gameObject, 1.0f);
        }
    }
}
