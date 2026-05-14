using UnityEngine;
using UnityEngine.AI;

namespace Simulation.Character
{
    /// <summary>
    /// AI สำหรับตัวละครให้เดินไปยังเป้าหมายด้วย NavMesh
    /// ถ้าเลือดหมดจะแสดง VFX และตาย
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PersonAI : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("ความเร็วในการเดิน (ควบคุมโดย NavMeshAgent ด้วย)")]
        public float moveSpeed = 2f;
        [Tooltip("ระยะที่ถือว่าถึงเป้าหมายแล้ว")]
        public float arrivalDistance = 0.1f;

        [Header("Health & Death")]
        public float maxHealth = 100f;
        public GameObject deathVFX;
        [Tooltip("ความแรงจากการชนขั้นต่ำที่จะทำให้ลดเลือด")]
        public float damageImpactThreshold = 3f;

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

        private float _currentHealth;
        private Transform _target;
        private Rigidbody _rb;
        private NavMeshAgent _agent;
        private bool _isDead = false;
        private GameObject _activePanicVFX;
        private float _panicTimer;
        private Vector3 _fleeDirection;

        public bool IsDead => _isDead;
        public bool HasReachedTarget { get; private set; } = false;
        public bool IsFleeing => _panicTimer > 0;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _currentHealth = maxHealth;
            
            // ล็อคการหมุนไม่ให้ล้ม
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            // ถ้ายังไม่ได้ Initialize ให้ทำทันที
            if (_agent == null) InitializeAgent();
        }

        public void InitializeAgent()
        {
            // เช็คว่าอยู่บน NavMesh หรือไม่ก่อน
            bool isOnNavMesh = UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas);
            if (isOnNavMesh)
            {
                transform.position = hit.position;
            }

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();

            // ตั้งค่า Agent
            if (_agent != null)
            {
                if (isOnNavMesh)
                {
                    _agent.enabled = true;
                    _agent.speed = moveSpeed;
                    _agent.stoppingDistance = arrivalDistance;
                    _agent.updateRotation = true;

                    _agent.radius = 0.3f;
                    _agent.height = 1.8f;
                    _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

                    if (_rb != null) _rb.isKinematic = true;
                    var col = GetComponent<CapsuleCollider>();
                    if (col != null) col.isTrigger = true;
                }
                else
                {
                    _agent.enabled = false;
                    if (_rb != null) _rb.isKinematic = false;
                    var col = GetComponent<CapsuleCollider>();
                    if (col != null) col.isTrigger = false;
                }
            }
        }

        public void SetTarget(Transform targetTransform)
        {
            _target = targetTransform;
            if (_agent != null && _target != null && _agent.enabled && _agent.isOnNavMesh)
            {
                // หาระยะที่ใกล้ที่สุดบน NavMesh จากตำแหน่งเป้าหมาย
                if (UnityEngine.AI.NavMesh.SamplePosition(_target.position, out UnityEngine.AI.NavMeshHit hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                }
                else
                {
                    _agent.SetDestination(_target.position);
                }
            }
        }

        private void Update()
        {
            if (_isDead) return;

            // เช็คพื้นรองรับ (หนีแค่บน Layer Ground และ Structure เท่านั้น)
            int floorMask = LayerMask.GetMask("Ground", "Structure");
            float rayDist = 0.8f;
            bool hasFloor = UnityEngine.Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit floorHit, rayDist, floorMask, QueryTriggerInteraction.Ignore);

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && hasFloor)
            {
                // ── ระบบตรวจจับซอมบี้และวิ่งหนี ──
                HandleFleeBehavior();

                if (IsFleeing)
                {
                    _agent.isStopped = false;
                    _agent.speed = fleeSpeed;
                    
                    Vector3 fleePos = transform.position + _fleeDirection * 5f;
                    if (UnityEngine.AI.NavMesh.SamplePosition(fleePos, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        _agent.SetDestination(hit.position);
                    }
                }
                else if (HasReachedTarget)
                {
                    _agent.isStopped = true;
                    _agent.speed = moveSpeed;
                }
                else if (_target != null)
                {
                    _agent.isStopped = false;
                    _agent.speed = moveSpeed;

                    // ถ้าเป้าหมายขยับ เราก็อัปเดตเรื่อยๆ (แต่ใช้ SamplePosition เพื่อความแม่นยำ)
                    if (Time.frameCount % 30 == 0) // อัปเดตทุก 30 เฟรมเพื่อลดภาระ
                    {
                        if (UnityEngine.AI.NavMesh.SamplePosition(_target.position, out UnityEngine.AI.NavMeshHit hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
                            _agent.SetDestination(hit.position);
                    }
                    
                    // เช็คว่าถึงเป้าหมายหรือยัง (นับว่าถึงถ้าอยู่ใกล้เป้าหมาย "หรือ" ไปต่อไม่ได้แล้วและอยู่ใกล้ที่สุดเท่าที่จะทำได้)
                    float distToTarget = Vector3.Distance(transform.position, _target.position);
                    bool arrived = distToTarget <= arrivalDistance + 0.1f;

                    // กรณีไปไม่ถึงเป้าหมายโดยตรง (เช่น เป้าอยู่ชั้นบนแต่ไม่มีบันได)
                    // ให้เช็คว่า Agent หยุดเดินแล้ว (remainingDistance น้อย) 
                    if (!arrived && !_agent.pathPending)
                    {
                        if (_agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
                        {
                            // ถ้าไปต่อไม่ได้แล้ว ให้ถือว่าถึงจุดที่ใกล้ที่สุดแล้ว
                            arrived = true;
                        }
                    }

                    if (arrived)
                    {
                        HasReachedTarget = true;
                        PersonTarget pt = _target.GetComponent<PersonTarget>();
                        if (pt != null) pt.StartFadeOut();
                    }
                }
            }
            else if (_agent != null && _agent.enabled && (!_agent.isOnNavMesh || !hasFloor))
            {
                // ร่วงหรือไม่มีพื้น
                StopFleeing();
                _agent.enabled = false;
                if (_rb != null) _rb.isKinematic = false;
                var col = GetComponent<CapsuleCollider>();
                if (col != null) col.isTrigger = false;
            }
        }

        private void HandleFleeBehavior()
        {
            // ค้นหาซอมบี้ใกล้ๆ
            Simulation.Mission.ZombieAI nearbyZombie = FindVisibleZombie();

            if (nearbyZombie != null)
            {
                // พบซอมบี้! เริ่มตกใจและวิ่งหนี
                _panicTimer = 3f; // หนีอย่างน้อย 3 วินาทีหลังจากไม่เห็นซอมบี้แล้ว
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

        private Simulation.Mission.ZombieAI FindVisibleZombie()
        {
            if (detectionRange <= 0)
            {
                Debug.LogWarning($"[PersonAI] {name} has detectionRange = 0! Please set it in Inspector.");
                return null;
            }

            // ตรวจสอบในรัศมีรอบตัว
            Collider[] hits = UnityEngine.Physics.OverlapSphere(transform.position, detectionRange);
            
            // Debug: ดูว่าเจออะไรบ้างในรัศมี (แสดงทุก 1 วินาที)
            if (Time.frameCount % 60 == 0) 
            {
                foreach(var h in hits) {
                    // Debug.Log($"[PersonAI] {name} sees collider: {h.name} on layer {LayerMask.LayerToName(h.gameObject.layer)}");
                }
            }

            foreach (var hit in hits)
            {
                // ข้ามถ้าเป็นตัวเอง
                if (hit.transform.root == transform.root) continue;

                var zombie = hit.GetComponentInParent<Simulation.Mission.ZombieAI>();
                if (zombie != null)
                {
                    if (!zombie.IsDead)
                    {
                        // เช็ค LOS (ยิงไปที่ใจกลาง Collider ที่เจอจริงๆ เพื่อความแม่นยำ)
                        Vector3 start = transform.position + Vector3.up * 1.0f; 
                        Vector3 end = hit.bounds.center; // ยิงไปที่กลางเป้าที่เจอ
                        Vector3 dir = (end - start).normalized;
                        float dist = Vector3.Distance(start, end);

                        // ถ้าเป้าหมายอยู่ใกล้มาก (แทบจะขี่คอกัน) ให้ถือว่าเห็นเลย
                        if (dist < 0.5f) return zombie;

                        // ใช้ ~0 เพื่อเช็คทุก Layer
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

                        // ถ้า Raycast ไม่โดนอะไรเลย แต่เรามั่นใจว่าตรงนั้นมีซอมบี้ (เพราะ OverlapSphere เจอ)
                        if (allHits.Length == 0)
                        {
                            return zombie;
                        }
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
                    // ปรับตำแหน่งให้อยู่บนหัว
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

        public void TakeDamage(float amount, bool isZombieBite = false)
        {
            if (_isDead) return;

            if (isZombieBite) _currentHealth = 0;
            else _currentHealth -= amount;

            if (_currentHealth <= 0) Die(isZombieBite);
        }

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
                    
                    // กระเด้งกลับ (Knockback / Bounce)
                    Vector3 forceDir = (otherRb.transform.position - transform.position).normalized;
                    forceDir.y = 1f; // ให้เด้งขึ้นด้วย
                    otherRb.AddForce(forceDir.normalized * impact * 0.5f, ForceMode.VelocityChange);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > damageImpactThreshold)
            {
                float massFactor = 1f;
                if (collision.rigidbody != null) massFactor = Mathf.Clamp(collision.rigidbody.mass, 1f, 500f);
                TakeDamage(collision.relativeVelocity.magnitude * massFactor * 2f);
            }
        }

        private void Die(bool turnIntoZombie = false)
        {
            _isDead = true;
            if (_agent != null) _agent.enabled = false;
            if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);

            if (turnIntoZombie)
            {
                if (Simulation.Mission.MissionManager.Instance != null)
                {
                    Simulation.Mission.MissionManager.Instance.SpawnNormalZombie(transform.position);
                }
            }
            Destroy(gameObject);
        }
    }
}
