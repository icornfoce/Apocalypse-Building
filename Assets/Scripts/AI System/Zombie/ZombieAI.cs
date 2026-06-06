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

        [Header("Effects & Animations")]
        [Tooltip("Animator สำหรับควบคุม Animation")]
        public Animator animator;
        [Tooltip("AudioSource สำหรับเล่นเสียง")]
        public AudioSource audioSource;

        [Header("Sound Effects (SFX) Clips")]
        public AudioClip attackSFX;
        public AudioClip biteWallSFX;
        public AudioClip hurtSFX;
        public AudioClip deathSFX;

        [Header("Visual Effects (VFX) Prefabs")]
        public GameObject biteWallVFX;

        protected float _currentHealth;
        protected NavMeshAgent _agent;
        protected Rigidbody _rb;
        protected PersonAI _targetPerson;
        protected float _personAttackTimer;
        protected float _wallAttackTimer;
        protected bool _isDead = false;
        protected bool _isAttackingWall = false;
        protected StructureUnit _lastAttackedWall;

        // Cache สำหรับ FindTarget (ไม่ต้องค้นทุกเฟรม)
        protected float _findTargetCooldown;
        protected const float FIND_TARGET_INTERVAL = 0.5f;

        public bool IsDead => _isDead;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        protected virtual void Start()
        {
            _personAttackTimer = attackInterval;
            _wallAttackTimer = attackInterval;

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

        protected virtual void Update()
        {
            if (_isDead) return;

            // Increment attack timers
            _personAttackTimer += Time.deltaTime;
            _wallAttackTimer += Time.deltaTime;

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
                    // ตั้ง baseOffset ให้ตรงกับ CapsuleCollider เพื่อป้องกันตัวละครจมลงพื้น
                    var capsule = GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        _agent.baseOffset = capsule.center.y - capsule.height * 0.5f;
                    }
                    // Snap ตำแหน่งลงบน NavMesh พอดี
                    transform.position = new Vector3(hit.position.x, hit.position.y, hit.position.z);
                    _agent.enabled = true;
                    _rb.isKinematic = true;
                    // คืนค่า collision mode กลับปกติ (ประหยัด performance)
                    _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
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
                    // เช็คว่าเห็นคนหรือไม่ — ถ้าเห็น ให้เลิกสนใจกำแพงและวิ่งไปหาทันที
                    if (CanSeePerson(_targetPerson))
                    {
                        if (_isAttackingWall) ResetWallAttack(true);
                    }
                    else
                    {
                        // ถ้าไม่เห็นคน ค่อยเช็คกำแพงขวางทาง
                        CheckForWalls();
                    }

                    // เดินไปหา ถ้าไม่ได้ติดกำแพง
                    if (_agent.enabled && _agent.isOnNavMesh)
                    {
                        if (!_isAttackingWall)
                        {
                            _agent.isStopped = false;
                            _agent.SetDestination(_targetPerson.transform.position);
                        }
                        else
                        {
                            _agent.isStopped = true;
                        }
                    }
                }
            }
            else
            {
                // ไม่มีเป้าหมาย — ยืนเฉยๆ
                if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }

            // อัปเดต Animation เดิน/เคลื่อนที่
            if (animator != null)
            {
                bool isMoving = _agent != null && _agent.enabled && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f;
                animator.SetBool("IsMoving", isMoving);
            }
        }

        protected bool CanSeePerson(PersonAI person)
        {
            if (person == null || person.IsDead) return false;

            // เช็ค Line of Sight (LOS)
            Vector3 start = transform.position + Vector3.up * 1.5f;
            Vector3 end = person.transform.position + Vector3.up * 1.0f;
            Vector3 dir = (end - start).normalized;
            float dist = Vector3.Distance(start, end);

            // ยิง Ray ไปหาคน ถ้าไม่ติดอะไรเลย (รวมถึงกำแพง) แสดงว่าเห็น
            if (UnityEngine.Physics.Raycast(start, dir, out RaycastHit hit, dist + 0.5f))
            {
                if (hit.collider.gameObject == person.gameObject || hit.collider.transform.IsChildOf(person.transform))
                {
                    return true;
                }
            }
            return false;
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

        protected virtual void AttackPerson()
        {
            if (_personAttackTimer >= attackInterval)
            {
                _personAttackTimer = 0f;
                if (_targetPerson != null && !_targetPerson.IsDead)
                {
                    _targetPerson.TakeDamage(0f, true); // กัดทีเดียวตาย

                    // เล่น Animation / SFX กัดคน
                    if (animator != null) animator.SetTrigger("Bite");
                    if (audioSource != null && attackSFX != null) audioSource.PlayOneShot(attackSFX);

                    Debug.Log($"<color=red>[Zombie]</color> Attacking person: {_targetPerson.name}");
                }
            }
        }

        protected virtual void CheckForWalls()
        {
            // ทิศทางที่ต้องการไป: ใช้ความเร็วถ้ายังเดินอยู่ หรือทิศทางไปหาเป้าหมายถ้าหยุดแล้ว
            Vector3 direction = transform.forward;
            if (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
            {
                direction = _agent.velocity.normalized;
            }
            else if (_targetPerson != null)
            {
                direction = (_targetPerson.transform.position - transform.position).normalized;
                direction.y = 0;
            }

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            if (UnityEngine.Physics.SphereCast(ray, 0.5f, out RaycastHit hit, attackRange, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                
                // ข้ามการโจมตีถ้าเป็นบันไดหรือพื้น (เพื่อให้เดินขึ้น/เดินผ่านได้)
                if (unit != null && unit.Data != null)
                {
                    bool isStair = unit.Data.structureName.ToLower().Contains("stair") || (unit.Data.prefab != null && unit.Data.prefab.name.ToLower().Contains("stair"));
                    bool isFloor = unit.Data.structureType == Simulation.Data.StructureType.Floor;
                    bool isSpikeTrap = unit.GetComponentInChildren<SpikeTrap>() != null;
                    
                    if (isStair || isFloor || isSpikeTrap)
                    {
                        ResetWallAttack(false);
                        return;
                    }
                }

                if (unit != null)
                {
                    _isAttackingWall = true;
                    _lastAttackedWall = unit;

                    // หันหน้าไปทางกำแพง
                    Vector3 lookDir = hit.point - transform.position;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

                    if (_wallAttackTimer >= attackInterval)
                    {
                        _wallAttackTimer = 0f;

                        var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                        if (stress != null)
                        {
                            stress.ApplyMaxHPDamage(attackDamage);
                        }
                        else
                        {
                            unit.TakeMaxHPDamage(attackDamage);
                        }

                        // ── Architect Buff: สะท้อนดาเมจ 20% กลับไปยังซอมบี้ ──
                        var aura = unit.GetComponent<Simulation.NPC.ArchitectAura>();
                        if (aura != null && aura.IsActive)
                        {
                            float reflectedDamage = attackDamage * aura.reflectPercent;
                            TakeDamage(reflectedDamage);
                            Debug.Log($"<color=magenta>[Architect]</color> Reflected {reflectedDamage:F1} damage to {name}!");
                        }

                        // เล่น Animation / SFX / VFX กัดกำแพง
                        if (animator != null) animator.SetTrigger("Bite");
                        if (audioSource != null && biteWallSFX != null) audioSource.PlayOneShot(biteWallSFX);
                        if (biteWallVFX != null) Instantiate(biteWallVFX, hit.point, Quaternion.LookRotation(hit.normal));

                        Debug.Log($"<color=red>[Zombie]</color> Biting wall: {unit.name}");
                    }
                    return;
                }
            }

            // ถ้าไม่เจอกำแพงแล้ว แต่ก่อนหน้านี้กำลังกัดอยู่
            if (_isAttackingWall)
            {
                ResetWallAttack(true);
            }
            else
            {
                _wallAttackTimer = attackInterval;
            }
        }

        protected virtual void ResetWallAttack(bool forceRecalculate)
        {
            _isAttackingWall = false;
            _wallAttackTimer = attackInterval;
            _lastAttackedWall = null;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                if (forceRecalculate && _targetPerson != null)
                {
                    _agent.ResetPath();
                    _agent.SetDestination(_targetPerson.transform.position);
                }
            }
        }

        // ── Crush Damage (เหมือน PersonAI) ─────────────────────────────

        protected virtual void OnTriggerEnter(Collider other)
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
                    
                    // กระเด้งกลับ (Knockback / Bounce)
                    ApplyBounce(otherRb, impact);
                }
            }
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (_isDead) return;

            // รับความเสียหายเมื่อฟิสิกส์ทำงานปกติ (เช่น ตกจากที่สูง หรือมีของหล่นมาทับ)
            float impact = collision.relativeVelocity.magnitude;
            if (impact > damageImpactThreshold)
            {
                float massFactor = 1f;
                if (collision.rigidbody != null)
                {
                    massFactor = Mathf.Clamp(collision.rigidbody.mass, 1f, 500f);
                    // กระเด้งกลับ (Knockback / Bounce)
                    ApplyBounce(collision.rigidbody, impact);
                }
                TakeDamage(impact * massFactor * 2f);
            }
        }

        /// <summary>
        /// ทำให้วัตถุที่ตกลงมาโดนกระเด้งออกไป
        /// </summary>
        protected void ApplyBounce(Rigidbody otherRb, float impact)
        {
            // คำนวณทิศทางเด้ง: ออกจากตัวซอมบี้ + ดีดขึ้น
            Vector3 bounceDir = (otherRb.transform.position - transform.position).normalized;
            bounceDir.y = Mathf.Abs(bounceDir.y) + 0.8f; // ให้เด้งขึ้นเสมอ
            bounceDir = bounceDir.normalized;

            // แรงเด้งสัมพันธ์กับความเร็วตก แต่จำกัดไม่ให้เกินไป
            float bounceForce = Mathf.Clamp(impact * 2f, 3f, 20f);
            otherRb.AddForce(bounceDir * bounceForce, ForceMode.VelocityChange);
        }

        // ── HP ──────────────────────────────────────────────────────────

        public virtual void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // เล่น Animation / SFX เจ็บ
                if (animator != null) animator.SetTrigger("Hurt");
                if (audioSource != null && hurtSFX != null) audioSource.PlayOneShot(hurtSFX);
            }
        }

        protected virtual void Die()
        {
            _isDead = true;
            if (_agent != null) _agent.enabled = false;

            // เล่น Animation ตาย
            if (animator != null) animator.SetTrigger("Die");

            // เล่นเสียงตายแบบไม่โดนตัดเมื่อตัวละครถูกทำลาย
            if (deathSFX != null) AudioSource.PlayClipAtPoint(deathSFX, transform.position);

            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"<color=green>[Zombie]</color> {name} died!");
            Destroy(gameObject, 0.5f);
        }
    }
}
