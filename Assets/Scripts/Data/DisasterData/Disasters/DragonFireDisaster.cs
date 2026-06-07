using UnityEngine;
using Simulation.Building;
using System.Collections.Generic; // Added for fire list

namespace Simulation.Mission
{
    /// <summary>
    /// มังกรพ่นไฟ — มังกรบินผ่านและพ่นลูกไฟใส่โครงสร้าง
    /// ใช้ dragonPrefab สำหรับโมเดลมังกร, fireballPrefab สำหรับลูกไฟ
    /// intensity เป็นดาเมจลูกไฟ
    /// </summary>
    public class DragonFireDisaster : DisasterBase
    {
        private GameObject _dragon;
        private float _fireTimer;
        private float _flyT; // 0-1 progress ของการบินผ่าน
        private List<GameObject> _activeFires = new List<GameObject>(); // เก็บไฟที่กำลังลุกอยู่
        private float _fireDuration = 5f; // เวลาไฟอยู่ก่อนดับ

        public DragonFireDisaster(DisasterData data, MonoBehaviour runner) : base(data, runner) { }

        protected override void OnStart()
        {
            _fireTimer = 0f;
            _flyT = 0f;

            // Spawn มังกร
            if (data.dragonPrefab != null)
            {
                Vector3 startPos = data.centerOffset + new Vector3(-30f, 15f, 0f);
                _dragon = Object.Instantiate(data.dragonPrefab, startPos, Quaternion.identity);
                spawnedVFX.Add(_dragon);
            }
        }

        protected override void OnUpdate(float dt)
        {
            // มังกรบินผ่าน
            _flyT += dt / data.duration;
            _flyT = Mathf.Clamp01(_flyT);

            if (_dragon != null)
            {
                // บินจากซ้ายไปขวา + วนขึ้นลง
                Vector3 startPos = data.centerOffset + new Vector3(-30f, 15f, 0f);
                Vector3 endPos = data.centerOffset + new Vector3(30f, 15f, 0f);
                Vector3 pos = Vector3.Lerp(startPos, endPos, _flyT);
                pos.y += Mathf.Sin(_flyT * Mathf.PI * 3f) * 3f; // บินขึ้นลง
                _dragon.transform.position = pos;

                // หันหน้าไปทิศที่บิน
                Vector3 dir = (endPos - startPos).normalized;
                if (dir != Vector3.zero)
                {
                    _dragon.transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // สร้างไฟรอบมังกรเป็นช่วงเวลา
            _fireTimer += dt;
            float fireInterval = 2f; // สร้างไฟทุก 2 วินาที
            if (_fireTimer >= fireInterval)
            {
                _fireTimer -= fireInterval;
                EmitFire();
            }

            // ดาเมจต่อเนื่องจากการบินของมังกร (field damage)
            if (data.damagePerSecond > 0f)
            {
                var structures = GetStructuresInRadius(data.centerOffset, data.radius);
                foreach (var unit in structures)
                {
                    // ดาเมจทั่วไปจากความรุนแรงของมังกร
                    DamageStructure(unit, data.damagePerSecond * dt * 0.3f);
                }
            }

            // ดาเมจจากไฟที่กำลังลุกอยู่
            ApplyActiveFireDamage(dt);
        }

        // สร้างไฟพื้นฐาน (ไม่มีลูกไฟ) ที่ตำแหน่งมังกร
        private void EmitFire()
        {
            if (_dragon == null) return;

            Vector3 firePos = _dragon.transform.position + Vector3.down * 2f;

            // ใช้ fireballPrefab เป็นตัวแทนของ fire area ถ้าหากไม่มี fireAreaPrefab
            GameObject fireObj = null;
            if (data.fireballPrefab != null)
            {
                fireObj = Object.Instantiate(data.fireballPrefab, firePos, Quaternion.identity);
                // ปิดการเคลื่อนที่: ไม่ใส่แรง, ปิด gravity
                Rigidbody rb = fireObj.GetComponent<Rigidbody>();
                if (rb == null) rb = fireObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            else
            {
                // สร้าง placeholder ว่างเปล่า
                fireObj = new GameObject("DragonFireArea");
                fireObj.transform.position = firePos;
            }

            // ทำลายหลังจากช่วงเวลากำหนด
            Object.Destroy(fireObj, _fireDuration);
            _activeFires.Add(fireObj);
        }

        // ดาเมจต่อเนื่องจากไฟที่กำลังลุกอยู่
        private void ApplyActiveFireDamage(float dt)
        {
            foreach (var fire in _activeFires)
            {
                if (fire == null) continue;
                var structures = GetStructuresInRadius(fire.transform.position, data.radius > 0f ? data.radius : 5f);
                foreach (var unit in structures)
                {
                    // เพิ่มดาเมจต่อเนื่องแบบไฟ; ถ้าเป็นไม้เพิ่ม multiplier
                    float dmgMultiplier = 0.5f;
                    if (unit != null && unit.CompareTag("Wood"))
                    {
                        dmgMultiplier *= 2f; // ไม้ลามต่อเนื่องเป็นสองเท่า
                    }
                    DamageStructure(unit, data.intensity * dt * dmgMultiplier);
                    // TODO: หาก unit มี tag หรือ type "Wood" ให้เพิ่มโอกาสการลามต่อเนื่อง
                }
                // ดาเมจต่อคน (เช่น zombie bite)
                var people = GetAllPeople();
                foreach (var person in people)
                {
                    if (person == null) continue;
                    // เช็คระยะจาก fire
                    if (Vector3.Distance(person.transform.position, fire.transform.position) <= (data.radius > 0f ? data.radius : 5f))
                    {
                        // ใช้ intensity เป็นดาเมจต่อคน
                        DamagePerson(person, data.intensity * dt);
                    }
                }
            }
        }
    }
}
