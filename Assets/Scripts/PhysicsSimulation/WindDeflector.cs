using System.Collections.Generic;
using UnityEngine;
using Simulation.Building;

namespace Simulation.Physics
{
    /// <summary>
    /// "ระบบ aerodynamic ปลอม" สำหรับบันได (Stairs) เพื่อต้าน StrongWind.
    ///
    /// แนวคิด (ไม่ใช่ CFD จริง — เป็นการประมาณเชิง gameplay ราคาถูก):
    ///   • ถ้าแกนของบันได (ค่าเริ่มต้น = local +Z / transform.forward ซึ่งเป็นแกนยาวของบันได)
    ///     "หันไปทางเดียวกับลม" บันไดจะเอียงรับลมแล้วปัดลมขึ้น/ข้าม ทำให้:
    ///       1) บันไดเองรับ "ดาเมจจากลม" น้อยลง            (Self protection)
    ///       2) เกิด "เงาลม" (wind shadow) ด้านปลายลม โครงสร้างที่อยู่ในเงานี้รับดาเมจน้อยลง/ไม่โดน
    ///   • ระดับการป้องกันแปรผันตามความตรงของแกนกับทิศลม (alignment): ตรงเป๊ะ = ป้องกันสูงสุด
    ///
    /// วิธีใช้: แปะคอมโพเนนต์นี้บน prefab ของบันได (หรือให้ StructureUnit แปะอัตโนมัติตามชื่อ)
    /// จากนั้น StrongWindDisaster จะ query ตัวคูณดาเมจ/แรงผ่าน static API ด้านล่าง
    /// </summary>
    [DisallowMultipleComponent]
    public class WindDeflector : MonoBehaviour
    {
        // ── Active registry: ให้ disaster วน query ได้เร็ว ไม่ต้อง FindObjects ทุกเฟรม ──
        private static readonly List<WindDeflector> _active = new List<WindDeflector>();
        public static IReadOnlyList<WindDeflector> Active => _active;

        public enum FacingAxis { Forward_Z, Right_X, Up_Y }

        [Header("ทิศที่บันได 'หันรับลม'")]
        [Tooltip("แกน local ที่ถือว่าเป็นทิศรับลมของบันได (ค่าเริ่มต้น = +Z / transform.forward = แกนยาวของบันได)")]
        [SerializeField] private FacingAxis facingAxis = FacingAxis.Forward_Z;

        [Tooltip("กลับทิศแกน (ติ๊กถ้า prefab ของคุณหันด้าน -Z เข้าหาลม)")]
        [SerializeField] private bool invertAxis = false;

        [Tooltip("เริ่มป้องกันเมื่อ dot(แกน, ลม) ≥ ค่านี้ (1 = ตรงเป๊ะ, 0 = ตั้งฉาก). ต่ำกว่านี้ = ไม่ป้องกัน")]
        [Range(0f, 1f)] [SerializeField] private float alignmentThreshold = 0.5f;

        [Header("ป้องกันตัวบันไดเอง (Self)")]
        [Tooltip("ตัวคูณดาเมจของบันไดเมื่อ 'หันตรงกับลมเป๊ะ' (0 = ไม่รับดาเมจลมเลย, 1 = รับเต็ม). " +
                 "ตั้ง 0.1 ถ้าอยากให้ 'เบาลง' แต่ยังพอเสียหายได้")]
        [Range(0f, 1f)] [SerializeField] private float selfMinDamageMultiplier = 0f;

        [Header("เงาลม (ป้องกันโครงสร้างด้านหลัง)")]
        [Tooltip("ความยาวเงาลมไปทางปลายลม (เมตร)")]
        [SerializeField] private float shadowLength = 6f;

        [Tooltip("ครึ่งความกว้างของเงา (เมตร) ตั้งฉากกับทิศลม. ≤ 0 = คำนวณจากขนาด collider ของบันได")]
        [SerializeField] private float shadowHalfWidth = -1f;

        [Tooltip("ตัวคูณดาเมจของโครงสร้างที่อยู่ในเงาเต็มๆ เมื่อบันไดหันตรงกับลมเป๊ะ (0 = ไม่โดนดาเมจ)")]
        [Range(0f, 1f)] [SerializeField] private float shadowMinDamageMultiplier = 0f;

        [Header("แรงผลัก (ทางเลือก)")]
        [Tooltip("ลด 'แรงผลัก' จากลมด้วย ไม่ใช่แค่ดาเมจ (กันบันได/ของหลังบันไดถูกผลักล้ม)")]
        [SerializeField] private bool protectPushForce = true;

        [Tooltip("ตัวคูณแรงผลักเมื่อป้องกันเต็มที่ (0 = ไม่โดนผลัก, 1 = โดนเต็ม)")]
        [Range(0f, 1f)] [SerializeField] private float minForceMultiplier = 0.25f;

        [Header("ส่งแรงรับลมลงโครงสร้าง (req3: บันไดช่วยรับ/ส่งแรง)")]
        [Tooltip("สัดส่วนแรงลมที่บันได 'ส่งต่อ' ลงตัวค้ำ/โครงสร้างด้านหลัง " +
                 "(0 = รับไว้เองหมด, 1 = ส่งลงโครงสร้างเกือบหมด) — ช่วยให้บันไดไม่หลุดง่ายและช่วยค้ำโครงสร้าง")]
        [Range(0f, 1f)] [SerializeField] private float windForceTransmission = 0.6f;

        /// <summary>สัดส่วนแรงลมที่บันไดส่งต่อลงโครงสร้าง (ใช้โดย WindResponse.ApplyWindForceDistributed)</summary>
        public float WindForceTransmission => windForceTransmission;

        // ── cache ──
        private Transform _t;
        private StructureUnit _unit;
        private float _cachedHalfWidth = -1f;

        private void Awake()
        {
            _t = transform;
            _unit = GetComponent<StructureUnit>();
        }

        private void OnEnable()
        {
            if (!_active.Contains(this)) _active.Add(this);
        }

        private void OnDisable()
        {
            _active.Remove(this);
        }

        /// <summary>แกนรับลมในพิกัดโลก (normalized)</summary>
        private Vector3 AxisWorld()
        {
            if (_t == null) _t = transform;

            Vector3 a;
            switch (facingAxis)
            {
                case FacingAxis.Right_X: a = _t.right;   break;
                case FacingAxis.Up_Y:    a = _t.up;      break;
                default:                 a = _t.forward; break;
            }
            if (invertAxis) a = -a;
            return a.sqrMagnitude > 1e-6f ? a.normalized : Vector3.zero;
        }

        /// <summary>
        /// ความแรงการป้องกัน [0..1] ของ deflector ตัวนี้ ต่อเป้าหมายหนึ่งจุด
        /// 0 = ไม่ป้องกัน, 1 = ป้องกันสูงสุด (แกนตรงลมเป๊ะ + อยู่ในเงาเต็ม)
        /// windDir ต้องเป็น normalized แล้ว
        /// </summary>
        private float ProtectionStrength(StructureUnit target, Vector3 worldPos, Vector3 windDir, out bool isSelf)
        {
            isSelf = (_unit != null && _unit == target);

            // alignment: แกนต้องหันไปทางเดียวกับลม
            Vector3 axis = AxisWorld();
            if (axis == Vector3.zero) return 0f;

            float dot = Vector3.Dot(axis, windDir);                 // -1..1
            if (dot < alignmentThreshold) return 0f;
            float align = Mathf.InverseLerp(alignmentThreshold, 1f, dot); // 0..1

            if (isSelf) return align;                              // บันไดเองได้รับการป้องกันตามความตรง

            // เงาลม: เป้าหมายต้องอยู่ "ปลายลม" ของบันได และอยู่ในกรอบความกว้าง
            Vector3 d = worldPos - _t.position;
            float along = Vector3.Dot(d, windDir);                 // ระยะตามแนวลม (บวก = ปลายลม/ด้านหลัง)
            if (along <= 0.01f || along > shadowLength) return 0f;

            Vector3 perp = d - along * windDir;                    // องค์ประกอบตั้งฉากกับลม
            float halfW = shadowHalfWidth > 0f ? shadowHalfWidth : DerivedHalfWidth();
            if (perp.magnitude > halfW) return 0f;

            return align;
        }

        /// <summary>ครึ่งความกว้างเงา derived จากขนาด collider (fallback 1m)</summary>
        private float DerivedHalfWidth()
        {
            if (_cachedHalfWidth > 0f) return _cachedHalfWidth;

            var col = GetComponentInChildren<Collider>();
            if (col != null)
            {
                Vector3 e = col.bounds.extents;
                _cachedHalfWidth = Mathf.Max(0.5f, Mathf.Max(e.x, e.z));
            }
            else
            {
                _cachedHalfWidth = 1f;
            }
            return _cachedHalfWidth;
        }

        // ────────────────────────────────────────────────────────────────
        // Static query API — เรียกจาก StrongWindDisaster
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// ตัวคูณ "ดาเมจจากลม" สำหรับเป้าหมาย [0..1]. 1 = โดนเต็ม, 0 = ไม่โดน
        /// เลือกการป้องกันที่ "แรงที่สุด" จากบันไดทุกตัว (min multiplier)
        /// </summary>
        public static float GetWindDamageMultiplier(StructureUnit target, Vector3 worldPos, Vector3 windDir)
        {
            if (_active.Count == 0) return 1f;
            if (windDir.sqrMagnitude < 1e-6f) return 1f;
            windDir = windDir.normalized;

            float mult = 1f;
            for (int i = 0; i < _active.Count; i++)
            {
                var d = _active[i];
                if (d == null || !d.isActiveAndEnabled) continue;

                float s = d.ProtectionStrength(target, worldPos, windDir, out bool isSelf);
                if (s <= 0f) continue;

                float minMult = isSelf ? d.selfMinDamageMultiplier : d.shadowMinDamageMultiplier;
                float m = Mathf.Lerp(1f, minMult, s);
                if (m < mult) mult = m;
            }
            return mult;
        }

        /// <summary>
        /// ตัวคูณ "แรงผลักจากลม" สำหรับเป้าหมาย [0..1]. คืน 1 ถ้าไม่มี deflector ที่เปิด protectPushForce
        /// </summary>
        public static float GetWindForceMultiplier(StructureUnit target, Vector3 worldPos, Vector3 windDir)
        {
            if (_active.Count == 0) return 1f;
            if (windDir.sqrMagnitude < 1e-6f) return 1f;
            windDir = windDir.normalized;

            float mult = 1f;
            for (int i = 0; i < _active.Count; i++)
            {
                var d = _active[i];
                if (d == null || !d.isActiveAndEnabled || !d.protectPushForce) continue;

                float s = d.ProtectionStrength(target, worldPos, windDir, out _);
                if (s <= 0f) continue;

                float m = Mathf.Lerp(1f, d.minForceMultiplier, s);
                if (m < mult) mult = m;
            }
            return mult;
        }

#if UNITY_EDITOR
        // วาดกรอบ "เงาลม" ในหน้า Scene (เมื่อเลือก object) เพื่อช่วยจูนค่า
        private void OnDrawGizmosSelected()
        {
            if (_t == null) _t = transform;

            Vector3 dir = AxisWorld();
            if (dir == Vector3.zero) return;

            float halfW = shadowHalfWidth > 0f ? shadowHalfWidth : DerivedHalfWidth();
            Vector3 center = _t.position + dir * (shadowLength * 0.5f);

            // กัน LookRotation degenerate เมื่อแกนเกือบขนานกับ world up
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(dir, up);

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
            Vector3 boxSize = new Vector3(halfW * 2f, halfW * 2f, shadowLength);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.20f);
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = old;

            // ลูกศรแกนรับลม
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_t.position, dir * 2f);
        }
#endif
    }
}
