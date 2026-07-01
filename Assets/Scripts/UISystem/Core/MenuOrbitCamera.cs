using UnityEngine;
using Simulation.Building; // StructureUnit (สำหรับ auto-frame ตามขนาดตึก)

namespace Simulation.UI
{
    /// <summary>
    /// กล้องหมุนรอบตึกช้า ๆ สำหรับใช้เป็นพื้นหลังเมนู
    /// เล็งไปที่กลางตึกแล้วค่อย ๆ หมุน yaw รอบ ๆ
    /// auto-frame: ปรับ pivot/ระยะ/ความสูงตามขนาดตึกที่สร้างจริง (รองรับตึกสุ่มหลายชั้น)
    /// ใช้ unscaledTime จึงหมุนต่อแม้ timeScale = 0 (เช่นเมนู pause เกม)
    /// </summary>
    public class MenuOrbitCamera : MonoBehaviour
    {
        [Header("Orbit")]
        [SerializeField] private Vector3 pivot = new Vector3(0f, 5f, 0f);
        [SerializeField] private float distance = 24f;
        [SerializeField] private float height = 10f;
        [Tooltip("ความเร็วหมุน (องศาต่อวินาที)")]
        [SerializeField] private float rotateSpeed = 8f;
        [SerializeField] private float startYaw = 45f;

        [Header("Auto-frame ตามขนาดตึก")]
        [SerializeField] private bool autoFrame = true;
        [Tooltip("ช่วงเวลาในการคำนวณกรอบตึกใหม่ (วินาที)")]
        [SerializeField] private float reframeInterval = 0.5f;
        [Tooltip("ตัวคูณระยะห่างเพื่อเผื่อขอบ")]
        [SerializeField] private float framePadding = 1.7f;
        [Tooltip("ความลื่นในการเลื่อนเข้าหาเป้า")]
        [SerializeField] private float followLerp = 2f;

        [Header("Framing Offset (เลื่อนตึกในจอ)")]
        [Tooltip("เลื่อนตึกในเฟรมแนวนอน: + = ไปทางขวา, - = ไปทางซ้าย (หน่วยเป็นระยะโลก)")]
        [SerializeField] private float framingOffsetX = 0f;
        [Tooltip("เลื่อนตึกในเฟรมแนวตั้ง: + = ขึ้น, - = ลง")]
        [SerializeField] private float framingOffsetY = 0f;

        private float _yaw;
        private float _nextReframe;
        private Vector3 _targetPivot;
        private float _targetDistance;

        /// <summary>ตั้งค่าจากภายนอก (เช่นจาก MainMenuBuildingBackground)</summary>
        public void Configure(Vector3 p, float dist, float h, float speed)
        {
            pivot = p; distance = dist; height = h; rotateSpeed = speed;
            _targetPivot = pivot; _targetDistance = distance;
        }

        private void Start()
        {
            _yaw = startYaw;
            _targetPivot = pivot;
            _targetDistance = distance;
            Apply();
        }

        private void LateUpdate()
        {
            _yaw += rotateSpeed * Time.unscaledDeltaTime;

            if (autoFrame && Time.unscaledTime >= _nextReframe)
            {
                _nextReframe = Time.unscaledTime + reframeInterval;
                ComputeFrame();
            }

            // ค่อย ๆ เลื่อน pivot/ระยะเข้าหาเป้า (ลื่น ไม่กระตุก)
            pivot = Vector3.Lerp(pivot, _targetPivot, followLerp * Time.unscaledDeltaTime);
            distance = Mathf.Lerp(distance, _targetDistance, followLerp * Time.unscaledDeltaTime);
            Apply();
        }

        private void ComputeFrame()
        {
            var units = FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            if (units == null || units.Length == 0) return;

            bool has = false;
            Bounds b = new Bounds();
            foreach (var u in units)
            {
                if (u == null) continue;
                foreach (var r in u.GetComponentsInChildren<Renderer>())
                {
                    if (!has) { b = r.bounds; has = true; }
                    else b.Encapsulate(r.bounds);
                }
            }
            if (!has) return;

            _targetPivot = b.center;
            float radius = Mathf.Max(new Vector2(b.extents.x, b.extents.z).magnitude, 4f);
            _targetDistance = radius * framePadding + 6f;
            height = Mathf.Max(b.extents.y * 0.9f + 4f, 6f);
        }

        private void Apply()
        {
            Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -distance) + Vector3.up * height;
            transform.position = pivot + offset;
            transform.LookAt(pivot);

            // เลื่อนตึกในจอ: pan กล้อง+เป้าไปด้านตรงข้าม เพื่อดันตึกไปด้านที่ต้องการ
            // (+X = ตึกไปขวา, +Y = ตึกขึ้น) โดยไม่เปลี่ยนระยะซูมหรือการหมุน
            if (framingOffsetX != 0f || framingOffsetY != 0f)
            {
                Vector3 pan = transform.right * framingOffsetX + transform.up * framingOffsetY;
                transform.position -= pan;
                transform.LookAt(pivot - pan);
            }
        }
    }
}
