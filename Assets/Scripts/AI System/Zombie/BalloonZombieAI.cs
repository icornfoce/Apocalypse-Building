using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Balloon Zombie — บินไปหา NPC โดยตรง (Inherit from ZombieAI)
    /// - บินเหนือกำแพง ไม่สนกำแพง
    /// - พอเห็นคนในระยะ ลดระดับความสูงก่อน แล้วปล่อยตัวลงด้วยแรง g
    /// - กลายเป็น ZombieAI ที่ถ้าไม่มีทางลงให้กัดพื้นลง
    /// </summary>
    public class BalloonZombieAI : ZombieAI
    {
        [Header("Balloon Settings")]
        public float flySpeed = 2.0f;
        public float flyHeight = 4f;
        public float dropDistance = 1.5f;
        public float descendSpeed = 2.5f;          // ความเร็วลดระดับก่อนปล่อยตัว
        [Tooltip("ความสูงเหนือพื้นคนที่จะ PopBalloon หลังจาก descend")]
        public float releaseHeight = 1.0f;          // สูงกว่าคน 1 unit แล้วปล่อย
        public float floorEatDamage = 20f;
        public float floorEatInterval = 0.8f;

        [Header("Balloon Visuals")]
        public GameObject balloonObject;
        public GameObject balloonBurstVFX;

        // animator and audioSource are inherited from the base ZombieAI class

        [Header("Sound Effects (SFX) Clips")]
        public AudioClip balloonPopSFX;
        public AudioClip landSFX;
        public AudioClip floorEatSFX;

        [Header("Visual Effects (VFX) Prefabs")]
        public GameObject floorEatVFX;

        private bool _balloonPopped = false;
        private bool _hasLanded = false;
        private bool _isDescending = false;         // กำลังลดระดับอยู่ก่อนปล่อยตัว
        private float _floorEatTimer;
        private bool _isBitingFloor = false;

        protected override void Awake()
        {
            base.Awake();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            
            // เริ่มต้นแบบบิน (Kinematic)
            if (_agent != null) _agent.enabled = false;
            if (_rb != null)
            {
                _rb.useGravity = false;
                _rb.isKinematic = true;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = false;
        }

        protected override void Start()
        {
            if (balloonObject == null)
            {
                Transform t = transform.Find("Balloon");
                if (t != null) balloonObject = t.gameObject;
            }

            // ยกขึ้นบิน
            transform.position += new Vector3(0, flyHeight, 0);

            // เริ่มเล่น Animation บิน
            if (animator != null) animator.SetBool("IsFlying", true);
        }

        protected override void Update()
        {
            if (_isDead) return;

            if (!_hasLanded)
            {
                // เฟสบิน: หาเป้าหมายและบิน
                if (!_balloonPopped)
                {
                    _findTargetCooldown -= Time.deltaTime;
                    if (_findTargetCooldown <= 0f)
                    {
                        _findTargetCooldown = FIND_TARGET_INTERVAL;
                        FindTarget();
                    }

                    if (_targetPerson != null)
                    {
                        FlyTowardsTarget();
                    }
                }
            }
            else
            {
                // เช็คพื้นรองรับ เพื่อให้ตกลงมาถ้าพื้นพัง (ใช้การเช็คแบบปลอดภัยป้องกัน self-hit และทะลุพื้น)
                var capsule = GetComponent<CapsuleCollider>();
                float capsuleBottomY = capsule != null ? (transform.position.y + capsule.center.y - capsule.height * 0.5f) : transform.position.y;
                Vector3 rayStart = new Vector3(transform.position.x, capsuleBottomY + 0.5f, transform.position.z);
                float rayDist = 0.8f; // ระยะเช็คพื้นที่ปลอดภัยติดเท้าจริง
                int floorLayerMask = LayerMask.GetMask("Ground", "Structure");
                bool hasFloor = UnityEngine.Physics.Raycast(rayStart, Vector3.down, rayDist, floorLayerMask, QueryTriggerInteraction.Ignore);

                if (!hasFloor)
                {
                    _isBitingFloor = false; // หยุดกัดพื้นทันทีเพื่อร่วงหล่น
                    if (_agent != null) _agent.enabled = false;
                    if (_rb != null)
                    {
                        _rb.isKinematic = false;
                        _rb.useGravity = true;
                    }
                    if (capsule != null) capsule.isTrigger = false;
                }
                else if (hasFloor && _agent != null && !_agent.enabled && !_isBitingFloor && _rb != null && _rb.linearVelocity.sqrMagnitude < 0.1f)
                {
                    // กลับมาบนพื้นแล้ว เปิด Agent คืน
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        transform.position = new Vector3(hit.position.x, hit.position.y, hit.position.z);
                        _agent.enabled = true;
                        _rb.isKinematic = true;
                        _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        if (capsule != null) capsule.isTrigger = true;

                        // เล่น SFX / Animation ตอนสัมผัสพื้นปกติ
                        if (audioSource != null && landSFX != null) audioSource.PlayOneShot(landSFX);
                        if (animator != null) animator.SetTrigger("Land");
                    }
                }

                if (_agent != null && !_agent.enabled) return; // กำลังร่วงอยู่ หรือกำลังกัดพื้นแบบไร้ Agent ไม่ต้องเดินหรือกัด NPC

                // หาเป้าหมายใหม่ทุกๆ 0.5 วินาที
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
                        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                        AttackPerson();
                    }
                    else
                    {
                        if (CanSeePerson(_targetPerson))
                        {
                            if (_isAttackingWall) ResetWallAttack(true);
                        }
                        else
                        {
                            CheckForWalls();
                        }

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
                    if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                }

                // ถ้าไม่มีทางลงให้กัดพื้นลง
                if (_targetPerson != null)
                {
                    TryEatFloorDown();
                }
            }
        }

        private void FlyTowardsTarget()
        {
            Vector3 targetPos = _targetPerson.transform.position;
            float horizontalDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(targetPos.x, targetPos.z));

            // ── Phase: ลดระดับความสูงก่อนปล่อยตัว ──
            if (_isDescending)
            {
                float targetY = targetPos.y + releaseHeight;
                if (transform.position.y > targetY + 0.05f)
                {
                    // ค่อยๆ ลงมา (หยุดขยับแนวนอน)
                    Vector3 descendTarget = new Vector3(transform.position.x, targetY, transform.position.z);
                    transform.position = Vector3.MoveTowards(transform.position, descendTarget, descendSpeed * Time.deltaTime);
                }
                else
                {
                    // ลงถึงระดับที่ต้องการแล้ว — ปล่อยตัวลงด้วยแรง g
                    PopBalloon();
                }
                return;
            }

            // ── Phase: เดินทางแนวนอนเข้าหาเป้าหมาย ──

            // เมื่อเข้าใกล้เป้าหมายในระยะ dropDistance หรืออยู่เหนือเป้าโดยตรง → เริ่ม descend เสมอ
            if (horizontalDist <= dropDistance || horizontalDist <= 1.0f)
            {
                _isDescending = true;
                return;
            }

            // ยังไม่ถึงเป้าหมาย (แนวนอน) → บินไปข้างหน้า
            Vector3 aboveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            transform.position = Vector3.MoveTowards(transform.position, aboveTarget, flySpeed * Time.deltaTime);
            Vector3 dir = (aboveTarget - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        private void TryEatFloorDown()
        {
            if (_targetPerson == null || _agent == null || !_agent.enabled) return;

            // Target is below us
            float verticalDist = transform.position.y - _targetPerson.transform.position.y;
            if (verticalDist > 1.5f)
            {
                // Check if there is no way down (e.g. agent path is partial or no path, or we are close horizontally but stuck above)
                float horizontalDist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(_targetPerson.transform.position.x, _targetPerson.transform.position.z)
                );

                bool noWayDown = false;
                if (_agent.isOnNavMesh)
                {
                    if (_agent.pathStatus == NavMeshPathStatus.PathPartial || !_agent.hasPath)
                    {
                        noWayDown = true;
                    }
                    // Or if we are close horizontally but can't get down
                    else if (horizontalDist < 2.0f && _agent.remainingDistance < 1.0f)
                    {
                        noWayDown = true;
                    }
                }
                else
                {
                    noWayDown = true;
                }

                if (noWayDown)
                {
                    // Raycast down to find the floor we are standing on
                    StructureUnit floorUnit = null;
                    if (UnityEngine.Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2.0f, LayerMask.GetMask("Structure")))
                    {
                        StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                        if (unit != null && unit.Data != null && unit.Data.structureType == Simulation.Data.StructureType.Floor)
                        {
                            floorUnit = unit;
                        }
                    }

                    if (floorUnit != null)
                    {
                        // Stop agent while biting
                        if (_agent.enabled && _agent.isOnNavMesh)
                        {
                            _agent.isStopped = true;
                        }

                        // Bite/Eat the floor
                        _floorEatTimer += Time.deltaTime;
                        if (_floorEatTimer >= floorEatInterval)
                        {
                            _floorEatTimer = 0f;
                            var stress = floorUnit.GetComponent<Simulation.Physics.StructuralStress>();
                            if (stress != null) stress.ApplyExternalDamage(floorEatDamage);
                            else floorUnit.TakeDamage(floorEatDamage);

                            // เล่น SFX / VFX / Animation ตอนกัดพื้น
                            if (animator != null) animator.SetTrigger("Bite");
                            if (audioSource != null && floorEatSFX != null) audioSource.PlayOneShot(floorEatSFX);
                            if (floorEatVFX != null) Instantiate(floorEatVFX, transform.position + Vector3.down * 0.5f, Quaternion.identity);

                            Debug.Log($"<color=magenta>[BalloonZombie]</color> No way down. Biting floor: {floorUnit.name}");
                        }
                    }
                }
            }
        }

        public bool HasLanded => _balloonPopped;

        public void PopBalloon()
        {
            if (_balloonPopped || _isDead) return;

            _balloonPopped = true;
            _hasLanded = true; // เข้าสู่สถานะปกติทันที เพื่อให้ base.Update() รับช่วงต่อเรื่องการเช็คการตกและสัมผัสพื้น

            if (balloonObject != null)
            {
                // Detach balloon and make it float up and pop
                balloonObject.transform.SetParent(null);
                FloatingBalloon fb = balloonObject.AddComponent<FloatingBalloon>();
                fb.Setup(balloonBurstVFX, 3.0f, 2.0f);
            }

            if (_agent != null) _agent.enabled = false;

            // ปล่อยร่วงลงมาด้วยแรงโน้มถ่วงฟิสิกส์ปกติ
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = false;

            // เล่น SFX / Animation ตอนลูกโป่งแตก
            if (audioSource != null && balloonPopSFX != null) audioSource.PlayOneShot(balloonPopSFX);
            if (animator != null)
            {
                animator.SetBool("IsFlying", false);
                animator.SetTrigger("Fall");
            }

            Debug.Log($"<color=magenta>[BalloonZombie]</color> Balloon popped! Dropping down with gravity.");
        }

        protected override void Die()
        {
            _isDead = true;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
            Destroy(gameObject, 1.0f);
        }

        protected override void CheckForWalls()
        {
            // BalloonZombie does not attack/bite walls; it only bites floors to navigate downwards
        }
    }

    /// <summary>
    /// Component สำหรับให้ลูกโป่งที่หลุดออกลอยขึ้นฟ้าแล้วแตก
    /// </summary>
    public class FloatingBalloon : MonoBehaviour
    {
        private GameObject _burstVFX;
        private float _floatSpeed = 3.0f;
        private float _duration = 2.0f;
        private float _timer;

        public void Setup(GameObject burstVFX, float floatSpeed, float duration)
        {
            _burstVFX = burstVFX;
            _floatSpeed = floatSpeed;
            _duration = duration;
            _timer = 0f;
        }

        private void Update()
        {
            transform.Translate(Vector3.up * _floatSpeed * Time.deltaTime, Space.World);
            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                if (_burstVFX != null)
                {
                    Instantiate(_burstVFX, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }
}
