using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Simulation.Character;
using Simulation.Mission;
using Simulation.NPC;

namespace Simulation.Building
{
    /// <summary>
    /// ควบคุมการเปิด-ปิดประตูอัตโนมัติเมื่อมีคนเดินเข้าใกล้
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Header("Door Parts")]
        [Tooltip("ส่วนของประตูที่จะหมุนเปิด (ถ้าไม่ใส่จะใช้ Child ตัวแรก)")]
        public Transform doorPivot;

        [Header("Settings")]
        public float openAngle = 90f;
        public float smoothSpeed = 8f;          // open faster so walkers don't jam against the leaf
        public float detectionRadius = 1.5f;
        [Tooltip("รัศมีที่คน/NPC ต้องเข้ามาใกล้ก่อนประตูจะเปิด (เล็ก = เปิดตอนใกล้ขึ้น)")]
        public float openRadius = 1.4f;
        [Tooltip("ถ้ามีซอมบี้อยู่ในรัศมีนี้ ประตูจะไม่เปิด (กันซอมบี้เดินตามคนทะลุ → ต้องกัดพังเอง)")]
        public float zombieBlockRadius = 2.2f;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _shouldBeOpen = false;
        private HashSet<Collider> _occupants = new HashSet<Collider>();
        private BoxCollider _trigger;
        private float _scanTimer;
        private Simulation.Physics.StructuralStress _stress; // โครงสร้างของประตู (ใช้กันพังตอนคนเดินชน)
        private Collider[] _leafColliders; // collider แข็งของบานประตู — สลับเป็น trigger ตอนเปิด ให้คนทะลุได้ (ไม่ชน = ไม่พัง)
        private readonly Collider[] _scanBuffer = new Collider[48]; // buffer สำหรับ OverlapSphereNonAlloc (ลด GC)

        private void Start()
        {
            // ถ้าไม่ได้กำหนด pivot ให้ลองหาจากลูกตัวแรก
            if (doorPivot == null && transform.childCount > 0)
            {
                doorPivot = transform.GetChild(0);
            }

            if (doorPivot != null)
            {
                _closedRotation = doorPivot.localRotation;
                _openRotation = _closedRotation * Quaternion.Euler(0, openAngle, 0);

                // collider แข็งของบานประตู (ลูกของ Pivot) — เก็บไว้สลับเป็น trigger ตอนเปิด
                _leafColliders = doorPivot.GetComponentsInChildren<Collider>(true);
            }

            // ตรวจสอบและตั้งค่า Trigger
            _trigger = GetComponent<BoxCollider>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<BoxCollider>();
                _trigger.isTrigger = true;
                _trigger.size = new Vector3(2f, 2f, 2f); // ขนาดเริ่มต้น
            }
            else
            {
                _trigger.isTrigger = true;
            }

            // ── NavMesh: Exclude ประตูออกจากการ Bake ──
            // BoxCollider trigger ของประตูจะถูก NavMeshSurface นับเป็น "กำแพง"
            // ทำให้ Agent เดินผ่านช่องประตูไม่ได้ → ต้องใส่ NavMeshModifier เพื่อ ignore
            NavMeshModifier modifier = GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = gameObject.AddComponent<NavMeshModifier>();
            }
            modifier.overrideArea = true;
            modifier.area = 1; // area index 1 = "Not Walkable" (built-in)
            modifier.ignoreFromBuild = true; // ไม่รวมในการ Bake เลย

            // โครงสร้างของประตู — ใช้ต่ออายุ grace กันประตูพังตอนคนเดินชน (StructuralStress อยู่บนตัวประตู)
            _stress = GetComponentInParent<Simulation.Physics.StructuralStress>();
        }

        private void Update()
        {
            if (doorPivot == null) return;

            // ── ล้าง Collider ที่ถูก Destroy ออก (เช่น PersonAI โดน Destroy ตอน StopSimulation) ──
            _occupants.RemoveWhere(c => c == null);

            // ── เปิดประตูล่วงหน้าเมื่อมีคน/NPC เข้าใกล้ (กันการจ่อบานติดก่อนประตูจะกาง) ──
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 0.12f;
                bool nearbyPerson = false;
                bool nearbyZombie = false;
                float scanR = Mathf.Max(openRadius, zombieBlockRadius);
                int n = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, scanR, _scanBuffer, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++)
                {
                    var c = _scanBuffer[i];
                    if (c == null) continue;

                    // ซอมบี้จ่ออยู่ในรัศมีบล็อก → ห้ามเปิด (บังคับให้กัดพังเอง ไม่เดินตามคนทะลุ)
                    if (!nearbyZombie && c.GetComponentInParent<ZombieAI>() != null)
                    {
                        if (Vector3.Distance(transform.position, c.transform.position) <= zombieBlockRadius)
                        {
                            nearbyZombie = true;
                            continue;
                        }
                    }

                    // คน/NPC เข้ามาใกล้พอในรัศมีเปิด
                    if (!nearbyPerson &&
                        (c.GetComponentInParent<PersonAI>() != null || c.GetComponentInParent<NPCController>() != null))
                    {
                        if (Vector3.Distance(transform.position, c.transform.position) <= openRadius)
                        {
                            nearbyPerson = true;
                        }
                    }
                }

                // เปิดเฉพาะเมื่อมีคนใกล้ "และไม่มีซอมบี้จ่อ" — ไม่งั้นปิดไว้ (กันซอมบี้ทะลุ + ปิดเองเมื่อไม่มีคน)
                _shouldBeOpen = nearbyPerson && !nearbyZombie;
            }

            // กันประตูพังตอนคนเดินชน: ระหว่างมีคนใกล้ (ประตูควรเปิด) ต่ออายุช่วงงดคิดดาเมจจากแรงของโครงสร้าง
            if (_shouldBeOpen && _stress != null)
            {
                _stress.NotifyCharacterContact();
            }

            // หมุนประตูอย่างนุ่มนวล
            Quaternion targetRot = _shouldBeOpen ? _openRotation : _closedRotation;
            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, targetRot, Time.deltaTime * smoothSpeed);

            // ── เปิด = ทำบานประตูเป็น trigger ให้คนเดินทะลุได้ (ไม่ชน = ไม่มีแรงดัน = ประตูไม่พัง),
            //    ปิด = บานแข็งตามเดิมเมื่อปิดสนิทแล้วเท่านั้น (ยังบล็อกซอมบี้อยู่) ──
            bool isFullyClosed = Quaternion.Angle(doorPivot.localRotation, _closedRotation) < 5f;
            if (_leafColliders != null)
            {
                foreach (var c in _leafColliders)
                {
                    if (c != null)
                    {
                        c.isTrigger = _shouldBeOpen || !isFullyClosed;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // เช็คว่าเป็นคน (PersonAI หรือ NPCController) เท่านั้น (ซอมบี้เปิดไม่ได้)
            // หมายเหตุ: การเปิด/ปิดจริงตัดสินโดย scan ใน Update (เพื่อบังคับกฎ "ห้ามเปิดถ้ามีซอมบี้จ่อ")
            if (other.GetComponentInParent<PersonAI>() != null ||
                other.GetComponentInParent<NPCController>() != null)
            {
                _occupants.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_occupants.Contains(other))
            {
                _occupants.Remove(other);
            }
        }

        /// <summary>
        /// รีเซ็ตประตูกลับสถานะปิดทันที (เรียกจาก SimulationManager.StopSimulation)
        /// </summary>
        public void ResetDoor()
        {
            _occupants.Clear();
            _shouldBeOpen = false;

            // Snap ประตูกลับตำแหน่งปิดทันที (ไม่ Slerp)
            if (doorPivot != null)
            {
                doorPivot.localRotation = _closedRotation;
            }
        }

        private void OnDisable()
        {
            // รีเซ็ตเมื่อถูกปิด (เช่น ตอนพัง)
            _occupants.Clear();
            _shouldBeOpen = false;

            // Snap ปิดทันทีเพื่อไม่ให้ค้างกลางทาง
            if (doorPivot != null)
            {
                doorPivot.localRotation = _closedRotation;
            }
        }
    }
}
