using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Digger Zombie — ขุดดินข้ามกำแพงไปหา NPC
    /// - ไม่ใช้ NavMesh เดินตรงไปหาเป้าหมายเลย (ข้ามกำแพง)
    /// - ถ้าเจอพื้น (Floor) จะขุดช้าลง + ทำลายพื้นนั้น
    /// - โดนของทับตายได้เหมือน NPC
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class DiggerZombieAI : MonoBehaviour
    {
        [Header("Settings")]
        public float moveSpeed = 1.0f;
        public float digSpeed = 0.3f;         // ความเร็วตอนขุดพื้น (ช้าลง)
        public float attackDamage = 10f;
        public float attackInterval = 1.5f;
        public float attackRange = 1.5f;
        public float maxHealth = 60f;
        public float floorDigDamage = 15f;     // ดาเมจที่ทำใส่พื้นทุก tick
        public float digInterval = 1.0f;

        [Header("Crush Damage")]
        public float damageImpactThreshold = 3f;

        [Header("Death")]
        public GameObject deathVFX;

        [Header("Visual")]
        [Tooltip("Y offset ตอนขุดอยู่ใต้ดิน (ค่าลบ = จมลงไป)")]
        public float undergroundYOffset = -0.8f;

        private float _currentHealth;
        private Rigidbody _rb;
        private PersonAI _targetPerson;
        private float _personAttackTimer;
        private float _digTimer;
        private bool _isDead = false;
        private bool _isDigging = false;
        private StructureUnit _currentFloorTarget;

        // Cache สำหรับ FindTarget
        private float _findTargetCooldown;
        private const float FIND_TARGET_INTERVAL = 0.5f;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            _rb.useGravity = false; // ขุดใต้ดินไม่ต้องใช้แรงโน้มถ่วง
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // ตั้ง collider เป็น trigger เพื่อให้ทะลุกำแพงได้
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void Start()
        {
            // จมลงไปใต้ดินเล็กน้อย
            transform.position += new Vector3(0, undergroundYOffset, 0);
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

            float dist = Vector3.Distance(transform.position, _targetPerson.transform.position);

            if (dist <= attackRange)
            {
                // ถึงตัว — โจมตี
                AttackPerson();
            }
            else
            {
                // เคลื่อนที่ตรงไปหาเป้าหมาย (ข้ามกำแพง)
                MoveTowardsTarget();
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

        private void MoveTowardsTarget()
        {
            if (_targetPerson == null) return;

            Vector3 targetPos = _targetPerson.transform.position;
            // เก็บ Y เท่าเดิม (อยู่ใต้ดิน) — ยกเว้นเป้าหมายอยู่ชั้นอื่น
            Vector3 moveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

            // เช็คว่ามีพื้นขวางทางอยู่ไหม (Raycast ขึ้นบน)
            _currentFloorTarget = null;
            if (UnityEngine.Physics.Raycast(transform.position, Vector3.up, out RaycastHit hitUp, 2f, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hitUp.collider.GetComponentInParent<StructureUnit>();
                if (unit != null && unit.Data != null && unit.Data.structureType == Simulation.Data.StructureType.Floor)
                {
                    _currentFloorTarget = unit;
                    _isDigging = true;
                }
                else
                {
                    _isDigging = false;
                }
            }
            else
            {
                _isDigging = false;
            }

            // เคลื่อนที่: ช้าลงถ้ากำลังขุดพื้น
            float currentSpeed = _isDigging ? digSpeed : moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, currentSpeed * Time.deltaTime);

            // หันหน้าไปทางเป้าหมาย
            Vector3 dir = (moveTarget - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            // ขุดพื้นถ้ามีพื้นขวาง
            if (_isDigging && _currentFloorTarget != null)
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
                    stress.ApplyExternalDamage(floorDigDamage);
                }
                else
                {
                    _currentFloorTarget.TakeDamage(floorDigDamage);
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
                    _targetPerson.TakeDamage(attackDamage);
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
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"<color=green>[DiggerZombie]</color> {name} died!");
            Destroy(gameObject, 0.5f);
        }
    }
}
