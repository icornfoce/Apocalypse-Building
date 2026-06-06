using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Digger Zombie — มาแบบ zombie ปกติ
    /// - พอเจอกำแพงขวาง จะมุดดินลงไปขุดผ่าน
    /// - เมื่อพ้นกำแพง จะโผล่ขึ้นมาเดินปกติ
    /// </summary>
    public class DiggerZombieAI : ZombieAI
    {
        public enum DiggerState { Surface, Underground }

        [Header("Digger Settings")]
        public float digSpeed = 1.0f;
        public float floorDigDamage = 15f;
        public float digInterval = 0.8f;
        public float digDistance = 2.0f;
        public float digWaitTime = 0.5f;
        
        [Header("Visual")]
        [Tooltip("Y offset ตอนขุดอยู่ใต้ดิน (ค่าลบ = จมลงไป)")]
        public float undergroundYOffset = -2.5f; // เพิ่มความลึกให้พ้น Collider พื้นแน่นอน

        // animator and audioSource are inherited from the base ZombieAI class
        
        [Header("Visual Effects (VFX) Prefabs")]
        public GameObject digStartVFX;
        public GameObject diggingVFX;
        public GameObject surfaceVFX;

        [Header("Sound Effects (SFX) Clips")]
        public AudioClip digStartSFX;
        public AudioClip diggingSFX;
        public AudioClip surfaceSFX;

        private DiggerState _currentState = DiggerState.Surface;
        private Vector3 _digStartPos;
        private Vector3 _digTargetPos;
        private float _digTimer;
        private float _digWaitTimer;

        protected override void Awake()
        {
            base.Awake();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        protected override void Update()
        {
            if (_isDead) return;

            // 1. จัดการเรื่องฟิสิกส์และการร่วง (เฉพาะตอนอยู่บนผิวดิน)
            if (_currentState == DiggerState.Surface)
            {
                HandleSurfacePhysics();
            }

            // 2. ค้นหาเป้าหมาย
            _findTargetCooldown -= Time.deltaTime;
            if (_findTargetCooldown <= 0f)
            {
                _findTargetCooldown = FIND_TARGET_INTERVAL;
                FindTarget();
            }

            // หา target ที่ใกล้ที่สุด (PersonAI หรือ NPCController)
            Transform targetTransform = null;
            if (_targetPerson != null) targetTransform = _targetPerson.transform;
            else if (_targetNPCController != null) targetTransform = _targetNPCController.transform;

            if (targetTransform == null)
            {
                if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            // 3. จัดการ State การเดิน/ขุด
            float dist = Vector3.Distance(transform.position, targetTransform.position);

            if (_currentState == DiggerState.Surface)
            {
                if (dist <= attackRange)
                {
                    if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                    AttackPerson();
                }
                else
                {
                    if (_agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(targetTransform.position);
                    }
                    
                    // เช็คกำแพงขวางหน้าเพื่อเริ่มขุด
                    CheckForWalls();
                }
            }
            else
            {
                // โหมดมุดดิน
                MoveUnderground();
            }
        }

        private void HandleSurfacePhysics()
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
        }

        protected override void CheckForWalls()
        {
            // ตรวจสอบทิศทางที่จะไป
            Vector3 direction = (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
                ? _agent.velocity.normalized
                : transform.forward;

            // ยิง Ray ไปข้างหน้าเพื่อหากำแพง
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            if (UnityEngine.Physics.SphereCast(ray, 0.4f, out RaycastHit hit, 1.2f, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                
                // ถ้าเป็นพื้นหรือบันได ไม่ต้องขุด ให้เดินผ่านไปเลย
                if (unit != null && unit.Data != null)
                {
                    bool isFloor = unit.Data.structureType == Simulation.Data.StructureType.Floor;
                    bool isStair = unit.Data.structureName.ToLower().Contains("stair");
                    bool isSpikeTrap = unit.GetComponentInChildren<SpikeTrap>() != null;

                    if (isFloor || isStair || isSpikeTrap)
                        return;
                }

                if (unit != null)
                {
                    StartDigging(unit);
                }
            }
        }

        private void StartDigging(StructureUnit obstacle)
        {
            if (_currentState == DiggerState.Underground) return;

            Debug.Log($"<color=orange>[DiggerZombie]</color> Starting to dig under {obstacle.name}");
            
            _currentState = DiggerState.Underground;
            _digStartPos = transform.position;
            
            // คำนวณทิศทางการขุด
            Vector3 digDirection = transform.forward;
            if (_agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.01f)
            {
                digDirection = _agent.velocity.normalized;
            }
            digDirection.y = 0;
            digDirection = digDirection.normalized;
            
            // ตั้งเป้าหมายขุดข้ามไปตามระยะทาง digDistance
            _digTargetPos = _digStartPos + digDirection * digDistance;
            _digWaitTimer = 0f;
            _digTimer = 0f;
            if (_agent != null) _agent.enabled = false;
            
            // เล่น Animation / VFX / SFX เมื่อเริ่มขุด
            if (animator != null) animator.SetTrigger("DigStart");
            if (digStartVFX != null) Instantiate(digStartVFX, _digStartPos, Quaternion.identity);
            if (audioSource != null && digStartSFX != null) audioSource.PlayOneShot(digStartSFX);
            
            // ขยับตัวลงไปใต้ดิน
            transform.position += new Vector3(0, undergroundYOffset, 0);
            
            // ดันตัวไปข้างหน้าเล็กน้อยเพื่อให้พ้นจุดเริ่มกำแพง
            transform.position += transform.forward * 0.8f;

            if (_rb != null) 
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void MoveUnderground()
        {
            if (_targetPerson == null && _targetNPCController == null) return;

            // คำนวณเป้าหมายขุดในระดับความลึกปัจจุบัน
            Vector3 targetPosOnXZ = new Vector3(_digTargetPos.x, transform.position.y, _digTargetPos.z);
            float distToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(_digTargetPos.x, 0, _digTargetPos.z));

            if (distToTarget > 0.1f)
            {
                // ขยับตัวไปข้างหน้าใต้ดินจนถึงเป้าหมาย
                transform.position = Vector3.MoveTowards(transform.position, targetPosOnXZ, digSpeed * Time.deltaTime);
                Vector3 moveDir = (targetPosOnXZ - transform.position).normalized;
                if (moveDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
                return;
            }

            // เมื่อถึงตำแหน่งปลายทางใต้ดิน (ข้ามพ้นกำแพงตามระยะทางที่กำหนดแล้ว)
            // เช็คว่ามีสิ่งกีดขวาง เช่น พื้น หรืออย่างอื่นอยู่ด้านบนหรือไม่
            bool isBlockedAbove = UnityEngine.Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.up, out RaycastHit hitUp, 3.5f, LayerMask.GetMask("Structure"));
            if (isBlockedAbove)
            {
                StructureUnit unitAbove = hitUp.collider.GetComponentInParent<StructureUnit>();
                if (unitAbove != null)
                {
                    bool isSpikeTrap = unitAbove.GetComponentInChildren<SpikeTrap>() != null;
                    if (!isSpikeTrap)
                    {
                        // ถ้าเจอพื้นหรืออย่างอื่นขวางด้านบน ให้โจมตีมันจนพัง
                        _digTimer += Time.deltaTime;
                        if (_digTimer >= digInterval)
                        {
                            _digTimer = 0f;
                            var stress = unitAbove.GetComponent<Simulation.Physics.StructuralStress>();
                            if (stress != null) stress.ApplyMaxHPDamage(floorDigDamage);
                            else unitAbove.TakeMaxHPDamage(floorDigDamage);
                            
                            // เล่น Animation / VFX / SFX ขณะกำลังขุดโจมตีสิ่งกีดขวางด้านบน

                            if (diggingVFX != null) Instantiate(diggingVFX, hitUp.point, Quaternion.identity);
                            if (audioSource != null && diggingSFX != null) audioSource.PlayOneShot(diggingSFX);
                            
                            Debug.Log($"<color=orange>[DiggerZombie]</color> Blocked above by {unitAbove.name}. Attacking structure!");
                        }
                        // ทำลายสิ่งกีดขวางอยู่ ยังไม่โผล่ขึ้นไป
                        return;
                    }
                }
            }

            // ถ้าไม่มีอะไรขวางด้านบนแล้ว (หรือตีจนพังไปแล้ว) ให้ขึ้นไปด้านบนโดยต้องรอเวลาขุดข้าม
            _digWaitTimer += Time.deltaTime;
            if (_digWaitTimer >= digWaitTime)
            {
                Surface();
            }
        }

        private void Surface()
        {
            _currentState = DiggerState.Surface;
            
            Vector3 surfacePos = transform.position - new Vector3(0, undergroundYOffset, 0);

            // กลับขึ้นมาที่ระดับพื้นปกติ
            transform.position = surfacePos;

            // เล่น Animation / VFX / SFX เมื่อโผล่ขึ้นมาผิวดิน
            if (animator != null) animator.SetTrigger("Surface");
            if (surfaceVFX != null) Instantiate(surfaceVFX, surfacePos, Quaternion.identity);
            if (audioSource != null && surfaceSFX != null) audioSource.PlayOneShot(surfaceSFX);

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if (_agent != null)
                {
                    _agent.enabled = true;
                    _agent.speed = moveSpeed;
                }
            }
            
            Debug.Log($"<color=orange>[DiggerZombie]</color> Cleared obstacle, surfaced!");
        }
    }
}
