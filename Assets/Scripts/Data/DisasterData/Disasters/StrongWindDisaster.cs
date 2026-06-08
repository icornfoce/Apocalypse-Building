using UnityEngine;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// ลมแรง — ผลักโครงสร้างทั้งหมดในทิศทางที่กำหนด
    /// ใช้ windDirection เป็นทิศทาง, intensity เป็นแรงลม
    /// </summary>
    public class StrongWindDisaster : DisasterBase
    {
        public StrongWindDisaster(DisasterData data, MonoBehaviour runner) : base(data, runner) { }

        protected override void OnUpdate(float dt)
        {
            // ทิศลม (normalized) — กันกรณี windDirection เป็นศูนย์ (หาร normalize ไม่ได้)
            Vector3 windDir = data.windDirection.sqrMagnitude > 1e-6f
                ? data.windDirection.normalized
                : Vector3.zero;

            // แรงลม = ทิศทาง * ความรุนแรง * ตัวคูณแรงลม (เพื่อให้ปรับแรงผลักได้กว้างขึ้น)
            Vector3 force = windDir * (data.intensity * data.windForceMultiplier);

            var structures = GetStructuresInRadius(data.centerOffset, data.radius);
            foreach (var unit in structures)
            {
                if (unit == null) continue;

                // ── ระบบ aerodynamic ปลอมของบันได ──
                // บันไดที่หันแกน Z ตรงกับลมจะ "ลดดาเมจ" ให้ตัวเอง และสร้าง "เงาลม"
                // ที่ลดดาเมจ/แรงให้โครงสร้างที่อยู่ปลายลม (ดู WindDeflector.cs)
                Vector3 pos = unit.transform.position;
                float dmgMult   = Simulation.Physics.WindDeflector.GetWindDamageMultiplier(unit, pos, windDir);
                float forceMult = Simulation.Physics.WindDeflector.GetWindForceMultiplier(unit, pos, windDir);

                Rigidbody rb = unit.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForce(force * forceMult, ForceMode.Force);

                    // ลมกระแทกแรงเป็นช่วง (gust)
                    if (Random.value < 0.05f)
                    {
                        rb.AddForce(force * 3f * forceMult, ForceMode.Impulse);
                    }
                }

                // ดาเมจต่อเนื่อง (ลดตามการป้องกันของบันได)
                DamageStructure(unit, data.damagePerSecond * dt * dmgMult);
            }

            // คนก็โดนลมพัด
            var people = GetAllPeople();
            foreach (var person in people)
            {
                if (person == null) continue;
                
                // ถ้าลมแรงพอ (เช่น force > 500) ให้คนปลิวกระเด็น
                if (force.magnitude > 500f)
                {
                    var agent = person.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.enabled)
                    {
                        agent.enabled = false;
                        var col = person.GetComponent<CapsuleCollider>();
                        if (col != null) col.isTrigger = false;
                        
                        Rigidbody rbCheck = person.GetComponent<Rigidbody>();
                        if (rbCheck != null) rbCheck.isKinematic = false;
                    }
                }

                Rigidbody prb = person.GetComponent<Rigidbody>();
                if (prb != null && !prb.isKinematic)
                {
                    prb.AddForce(force * 0.5f, ForceMode.Force);
                }
                
                if (data.peopleDamagePerSecond > 0f)
                {
                    DamagePerson(person, data.peopleDamagePerSecond * dt);
                }
            }
        }
    }
}
