using UnityEngine;
using System.Collections.Generic;
using Simulation.Building;
using Simulation.Character;

namespace Simulation.Mission
{
    /// <summary>
    /// มังกรพ่นไฟ — มังกร "เกิดด้านข้าง" แล้วพ่นไฟเป็นลำเข้าหาสิ่งก่อสร้าง
    /// - ไฟถูกบังได้: ยิงเป็นลำตรง ถ้ามีอะไรขวางก่อน ไฟจะหยุดที่ชิ้นนั้น (ของด้านหลังปลอดภัย)
    /// - ของที่โดนไฟจะ "ติดไฟ" (เลือดลดต่อเนื่อง + มี VFX ไฟเกาะ)
    /// - ถ้าเป็นไม้ (วัสดุ flammable) ไฟจะลามไปยังชิ้นไม้ข้างเคียง
    /// </summary>
    public class DragonFireDisaster : DisasterBase
    {
        private class BurnState
        {
            public float timeLeft;
            public float spreadTimer;
            public GameObject vfx;
        }

        private GameObject _dragon;
        private float _time;
        private Vector3 _sideOffset; // ตำแหน่งด้านข้างที่มังกรลอยอยู่ (relative กับ center)

        private readonly Dictionary<StructureUnit, BurnState> _burning = new Dictionary<StructureUnit, BurnState>();
        private readonly List<StructureUnit> _toRemove = new List<StructureUnit>();
        private readonly List<StructureUnit> _pendingIgnite = new List<StructureUnit>();

        // ปรับจูน
        private const float BurnDuration = 6f;          // ไฟติดอยู่กี่วินาทีต่อการจุดหนึ่งครั้ง
        private const float SpreadInterval = 1.2f;      // ไม้ลามไฟทุกๆ กี่วินาที
        private const float SpreadRadius = 2.5f;        // รัศมีการลามไปชิ้นข้างเคียง
        private const float BreathSphereRadius = 1.2f;  // ความหนาของลำไฟ

        public DragonFireDisaster(DisasterData data, MonoBehaviour runner) : base(data, runner) { }

        protected override void OnStart()
        {
            _time = 0f;

            // มังกรเกิด "ด้านข้าง" (ซ้าย) ยกสูงเล็กน้อย แล้วหันหน้าเข้าหากลางแผนที่
            _sideOffset = new Vector3(-30f, 12f, 0f);

            if (data.dragonPrefab != null)
            {
                Vector3 startPos = data.centerOffset + _sideOffset;
                _dragon = Object.Instantiate(data.dragonPrefab, startPos, Quaternion.identity);
                spawnedVFX.Add(_dragon);
            }
        }

        protected override void OnUpdate(float dt)
        {
            _time += dt;

            UpdateDragon(dt);
            BreatheFire(dt);
            UpdateBurning(dt);
        }

        protected override void OnStop()
        {
            // เก็บกวาด VFX ไฟทั้งหมด
            foreach (var kv in _burning)
            {
                if (kv.Value != null && kv.Value.vfx != null) Object.Destroy(kv.Value.vfx);
            }
            _burning.Clear();
            _toRemove.Clear();
            _pendingIgnite.Clear();
        }

        // มังกรลอยอยู่ด้านข้าง โยกตัวเบาๆ และหันหน้าเข้าหาเป้า
        private void UpdateDragon(float dt)
        {
            if (_dragon == null) return;

            Vector3 basePos = data.centerOffset + _sideOffset;
            float bob = Mathf.Sin(_time * 1.5f) * 1.5f;   // ลอยขึ้นลง
            float sway = Mathf.Sin(_time * 0.7f) * 3f;    // แกว่งซ้ายขวา (แกน Z)
            Vector3 pos = basePos + new Vector3(0f, bob, sway);
            _dragon.transform.position = pos;

            Vector3 look = data.centerOffset - pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                _dragon.transform.rotation = Quaternion.LookRotation(look.normalized);
        }

        // พ่นไฟเป็นลำตรง ถูกบังได้
        private void BreatheFire(float dt)
        {
            if (_dragon == null) return;

            Vector3 mouth = _dragon.transform.position + _dragon.transform.forward * 2f;

            // เล็งไปกลางแผนที่ พร้อมกวาดขึ้น-ลง และแพนซ้าย-ขวา เพื่อให้ลำไฟสาดทั่วตัวอาคาร
            float panRange = data.radius > 0f ? data.radius * 0.5f : 8f;
            Vector3 aim = data.centerOffset;
            aim.y += Mathf.Sin(_time * 0.8f) * 4f;
            aim.z += Mathf.Sin(_time * 0.5f) * panRange;

            Vector3 dir = aim - mouth;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            float range = data.radius > 0f ? data.radius : 30f;

            // ยิงลำไฟ แล้วหา "สิ่งที่บังก่อน" (เรียงตามระยะ)
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(
                mouth, BreathSphereRadius, dir, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                Collider col = hit.collider;
                if (col == null) continue;

                // ข้ามตัวมังกรเอง
                if (_dragon != null && col.transform.IsChildOf(_dragon.transform)) continue;

                // โครงสร้าง = ตัวบังไฟ -> ติดไฟ + โดนดาเมจ แล้ว "หยุด" (ของด้านหลังปลอดภัย)
                StructureUnit su = col.GetComponentInParent<StructureUnit>();
                if (su != null)
                {
                    Ignite(su);
                    DamageStructure(su, data.intensity * dt);
                    return;
                }

                // คนก็บังไฟได้ -> โดนเผา แล้วหยุด
                PersonAI person = col.GetComponentInParent<PersonAI>();
                if (person != null)
                {
                    DamagePerson(person, data.intensity * dt);
                    return;
                }

                // อย่างอื่น (พื้นดิน/กำแพงฉาก) = บังไฟเช่นกัน -> หยุด
                return;
            }
        }

        // จุดไฟให้โครงสร้าง
        private void Ignite(StructureUnit unit)
        {
            if (unit == null) return;

            if (_burning.TryGetValue(unit, out var existing))
            {
                existing.timeLeft = BurnDuration; // ต่ออายุไฟ
                return;
            }

            var st = new BurnState { timeLeft = BurnDuration, spreadTimer = SpreadInterval };

            // สร้าง VFX ไฟเกาะติดชิ้นส่วน (ใช้ fireballPrefab เป็นตัวแทนเปลวไฟ)
            if (data.fireballPrefab != null)
            {
                GameObject vfx = Object.Instantiate(
                    data.fireballPrefab, unit.transform.position, Quaternion.identity, unit.transform);

                // ถอด Collider/Rigidbody ออก ไม่ให้ไปกวน physics หรือบังลำไฟครั้งต่อไป
                foreach (var c in vfx.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                foreach (var rb in vfx.GetComponentsInChildren<Rigidbody>()) Object.Destroy(rb);
                st.vfx = vfx;
            }

            _burning[unit] = st;
        }

        // อัปเดตไฟที่กำลังลุก: ดาเมจต่อเนื่อง + ลามถ้าเป็นไม้
        private void UpdateBurning(float dt)
        {
            if (_burning.Count == 0) return;

            _toRemove.Clear();
            _pendingIgnite.Clear();

            foreach (var kv in _burning)
            {
                StructureUnit unit = kv.Key;
                BurnState st = kv.Value;

                if (unit == null)
                {
                    if (st.vfx != null) Object.Destroy(st.vfx);
                    _toRemove.Add(unit);
                    continue;
                }

                // ดาเมจไฟต่อเนื่อง (ไม้คูณ burnDamageMultiplier ให้ไหม้เร็ว)
                float mul = (unit.CurrentMaterial != null)
                    ? Mathf.Max(0.1f, unit.CurrentMaterial.burnDamageMultiplier) : 1f;
                DamageStructure(unit, data.intensity * mul * dt);

                // ลามไฟ - เฉพาะวัสดุที่ติดไฟได้ (ไม้)
                bool flammable = unit.CurrentMaterial != null && unit.CurrentMaterial.flammable;
                if (flammable)
                {
                    st.spreadTimer -= dt;
                    if (st.spreadTimer <= 0f)
                    {
                        st.spreadTimer = SpreadInterval;
                        CollectSpreadTargets(unit); // เก็บไว้จุดทีหลัง (กันแก้ dict ระหว่างวน)
                    }
                }

                st.timeLeft -= dt;
                if (st.timeLeft <= 0f)
                {
                    if (st.vfx != null) Object.Destroy(st.vfx);
                    _toRemove.Add(unit);
                }
            }

            // จุดไฟชิ้นใหม่ที่ลามไป (ทำหลังวน เพื่อไม่ให้แก้ Dictionary ระหว่าง iterate)
            foreach (var u in _pendingIgnite) Ignite(u);

            // ลบไฟที่ดับ/ชิ้นที่หายไป
            foreach (var u in _toRemove) _burning.Remove(u);
        }

        // หาชิ้นไม้ข้างเคียงที่ยังไม่ติดไฟ เพื่อจะลามไป
        private void CollectSpreadTargets(StructureUnit source)
        {
            var near = GetStructuresInRadius(source.transform.position, SpreadRadius);
            foreach (var n in near)
            {
                if (n == null || n == source) continue;
                if (_burning.ContainsKey(n)) continue;
                if (_pendingIgnite.Contains(n)) continue;

                bool flammable = n.CurrentMaterial != null && n.CurrentMaterial.flammable;
                if (flammable) _pendingIgnite.Add(n);
            }
        }
    }
}
