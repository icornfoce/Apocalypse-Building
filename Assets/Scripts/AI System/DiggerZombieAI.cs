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
        
        [Header("Visual")]
        [Tooltip("Y offset ตอนขุดอยู่ใต้ดิน (ค่าลบ = จมลงไป)")]
        public float undergroundYOffset = -1.5f; // เพิ่มความลึกให้พ้น Collider พื้น

        private DiggerState _currentState = DiggerState.Surface;
        private StructureUnit _currentFloorTarget;
        private float _digTimer;

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

            if (_targetPerson == null)
            {
                if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            // 3. จัดการ State การเดิน/ขุด
            float dist = Vector3.Distance(transform.position, _targetPerson.transform.position);

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
                        _agent.SetDestination(_targetPerson.transform.position);
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
                    if (unit.Data.structureType == Simulation.Data.StructureType.Floor || unit.Data.structureName.ToLower().Contains("stair"))
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
            if (_agent != null) _agent.enabled = false;
            
            // ขยับตัวลงไปใต้ดิน
            transform.position += new Vector3(0, undergroundYOffset, 0);
            
            // ดันตัวไปข้างหน้าเล็กน้อยเพื่อให้พ้นจุดเริ่มกำแพง
            transform.position += transform.forward * 0.5f;

            if (_rb != null) _rb.isKinematic = true;
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void MoveUnderground()
        {
            if (_targetPerson == null) return;

            // คำนวณทิศทางไปยังเป้าหมาย (ในระนาบ XZ)
            Vector3 targetPos = _targetPerson.transform.position;
            Vector3 moveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            Vector3 moveDir = (moveTarget - transform.position).normalized;

            // 1. เช็คว่าพ้นสิ่งกีดขวางหรือยัง (ยิง Ray ขึ้นไปหา Layer Structure)
            // ยิงขึ้นไปสูงหน่อยเพื่อให้พ้น Collider พื้นของตัวมันเอง
            bool isBlockedAbove = UnityEngine.Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.up, out RaycastHit hitUp, 2.5f, LayerMask.GetMask("Structure"));

            if (!isBlockedAbove)
            {
                // ไม่เจออะไรขวางข้างบนแล้ว — โผล่ขึ้นมา!
                Surface();
                return;
            }

            // 2. ถ้าเจอพื้น (Floor) ข้างบน ให้ขุดทำความเสียหาย
            StructureUnit unitAbove = hitUp.collider.GetComponentInParent<StructureUnit>();
            if (unitAbove != null && unitAbove.Data != null && unitAbove.Data.structureType == Simulation.Data.StructureType.Floor)
            {
                _digTimer += Time.deltaTime;
                if (_digTimer >= digInterval)
                {
                    _digTimer = 0f;
                    var stress = unitAbove.GetComponent<Simulation.Physics.StructuralStress>();
                    if (stress != null) stress.ApplyMaxHPDamage(floorDigDamage);
                    else unitAbove.TakeMaxHPDamage(floorDigDamage);
                }
            }

            // 3. ขยับตัวไปข้างหน้าใต้ดิน
            float currentSpeed = (unitAbove != null) ? digSpeed : moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, currentSpeed * Time.deltaTime);
            
            if (moveDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(moveDir);
        }

        private void Surface()
        {
            _currentState = DiggerState.Surface;
            
            // กลับขึ้นมาที่ระดับพื้นปกติ
            transform.position -= new Vector3(0, undergroundYOffset, 0);

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
