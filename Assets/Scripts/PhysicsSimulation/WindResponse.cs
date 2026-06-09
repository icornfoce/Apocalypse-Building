using System.Collections.Generic;
using UnityEngine;
using Simulation.Building;

namespace Simulation.Physics
{
    /// <summary>
    /// ตัวช่วยตอบสนองต่อลม (ใช้ร่วมกับ StrongWindDisaster) — ส่วนขยายของระบบ aerodynamic ปลอม
    ///
    ///   • GetOcclusionMultiplier (req2):
    ///       ถ้ามีโครงสร้างอื่นบังลมอยู่ "ด้านหน้า" (เหนือลม) ของชิ้นนี้ → ลดแรงลม/ดาเมจที่ชิ้นนี้รับ
    ///       ยิงเรย์หลายเส้นเพื่อให้เกิด "บังบางส่วน" (แรงน้อยลง) ไม่ใช่แค่บัง/ไม่บัง
    ///
    ///   • ApplyWindForceDistributed (req1, req3):
    ///       กระจายแรงลมของชิ้นหนึ่งไปยัง "ข้อต่อด้านหลัง" (ตัวค้ำผ่าน Joint.connectedBody)
    ///       เพื่อให้แรงถูกแชร์ลงโครงสร้างก่อนที่ข้อต่อหน้าจะรับไม่ไหวแล้วหลุด
    ///       — รวมแรงคงที่ (อนุรักษ์แรงรวมต่อชิ้น = ไม่ทำให้ระบบระเบิด)
    ///       — บันได (มี WindDeflector) ส่งแรงลงโครงสร้างได้มากกว่า (ช่วยค้ำ/รับลม)
    /// </summary>
    public static class WindResponse
    {
        // buffer ใช้ซ้ำ กัน GC ตอนยิงเรย์ทุกเฟรม
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private static readonly List<Rigidbody> _supportScratch = new List<Rigidbody>(8);

        // ────────────────────────────────────────────────────────────────
        // req2 — Occlusion (ของด้านหน้าบังลมให้ของด้านหลัง)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ตัวคูณแรงลม [0..1] จากการ "ถูกบัง" โดยโครงสร้างด้านหน้า
        /// 1 = ไม่ถูกบัง (โดนเต็ม), ต่ำลง = ถูกบังมาก. windDir ไม่ต้อง normalized มาก่อนก็ได้
        /// </summary>
        public static float GetOcclusionMultiplier(StructureUnit unit, Vector3 windDir,
                                                   float occlusionDistance, float maxOcclusion)
        {
            if (unit == null) return 1f;
            if (windDir.sqrMagnitude < 1e-6f || occlusionDistance <= 0f) return 1f;
            windDir = windDir.normalized;

            // จุดอ้างอิง + ครึ่งความกว้างตั้งฉากกับลม (จาก bounds)
            Vector3 center = unit.transform.position;
            float halfW = 0.5f;
            var col = unit.GetComponentInChildren<Collider>();
            if (col != null)
            {
                center = col.bounds.center;
                Vector3 e = col.bounds.extents;

                // แกนนอนที่ตั้งฉากกับลม
                Vector3 perp = Vector3.Cross(Vector3.up, windDir);
                if (perp.sqrMagnitude < 1e-4f) perp = Vector3.Cross(Vector3.forward, windDir);
                perp = perp.sqrMagnitude > 1e-6f ? perp.normalized : Vector3.right;

                // ครึ่งความกว้างของ AABB ตามแกน perp (support projection)
                halfW = Mathf.Abs(e.x * perp.x) + Mathf.Abs(e.y * perp.y) + Mathf.Abs(e.z * perp.z);
                halfW = Mathf.Max(0.25f, halfW);

                return SampleOcclusion(unit, center, perp, halfW, windDir, occlusionDistance, maxOcclusion);
            }

            // ไม่มี collider → ยิงเส้นเดียวจากจุดกึ่งกลาง
            Vector3 perp2 = Vector3.Cross(Vector3.up, windDir);
            if (perp2.sqrMagnitude < 1e-4f) perp2 = Vector3.right;
            return SampleOcclusion(unit, center, perp2.normalized, halfW, windDir, occlusionDistance, maxOcclusion);
        }

        private static float SampleOcclusion(StructureUnit self, Vector3 center, Vector3 perp, float halfW,
                                             Vector3 windDir, float dist, float maxOcclusion)
        {
            // ยิง 3 เส้น (กลาง, ±0.6*halfW) ไปทาง "เหนือลม" (-windDir)
            int blocked = 0;
            const int total = 3;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 origin = center + perp * (i * halfW * 0.6f);
                if (IsBlockedUpwind(self, origin, -windDir, dist)) blocked++;
            }

            float frac = (float)blocked / total;
            return Mathf.Lerp(1f, 1f - Mathf.Clamp01(maxOcclusion), frac);
        }

        private static bool IsBlockedUpwind(StructureUnit self, Vector3 origin, Vector3 dir, float dist)
        {
            int n = UnityEngine.Physics.RaycastNonAlloc(origin, dir, _hitBuffer, dist,
                        ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = _hitBuffer[i].collider;
                if (col == null) continue;

                // ใช้ตัวตนของ StructureUnit ในการข้าม "ตัวเอง" (กันปัญหา parenting/ชื่อ root)
                var other = col.GetComponentInParent<StructureUnit>();
                if (other == null) continue;       // ไม่ใช่โครงสร้าง (พื้น/prop) → ไม่ถือว่าบัง
                if (other == self) continue;        // ตัวเอง

                // ถ้าชิ้นนั้นพังไปแล้ว ไม่นับว่าบัง (ปกติจะถูกปิด collider อยู่แล้ว แต่กันไว้)
                var st = other.GetComponent<StructuralStress>();
                if (st != null && st.IsBroken) continue;

                return true; // เจอโครงสร้างบังอยู่ด้านหน้า
            }
            return false;
        }

        // ────────────────────────────────────────────────────────────────
        // req1 & req3 — กระจายแรงลมไปยังข้อต่อ/ตัวค้ำด้านหลัง
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ใส่แรงลมให้ชิ้น โดยแบ่งบางส่วนไปยัง "ตัวค้ำด้านหลัง" (ผ่าน Joint.connectedBody)
        /// แรงรวมที่ใส่ต่อชิ้น = windForce (อนุรักษ์) เพียงแต่กระจายลงโครงสร้าง
        /// loadSpread = สัดส่วนที่ส่งไปตัวค้ำ (0 = รับเองหมด). บันไดจะใช้ค่าที่สูงกว่าถ้าตั้งไว้
        /// </summary>
        public static void ApplyWindForceDistributed(StructureUnit unit, Vector3 windForce,
                                                     float loadSpread, ForceMode mode)
        {
            if (unit == null) return;

            Rigidbody rb = unit.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic) return; // ชิ้น kinematic ไม่รับแรงลม (เหมือนเดิม)

            // (req3) บันไดส่งแรงลงโครงสร้างได้มากกว่า
            var deflector = unit.GetComponent<WindDeflector>();
            float spread = (deflector != null)
                ? Mathf.Max(loadSpread, deflector.WindForceTransmission)
                : loadSpread;
            spread = Mathf.Clamp01(spread);

            // หา "ตัวค้ำด้านหลัง" จากข้อต่อทั้งหมดบนชิ้นนี้
            var supports = GetSupportBodies(unit);

            if (spread <= 0f || supports.Count == 0)
            {
                // ไม่กระจาย หรือไม่มีตัวค้ำ (เช่นยึดกับพื้นโดยตรง) → รับแรงเองทั้งหมด
                rb.AddForce(windForce, mode);
                return;
            }

            // ส่วนของตัวเอง
            rb.AddForce(windForce * (1f - spread), mode);

            // ส่วนที่กระจายไปตัวค้ำ (แบ่งเท่าๆ กัน) — ทำให้ข้อต่อด้านหลังรับภาระแทน
            Vector3 share = windForce * (spread / supports.Count);
            for (int i = 0; i < supports.Count; i++)
            {
                var s = supports[i];
                if (s != null && !s.isKinematic)
                    s.AddForce(share, mode);
                // ถ้าตัวค้ำเป็น ground anchor (kinematic) → แรงถูกดูดซับลงพื้นตามจริง
            }
        }

        /// <summary>รวบรวม Rigidbody ของตัวค้ำที่ชิ้นนี้เกาะอยู่ (ผ่าน Joint.connectedBody)</summary>
        private static List<Rigidbody> GetSupportBodies(StructureUnit unit)
        {
            _supportScratch.Clear();
            var joints = unit.GetComponents<Joint>();
            for (int i = 0; i < joints.Length; i++)
            {
                var j = joints[i];
                if (j == null || j.connectedBody == null) continue;
                if (!_supportScratch.Contains(j.connectedBody))
                    _supportScratch.Add(j.connectedBody);
            }
            return _supportScratch;
        }
    }
}
