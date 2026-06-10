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
    ///       ยิงเรย์ 5 เส้น (แนวนอน 3 + แนวตั้ง 2) เพื่อให้เกิด "บังบางส่วน" และให้กำแพงเตี้ย
    ///       บังส่วนล่างของชิ้นสูงได้ — ผล cache ไว้ช่วงสั้นๆ เพื่อลดจำนวนเรย์ต่อวินาที ~5 เท่า
    ///
    ///   • ApplyWindForceDistributed (req1, req3):
    ///       กระจายแรงลมของชิ้นหนึ่งไปยัง "ตัวค้ำ" ที่เชื่อมผ่าน Joint ทั้งสองทาง:
    ///         - Joint ที่ชิ้นนี้เป็นเจ้าของ (เกาะของที่วางก่อน)
    ///         - Joint ขาเข้า (ของที่วางทีหลังแล้วมาเกาะชิ้นนี้ เช่น ตัวค้ำที่เสริม "ด้านหลัง")
    ///       โดยถ่วงน้ำหนักให้ตัวค้ำ "ปลายลม/ด้านหลัง" รับมากกว่า → แรงไหลลงโครงสร้างด้านหลัง
    ///       ก่อนที่ข้อต่อหน้าจะรับไม่ไหวแล้วหลุด
    ///       — รวมแรงคงที่ (อนุรักษ์แรงรวมต่อชิ้น = ไม่ทำให้ระบบระเบิด)
    ///       — บันได (มี WindDeflector) ส่งแรงลงโครงสร้างได้มากกว่า และแปลงแรงผลักส่วนตัวเอง
    ///         บางส่วนเป็น "แรงกดลง" (ลมถูกปัดขึ้น → บันไดถูกกดให้นิ่ง ไม่ถูกผลักล้ม)
    /// </summary>
    public static class WindResponse
    {
        // buffer ใช้ซ้ำ กัน GC ตอนยิงเรย์/กระจายแรงทุกเฟรม
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private static readonly List<Rigidbody> _supportScratch = new List<Rigidbody>(8);
        private static readonly List<float> _weightScratch = new List<float>(8);

        // ── cache ผล occlusion ต่อชิ้น (ลดจำนวน raycast) ──
        private struct OccEntry { public float value; public float time; }
        private static readonly Dictionary<int, OccEntry> _occCache = new Dictionary<int, OccEntry>(64);
        private const float OCC_CACHE_DURATION = 0.15f;   // วินาที — สั้นพอที่ผู้เล่นไม่เห็นดีเลย์

        // ── cache "joint ขาเข้า" (ของอื่นที่มาเกาะชิ้นนี้) — rebuild เป็นช่วงๆ ──
        private struct SupportRef { public Rigidbody rb; public StructuralStress stress; }
        private static readonly Dictionary<Rigidbody, List<SupportRef>> _reverseSupports =
            new Dictionary<Rigidbody, List<SupportRef>>(64);
        private static float _nextReverseRebuild = -1f;
        private const float REVERSE_CACHE_INTERVAL = 0.5f; // วินาที

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

            // cache: ตำแหน่งโครงสร้างแทบไม่ขยับในช่วง 0.15s → ใช้ค่าเดิมได้
            int key = unit.GetInstanceID();
            float now = Time.time;
            if (_occCache.TryGetValue(key, out OccEntry entry) && now - entry.time < OCC_CACHE_DURATION)
                return entry.value;

            float value = ComputeOcclusion(unit, windDir.normalized, occlusionDistance, maxOcclusion);

            if (_occCache.Count > 2048) _occCache.Clear(); // กัน dict โตสะสมข้ามรอบเล่น
            _occCache[key] = new OccEntry { value = value, time = now };
            return value;
        }

        private static float ComputeOcclusion(StructureUnit unit, Vector3 windDir,
                                              float occlusionDistance, float maxOcclusion)
        {
            Vector3 center = unit.transform.position;
            var col = unit.GetComponentInChildren<Collider>();

            // แกนนอนที่ตั้งฉากกับลม
            Vector3 perp = Vector3.Cross(Vector3.up, windDir);
            if (perp.sqrMagnitude < 1e-4f) perp = Vector3.Cross(Vector3.forward, windDir);
            perp = perp.sqrMagnitude > 1e-6f ? perp.normalized : Vector3.right;

            if (col == null)
            {
                // ไม่มี collider → ยิงเส้นเดียวจากจุดกึ่งกลาง
                return IsBlockedUpwind(unit, center, -windDir, occlusionDistance)
                    ? Mathf.Lerp(1f, 1f - Mathf.Clamp01(maxOcclusion), 1f)
                    : 1f;
            }

            center = col.bounds.center;
            Vector3 e = col.bounds.extents;

            // ครึ่งความกว้างของ AABB ตามแกน perp (support projection) และครึ่งความสูง
            float halfW = Mathf.Abs(e.x * perp.x) + Mathf.Abs(e.y * perp.y) + Mathf.Abs(e.z * perp.z);
            halfW = Mathf.Max(0.25f, halfW);
            float halfH = Mathf.Max(0.25f, e.y);

            // ยิง 5 เส้นไปทาง "เหนือลม" (-windDir):
            //   แนวนอน: กลาง, ±0.6*halfW   (บังซ้าย/ขวาบางส่วน)
            //   แนวตั้ง: ±0.5*halfH         (กำแพงเตี้ยบังส่วนล่างของชิ้นสูงได้)
            int blocked = 0;
            const int total = 5;
            Vector3 upwind = -windDir;

            if (IsBlockedUpwind(unit, center, upwind, occlusionDistance)) blocked++;
            if (IsBlockedUpwind(unit, center + perp * (halfW * 0.6f), upwind, occlusionDistance)) blocked++;
            if (IsBlockedUpwind(unit, center - perp * (halfW * 0.6f), upwind, occlusionDistance)) blocked++;
            if (IsBlockedUpwind(unit, center + Vector3.up * (halfH * 0.5f), upwind, occlusionDistance)) blocked++;
            if (IsBlockedUpwind(unit, center - Vector3.up * (halfH * 0.5f), upwind, occlusionDistance)) blocked++;

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
        /// ใส่แรงลมให้ชิ้น โดยแบ่งบางส่วนไปยัง "ตัวค้ำ" ที่เชื่อมด้วย Joint (ทั้งขาออกและขาเข้า)
        /// ถ่วงน้ำหนักให้ตัวค้ำฝั่ง "ปลายลม/ด้านหลัง" รับมากกว่า
        /// แรงรวมที่ใส่ต่อชิ้น = windForce (อนุรักษ์) เพียงแต่กระจายลงโครงสร้าง
        /// loadSpread = สัดส่วนที่ส่งไปตัวค้ำ (0 = รับเองหมด). บันไดจะใช้ค่าที่สูงกว่าถ้าตั้งไว้
        /// </summary>
        public static void ApplyWindForceDistributed(StructureUnit unit, Vector3 windForce,
                                                     float loadSpread, ForceMode mode)
        {
            if (unit == null) return;
            if (windForce.sqrMagnitude < 1e-8f) return;

            Rigidbody rb = unit.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic) return; // ชิ้น kinematic ไม่รับแรงลม (เหมือนเดิม)

            Vector3 windDir = windForce.normalized;

            // (req3) บันไดส่งแรงลงโครงสร้างได้มากกว่า
            var deflector = unit.GetComponent<WindDeflector>();
            float spread = (deflector != null)
                ? Mathf.Max(loadSpread, deflector.WindForceTransmission)
                : loadSpread;
            spread = Mathf.Clamp01(spread);

            // หา "ตัวค้ำ" จาก Joint ทั้งสองทาง (ขาออก + ขาเข้า)
            CollectSupports(unit, rb);

            if (spread <= 0f || _supportScratch.Count == 0)
            {
                // ไม่กระจาย หรือไม่มีตัวค้ำ (เช่นยึดกับพื้นโดยตรง) → รับแรงเองทั้งหมด
                rb.AddForce(DeflectSelfForce(deflector, windForce, windDir), mode);
                return;
            }

            // ส่วนของตัวเอง (บันไดที่หันรับลม: แปลงบางส่วนเป็นแรงกดลง — เสถียรขึ้น)
            Vector3 selfPortion = windForce * (1f - spread);
            rb.AddForce(DeflectSelfForce(deflector, selfPortion, windDir), mode);

            // (req1) ส่วนที่กระจายไปตัวค้ำ — ถ่วงน้ำหนักให้ฝั่ง "ตามทิศลม" (ด้านหลัง) รับมากกว่า
            Vector3 sharedPortion = windForce * spread;
            Vector3 myPos = rb.worldCenterOfMass;

            _weightScratch.Clear();
            float weightSum = 0f;
            for (int i = 0; i < _supportScratch.Count; i++)
            {
                var s = _supportScratch[i];
                float w = 0.25f; // ฐานขั้นต่ำ — ทุกตัวค้ำมีส่วนช่วยเสมอ (รวมพื้น/เสาด้านล่าง)
                if (s != null)
                {
                    Vector3 d = s.worldCenterOfMass - myPos;
                    if (d.sqrMagnitude > 1e-6f)
                        w += Mathf.Max(0f, Vector3.Dot(d.normalized, windDir)); // ด้านหลังได้น้ำหนักเพิ่ม
                }
                _weightScratch.Add(w);
                weightSum += w;
            }
            if (weightSum <= 1e-6f) { rb.AddForce(sharedPortion, mode); return; } // กันหารศูนย์

            for (int i = 0; i < _supportScratch.Count; i++)
            {
                var s = _supportScratch[i];
                if (s == null || s.isKinematic || !s.gameObject.activeInHierarchy) continue;
                s.AddForce(sharedPortion * (_weightScratch[i] / weightSum), mode);
                // ถ้าตัวค้ำเป็น ground anchor (kinematic) → แรงถูกดูดซับลงพื้นตามจริง
            }
        }

        /// <summary>
        /// (req3) บันไดที่หันรับลมตรง: ลม "ถูกปัดขึ้น" → แรงปฏิกิริยากดบันไดลงโครงสร้าง
        /// แปลงสัดส่วนหนึ่งของแรงผลักแนวลมเป็นแรงกดลง (ขนาดรวมไม่เกินของเดิม)
        /// </summary>
        private static Vector3 DeflectSelfForce(WindDeflector deflector, Vector3 force, Vector3 windDir)
        {
            if (deflector == null) return force;

            float align = deflector.GetSelfAlignment(windDir);
            if (align <= 0f) return force;

            float f = Mathf.Clamp01(deflector.DeflectDownforceFraction) * align;
            return force * (1f - f) + Vector3.down * (force.magnitude * f);
        }

        /// <summary>
        /// รวบรวม Rigidbody ของตัวค้ำที่เชื่อมกับชิ้นนี้ ทั้งสองทิศทาง:
        ///   1) Joint บนชิ้นนี้ → connectedBody (ของที่วางก่อนแล้วชิ้นนี้ไปเกาะ)
        ///   2) Joint ขาเข้า: ของที่วางทีหลังแล้วมาเกาะชิ้นนี้ (เช่น ตัวค้ำเสริมด้านหลัง)
        ///      — BuildingSystem สร้าง Joint บนชิ้นใหม่ชี้มาหาชิ้นเก่า จึงต้องมี reverse lookup
        /// ผลลัพธ์ถูกเก็บใน _supportScratch (ไม่ alloc)
        /// </summary>
        private static void CollectSupports(StructureUnit unit, Rigidbody selfRb)
        {
            _supportScratch.Clear();

            // 1) ขาออก — สดเสมอ (joint บนตัวเอง)
            var joints = unit.GetComponents<Joint>();
            for (int i = 0; i < joints.Length; i++)
            {
                var j = joints[i];
                if (j == null || j.connectedBody == null) continue;
                if (!_supportScratch.Contains(j.connectedBody))
                    _supportScratch.Add(j.connectedBody);
            }

            // 2) ขาเข้า — จาก cache (rebuild ทุก REVERSE_CACHE_INTERVAL วินาที)
            RebuildReverseMapIfStale();
            if (_reverseSupports.TryGetValue(selfRb, out List<SupportRef> incoming))
            {
                for (int i = 0; i < incoming.Count; i++)
                {
                    var s = incoming[i];
                    if (s.rb == null || !s.rb.gameObject.activeInHierarchy) continue;
                    // ชิ้นที่หลุด/พังไปแล้วระหว่างรอบ rebuild → ไม่ใช่ตัวค้ำแล้ว
                    if (s.stress != null && (s.stress.IsBroken || s.stress.IsDetached)) continue;
                    if (!_supportScratch.Contains(s.rb)) _supportScratch.Add(s.rb);
                }
            }
        }

        private static void RebuildReverseMapIfStale()
        {
            float now = Time.time;
            if (now < _nextReverseRebuild) return;
            _nextReverseRebuild = now + REVERSE_CACHE_INTERVAL;

            _reverseSupports.Clear();
            var allJoints = Object.FindObjectsByType<Joint>(FindObjectsSortMode.None);
            for (int i = 0; i < allJoints.Length; i++)
            {
                var j = allJoints[i];
                if (j == null || j.connectedBody == null) continue;

                var ownerRb = j.GetComponent<Rigidbody>(); // Joint ต้องมี Rigidbody บน GO เดียวกันเสมอ
                if (ownerRb == null || ownerRb == j.connectedBody) continue;

                if (!_reverseSupports.TryGetValue(j.connectedBody, out List<SupportRef> list))
                {
                    list = new List<SupportRef>(4);
                    _reverseSupports[j.connectedBody] = list;
                }

                bool dup = false;
                for (int k = 0; k < list.Count; k++)
                    if (list[k].rb == ownerRb) { dup = true; break; }
                if (!dup)
                    list.Add(new SupportRef { rb = ownerRb, stress = ownerRb.GetComponent<StructuralStress>() });
            }
        }

        /// <summary>
        /// ล้าง cache ทั้งหมด — เรียกตอนเริ่ม/หยุดจำลอง (เช่นจาก SimulationManager) เพื่อบังคับคำนวณใหม่
        /// ไม่เรียกก็ได้: cache หมดอายุเองตามเวลา
        /// </summary>
        public static void ClearCaches()
        {
            _occCache.Clear();
            _reverseSupports.Clear();
            _nextReverseRebuild = -1f;
        }
    }
}
