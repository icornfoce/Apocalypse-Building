using UnityEngine;
using UnityEngine.AI;
using Simulation.Character;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Balloon Zombie — บินไปหา NPC โดยตรง (Inherit from ZombieAI)
    /// - บินเหนือกำแพง ไม่สนกำแพง
    /// - ถ้าถึงตัว NPC แต่มีพื้นกั้นอยู่ข้างล่าง จะกินพื้นเพื่อลงไปหา
    /// </summary>
    public class BalloonZombieAI : ZombieAI
    {
        [Header("Balloon Settings")]
        public float flySpeed = 2.0f;
        public float descendSpeed = 1.0f;
        public float flyHeight = 8f;
        public float floorEatDamage = 20f;
        public float floorEatInterval = 0.8f;

        [Header("Balloon Visuals")]
        public GameObject balloonObject;
        public GameObject balloonBurstVFX;

        private bool _hasLanded = false;
        private float _floorEatTimer;

        protected override void Awake()
        {
            base.Awake();
            
            // เริ่มต้นแบบบิน (Kinematic)
            if (_agent != null) _agent.enabled = false;
            if (_rb != null)
            {
                _rb.useGravity = false;
                _rb.isKinematic = true;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;
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
        }

        protected override void Update()
        {
            if (_isDead) return;

            if (!_hasLanded)
            {
                // เฟสบิน: หาเป้าหมายและบินไปเหนือเป้าหมาย
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
            else
            {
                // ลงพื้นแล้ว: ใช้ Logic ของ ZombieAI ปกติ
                base.Update();
            }
        }

        private void FlyTowardsTarget()
        {
            Vector3 targetPos = _targetPerson.transform.position;
            Vector3 aboveTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            float horizontalDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPos.x, targetPos.z));

            if (horizontalDist > 1.0f)
            {
                // ยังไม่ถึงเป้าหมาย (แนวนอน)
                transform.position = Vector3.MoveTowards(transform.position, aboveTarget, flySpeed * Time.deltaTime);
                Vector3 dir = (aboveTarget - transform.position).normalized;
                if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
            }
            else
            {
                // ถึงเหนือเป้าหมายแล้ว -> พยายามลง
                TryDescend();
            }
        }

        private void TryDescend()
        {
            // เช็คว่ามีพื้นขวางข้างล่างไหม
            StructureUnit floorUnit = null;
            if (UnityEngine.Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.0f, LayerMask.GetMask("Structure")))
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                if (unit != null && unit.Data != null && unit.Data.structureType == Simulation.Data.StructureType.Floor)
                {
                    floorUnit = unit;
                }
            }

            if (floorUnit != null)
            {
                // มีพื้นขวาง -> กินพื้น
                _floorEatTimer += Time.deltaTime;
                if (_floorEatTimer >= floorEatInterval)
                {
                    _floorEatTimer = 0f;
                    var stress = floorUnit.GetComponent<Simulation.Physics.StructuralStress>();
                    if (stress != null) stress.ApplyExternalDamage(floorEatDamage);
                    else floorUnit.TakeDamage(floorEatDamage);
                }
            }
            else
            {
                // ไม่มีพื้นขวาง -> ลง
                float targetY = _targetPerson.transform.position.y;
                Vector3 descendTarget = new Vector3(transform.position.x, targetY, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, descendTarget, descendSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - targetY) < 0.3f)
                {
                    OnLanded();
                }
            }
        }

        private void OnLanded()
        {
            _hasLanded = true;
            
            if (balloonObject != null) balloonObject.SetActive(false);
            if (balloonBurstVFX != null) Instantiate(balloonBurstVFX, transform.position + Vector3.up * 1f, Quaternion.identity);

            // เปิดระบบ NavMesh เพื่อให้เดินเหมือน Zombie ปกติ
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if (_agent != null)
                {
                    _agent.enabled = true;
                    _agent.speed = moveSpeed;
                    _agent.stoppingDistance = attackRange * 0.8f;
                }
                
                if (_rb != null) _rb.isKinematic = true;
                var col = GetComponent<CapsuleCollider>();
                if (col != null) col.isTrigger = true;
            }
            
            Debug.Log($"<color=magenta>[BalloonZombie]</color> Landed and starting ground movement!");
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
    }
}
