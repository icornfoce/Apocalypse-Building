using UnityEngine;
using System.Collections.Generic;
using Simulation.Character;
using Simulation.Mission;

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
        public float smoothSpeed = 5f;
        public float detectionRadius = 1.5f;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _shouldBeOpen = false;
        private HashSet<Collider> _occupants = new HashSet<Collider>();
        private BoxCollider _trigger;

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
        }

        private void Update()
        {
            if (doorPivot == null) return;

            // หมุนประตูอย่างนุ่มนวล
            Quaternion targetRot = _shouldBeOpen ? _openRotation : _closedRotation;
            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, targetRot, Time.deltaTime * smoothSpeed);
        }

        private void OnTriggerEnter(Collider other)
        {
            // เช็คว่าเป็นคน (PersonAI) เท่านั้น (ซอมบี้เปิดไม่ได้)
            if (other.GetComponentInParent<PersonAI>() != null)
            {
                _occupants.Add(other);
                _shouldBeOpen = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_occupants.Contains(other))
            {
                _occupants.Remove(other);
                if (_occupants.Count == 0)
                {
                    _shouldBeOpen = false;
                }
            }
        }

        private void OnDisable()
        {
            // รีเซ็ตเมื่อถูกปิด (เช่น ตอนพัง)
            _occupants.Clear();
            _shouldBeOpen = false;
        }
    }
}
