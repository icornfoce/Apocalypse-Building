using UnityEngine;
using Simulation.Mission;

namespace Simulation.Building
{
    /// <summary>
    /// ลูกธนูที่บินไปหาเป้าหมาย (BalloonZombieAI)
    /// เมื่อถึงหรือเป้าตาย → Pop Balloon + ทำลายตัวเอง
    /// บินโค้งเลี้ยวหาเป้าหมาย (Steering) โดยใช้หัวลูกศร (arrowTip) นำทางอย่างราบรื่น
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        [Tooltip("ลากส่วนหัวของลูกธนู (Transform) มาใส่ที่นี่ เพื่อให้ส่วนนี้พุ่งเข้าหาเป้าหมาย")]
        public Transform arrowTip;

        [Tooltip("ความเร็วในการบิน (เมตรต่อวินาที) — สามารถตั้งความเร็วช้าๆ (เช่น 2 หรือ 3) ในหน้าต่าง Inspector เพื่อดีบักทิศทางการบินได้")]
        public float speed = 30f;

        private BalloonZombieAI _target;
        private float _lifetime;
        private const float MAX_LIFETIME = 5f;
        private const float HIT_DISTANCE = 1.0f;

        public void Initialize(BalloonZombieAI target, float launcherSpeed)
        {
            _target = target;
            _lifetime = 0f;

            // ใช้ความเร็วที่กำหนดจากเครื่องยิง (BalloonLauncher) โดยตรง
            // ทำให้ปรับความเร็วได้จาก Inspector ของเครื่องยิงได้ทันทีและถูกต้อง
            this.speed = launcherSpeed;

            // ค้นหาอัตโนมัติหากไม่ได้กำหนดใน Inspector
            if (arrowTip == null)
            {
                arrowTip = FindChildRecursive(transform, "Tip");
                if (arrowTip == null) arrowTip = FindChildRecursive(transform, "ArrowTip");
                if (arrowTip == null) arrowTip = FindChildRecursive(transform, "head");
            }

            // แสดง Log เพื่อช่วยดีบักตำแหน่งและขนาดของลูกธนูที่ถูกเสก
            Debug.Log($"<color=yellow>[ArrowProjectile]</color> Spawned! Pos: {transform.position}, Scale: {transform.localScale}, LossyScale: {transform.lossyScale}, Active: {gameObject.activeInHierarchy}");
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void Update()
        {
            _lifetime += Time.deltaTime;

            // ถ้าเป้าตายหรือหายไป หรือเกินเวลา → ทำลาย
            if (_target == null || _lifetime > MAX_LIFETIME)
            {
                Destroy(gameObject);
                return;
            }

            // ค้นหาตำแหน่งเป้าหมายจริง (ลูกโป่ง)
            Vector3 targetPos = _target.balloonObject != null ? _target.balloonObject.transform.position : _target.transform.position + Vector3.up * 2f;
            Vector3 targetDirection = (targetPos - transform.position).normalized;

            // หมุนหน้าไปทิศเป้าหมายอย่างราบรื่น (Steering)
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                // เลี้ยวเร็วขึ้นเมื่อเข้าใกล้เป้าหมาย เพื่อไม่ให้บินเลยเป้า
                float distanceToTarget = Vector3.Distance(transform.position, targetPos);
                float turnSpeed = distanceToTarget < 5f ? 30f : 15f;

                // คำนวณ targetRotation สำหรับเครื่องยิงหลัก โดยหักล้างมุมหมุนของหัวลูกศร
                Quaternion targetRotation;
                if (arrowTip != null && arrowTip != transform)
                {
                    // หมุนให้ forward ของ arrowTip ชี้ไปที่เป้าหมาย
                    targetRotation = Quaternion.LookRotation(targetDirection) * Quaternion.Inverse(arrowTip.localRotation);
                }
                else
                {
                    targetRotation = Quaternion.LookRotation(targetDirection);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            // เคลื่อนที่ไปข้างหน้าตามทิศของหัวธนูเสมอ เพื่อให้หัวธนูหันตามทิศทางบินเสมอ
            Vector3 moveDirection = arrowTip != null ? arrowTip.forward : transform.forward;
            transform.position += moveDirection * speed * Time.deltaTime;

            // เช็คว่าถึงเป้าหมายหรือยัง
            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist <= HIT_DISTANCE)
            {
                // โดนแล้ว!
                if (_target != null && !_target.IsDead && !_target.HasLanded)
                {
                    _target.PopBalloon();
                }
                Destroy(gameObject);
            }
        }
    }
}
