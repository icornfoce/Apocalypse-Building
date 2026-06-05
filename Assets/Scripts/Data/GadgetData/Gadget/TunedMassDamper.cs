using UnityEngine;
using System.Collections.Generic;
using Simulation.Physics;

namespace Simulation.Building
{
    /// <summary>
    /// Tuned Mass Damper (TMD) — เครื่องลดการสั่นและแกว่งตัวของตึก
    /// 1. ช่วยลดการแกว่งโดยซับและหน่วงแรงสั่นสะเทือน (Damping) บนโครงสร้างที่เชื่อมต่ออยู่ทั้งหมด
    /// 2. ช่วยพยุงและออกแรงพยายามดึงโครงสร้างให้กลับมาอยู่ในแนวตั้งตรง (Restoring Torque)
    /// 3. ใช้ ConfigurableJoint สำหรับเชือกเพื่อให้ลูกตุ้มแกว่งตาม Physics จริง
    /// </summary>
    public class TunedMassDamper : MonoBehaviour
    {
        [Header("TMD Physics Settings")]
        [Tooltip("สัมประสิทธิ์การหน่วงความเร็ว (ความหน่วงเชิงเส้นและเชิงมุม)")]
        public float dampingCoefficient = 0.8f;
        
        [Tooltip("ความแรงในการดึงโครงสร้างกลับมาแนวตั้งตรง")]
        public float restoringStrength = 15f;

        [Header("Pendulum References (จาก Prefab)")]
        [Tooltip("Transform ของเชือก — Auto-find 'Rope' ถ้าไม่ assign")]
        public Transform ropeTransform;
        [Tooltip("Transform ของลูกตุ้ม — Auto-find 'Pendulum' / 'Weight' ถ้าไม่ assign")]
        public Transform pendulumTransform;

        [Header("ConfigurableJoint Settings")]
        [Tooltip("Spring Force ของเชือก (ความแข็ง)")]
        public float ropeSpring = 500f;
        [Tooltip("Damper Force ของเชือก (ความหน่วง)")]
        public float ropeDamper = 50f;
        [Tooltip("ขีดจำกัดระยะห่างสูงสุดของลูกตุ้ม (Linear Limit)")]
        public float ropeLinearLimit = 0.01f;
        [Tooltip("มุมแกว่งสูงสุด (องศา)")]
        public float swingAngleLimit = 45f;

        // รายการ Rigidbody และมุมเริ่มต้นของอาคารที่เชื่อมต่อกัน
        private List<Rigidbody> _connectedRigidbodies = new List<Rigidbody>();
        private Dictionary<Rigidbody, Quaternion> _originalRotations = new Dictionary<Rigidbody, Quaternion>();
        private bool _isInitialized = false;
        private ConfigurableJoint _pendulumJoint;
        private Rigidbody _pendulumRb;

        private Vector3 _originalPendulumLocalPos;
        private Quaternion _originalPendulumLocalRot;
        private bool _hasSavedOriginalTransform = false;
        private bool _wasSimulatingLastFrame = false;

        private void Start()
        {
            AutoFindReferences();
            SaveOriginalPendulumTransform();
        }

        private void OnEnable()
        {
            // ลงทะเบียนอีเวนต์ หรือเตรียมตัวเมื่อเริ่มจำลอง
            _isInitialized = false;
            _wasSimulatingLastFrame = SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating;
        }

        private void OnDisable()
        {
            ResetPendulum();
        }

        private void Update()
        {
            bool isSimulating = SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating;
            if (_wasSimulatingLastFrame && !isSimulating)
            {
                ResetPendulum();
            }
            _wasSimulatingLastFrame = isSimulating;

            if (!isSimulating)
            {
                if (pendulumTransform != null)
                {
                    var rb = pendulumTransform.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    if (_hasSavedOriginalTransform)
                    {
                        pendulumTransform.localPosition = _originalPendulumLocalPos;
                        pendulumTransform.localRotation = _originalPendulumLocalRot;
                    }
                }
            }
        }

        private void SaveOriginalPendulumTransform()
        {
            if (pendulumTransform != null && !_hasSavedOriginalTransform)
            {
                _originalPendulumLocalPos = pendulumTransform.localPosition;
                _originalPendulumLocalRot = pendulumTransform.localRotation;
                _hasSavedOriginalTransform = true;
            }
        }

        public void ResetPendulum()
        {
            _isInitialized = false;

            if (pendulumTransform != null && _hasSavedOriginalTransform)
            {
                // ลบ Joint เก่า
                if (_pendulumJoint != null)
                {
                    DestroyImmediate(_pendulumJoint);
                    _pendulumJoint = null;
                }
                var joints = pendulumTransform.GetComponents<Joint>();
                foreach (var j in joints) DestroyImmediate(j);

                // รีเซ็ต Rigidbody
                if (_pendulumRb != null)
                {
                    _pendulumRb.linearVelocity = Vector3.zero;
                    _pendulumRb.angularVelocity = Vector3.zero;
                    _pendulumRb.isKinematic = true;
                }

                // คืนตำแหน่งและการหมุนแบบ Local
                pendulumTransform.localPosition = _originalPendulumLocalPos;
                pendulumTransform.localRotation = _originalPendulumLocalRot;
            }
        }

        /// <summary>
        /// ค้นหา ropeTransform และ pendulumTransform จาก child อัตโนมัติ
        /// </summary>
        private void AutoFindReferences()
        {
            if (ropeTransform == null)
            {
                ropeTransform = transform.Find("Rope");
                if (ropeTransform == null) ropeTransform = transform.Find("Cable");
                if (ropeTransform == null) ropeTransform = transform.Find("String");
            }

            if (pendulumTransform == null)
            {
                pendulumTransform = transform.Find("Pendulum");
                if (pendulumTransform == null) pendulumTransform = transform.Find("Weight");
                if (pendulumTransform == null) pendulumTransform = transform.Find("TMD_Weight");
                // ลองหาจาก Rope child
                if (pendulumTransform == null && ropeTransform != null)
                {
                    pendulumTransform = ropeTransform.Find("Pendulum");
                    if (pendulumTransform == null) pendulumTransform = ropeTransform.Find("Weight");
                }
            }
        }

        private void FixedUpdate()
        {
            // ตรวจสอบสถานะการจำลอง
            if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating)
            {
                _isInitialized = false;
                return;
            }

            // ถ้าจำลองเริ่มทำงานแล้ว และยังไม่ได้เชื่อมต่อโครงสร้าง
            if (!_isInitialized)
            {
                InitializeConnectedBuilding();
                SetupPendulumJoint();
            }

            // ทำงาน Damping และ Restoring บน Rigidbodies ของตึก
            ApplyStabilizationForces();
        }

        /// <summary>
        /// สร้าง ConfigurableJoint บนลูกตุ้มเพื่อเชื่อมกับ TMD ผ่านเชือก
        /// </summary>
        private void SetupPendulumJoint()
        {
            if (pendulumTransform == null)
            {
                Debug.LogWarning("[TunedMassDamper] No pendulum transform found! Cannot create ConfigurableJoint.");
                return;
            }

            SaveOriginalPendulumTransform();

            // เพิ่ม Rigidbody ให้ลูกตุ้มถ้ายังไม่มี
            _pendulumRb = pendulumTransform.GetComponent<Rigidbody>();
            if (_pendulumRb == null)
            {
                _pendulumRb = pendulumTransform.gameObject.AddComponent<Rigidbody>();
            }
            _pendulumRb.mass = 5f;
            _pendulumRb.linearDamping = 0.5f;
            _pendulumRb.angularDamping = 0.5f;
            _pendulumRb.useGravity = true;
            _pendulumRb.isKinematic = false;
            _pendulumRb.interpolation = RigidbodyInterpolation.Interpolate;

            // สั่งให้ Collider ของลูกตุ้มมองข้ามการชนกับโครงสร้างตึกทั้งหมดในฉาก (ป้องกันการดันกันเองทำให้ตลกลอยตัว/ตกช้า)
            Collider[] pendulumCols = pendulumTransform.GetComponentsInChildren<Collider>();
            StructureUnit[] allStructures = Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);
            foreach (var structUnit in allStructures)
            {
                if (structUnit == null) continue;
                Collider[] structCols = structUnit.GetComponentsInChildren<Collider>();
                foreach (var sCol in structCols)
                {
                    foreach (var pCol in pendulumCols)
                    {
                        if (sCol != null && pCol != null && sCol != pCol)
                        {
                            UnityEngine.Physics.IgnoreCollision(sCol, pCol, true);
                        }
                    }
                }
            }

            // ลบ Joint เก่าถ้ามี
            var oldJoints = pendulumTransform.GetComponents<Joint>();
            foreach (var j in oldJoints) DestroyImmediate(j);

            // ── สร้าง ConfigurableJoint ──
            _pendulumJoint = pendulumTransform.gameObject.AddComponent<ConfigurableJoint>();

            // เชื่อมกับ Rigidbody ของ TMD เอง (ตัวฐาน)
            Rigidbody tmdRb = GetComponent<Rigidbody>();
            _pendulumJoint.connectedBody = tmdRb;

            // ── ตั้งค่า Anchor ──
            // Anchor อยู่ที่ตัวลูกตุ้มเอง (จุดศูนย์กลาง)
            _pendulumJoint.anchor = Vector3.zero;
            // Connected Anchor อยู่ที่จุดแขวน (ตำแหน่งเชือกบน TMD ในพิกัด Local ของ TMD)
            if (ropeTransform != null)
            {
                _pendulumJoint.connectedAnchor = transform.InverseTransformPoint(ropeTransform.position);
            }
            else
            {
                _pendulumJoint.connectedAnchor = Vector3.zero;
            }

            // ── Linear Motion: ล็อกทุกแกน (ไม่ให้ยืดหด) ──
            _pendulumJoint.xMotion = ConfigurableJointMotion.Locked;
            _pendulumJoint.yMotion = ConfigurableJointMotion.Locked;
            _pendulumJoint.zMotion = ConfigurableJointMotion.Locked;

            // ── Angular Motion: ปล่อยให้แกว่งได้ (Limited) ──
            _pendulumJoint.angularXMotion = ConfigurableJointMotion.Limited;
            _pendulumJoint.angularYMotion = ConfigurableJointMotion.Free;
            _pendulumJoint.angularZMotion = ConfigurableJointMotion.Limited;

            // ── Angular Limits ──
            SoftJointLimit angularLimit = new SoftJointLimit();
            angularLimit.limit = swingAngleLimit;
            angularLimit.bounciness = 0f;
            angularLimit.contactDistance = 5f;
            // Low/High Angular X Limit (ก้มเงย)
            SoftJointLimit lowAngX = new SoftJointLimit();
            lowAngX.limit = -swingAngleLimit;
            _pendulumJoint.lowAngularXLimit = lowAngX;
            SoftJointLimit highAngX = new SoftJointLimit();
            highAngX.limit = swingAngleLimit;
            _pendulumJoint.highAngularXLimit = highAngX;
            // Angular Z Limit (ซ้ายขวา)
            _pendulumJoint.angularZLimit = angularLimit;

            // ── Angular Drive (Spring + Damper) ── ให้ลูกตุ้มมีแรงพยุงกลับจุดศูนย์กลาง
            JointDrive angularDrive = new JointDrive();
            angularDrive.positionSpring = ropeSpring;
            angularDrive.positionDamper = ropeDamper;
            angularDrive.maximumForce = float.MaxValue;
            _pendulumJoint.angularXDrive = angularDrive;
            _pendulumJoint.angularYZDrive = angularDrive;

            // ปิด Projection เพื่อเพิ่มเสถียรภาพ
            _pendulumJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            _pendulumJoint.projectionDistance = 0.01f;
            _pendulumJoint.projectionAngle = 5f;

            Debug.Log($"<color=green>[TunedMassDamper]</color> ConfigurableJoint created on pendulum '{pendulumTransform.name}'");
        }

        private void InitializeConnectedBuilding()
        {
            _connectedRigidbodies.Clear();
            _originalRotations.Clear();

            StructureUnit myUnit = GetComponent<StructureUnit>();
            if (myUnit == null) return;

            // ค้นหาชิ้นส่วนโครงสร้างที่เชื่อมต่อกันทั้งหมดโดยใช้ Breadth-First Search (BFS)
            HashSet<StructureUnit> visited = new HashSet<StructureUnit>();
            Queue<StructureUnit> queue = new Queue<StructureUnit>();

            queue.Enqueue(myUnit);
            visited.Add(myUnit);

            StructureUnit[] allPlaced = Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None);

            while (queue.Count > 0)
            {
                StructureUnit current = queue.Dequeue();
                Rigidbody currentRb = current.GetComponent<Rigidbody>();
                
                // เก็บ Rigidbody ของตึก (ยกเว้นตัว TMD เอง)
                if (currentRb != null && currentRb != GetComponent<Rigidbody>())
                {
                    if (!_connectedRigidbodies.Contains(currentRb))
                    {
                        _connectedRigidbodies.Add(currentRb);
                        _originalRotations[currentRb] = current.transform.rotation;
                    }
                }

                // ค้นหาทิศการต่อของ Joint (จากฝั่งตัวเองไปหาคนอื่น)
                Joint[] joints = current.GetComponents<Joint>();
                foreach (var j in joints)
                {
                    if (j != null && j.connectedBody != null)
                    {
                        StructureUnit other = j.connectedBody.GetComponent<StructureUnit>();
                        if (other != null && !visited.Contains(other))
                        {
                            visited.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }

                // ค้นหาทิศการต่อย้อนกลับ (จากฝั่งคนอื่นมาหาตัวเอง)
                if (currentRb != null)
                {
                    foreach (var other in allPlaced)
                    {
                        if (other == null || visited.Contains(other)) continue;

                        Joint[] otherJoints = other.GetComponents<Joint>();
                        foreach (var oj in otherJoints)
                        {
                            if (oj != null && oj.connectedBody == currentRb)
                            {
                                visited.Add(other);
                                queue.Enqueue(other);
                                break;
                            }
                        }
                    }
                }
            }

            _isInitialized = true;
            Debug.Log($"<color=green>[TunedMassDamper]</color> Initialized damping on {visited.Count} building structures.");
        }

        private bool IsBuildingGrounded()
        {
            // Check TMD's own joints (Fixed to world/ground has connectedBody == null)
            Joint[] myJoints = GetComponents<Joint>();
            foreach (var j in myJoints)
            {
                if (j != null && j.connectedBody == null) return true;
            }

            // Check all connected rigidbodies' joints
            foreach (var rb in _connectedRigidbodies)
            {
                if (rb == null) continue;
                Joint[] joints = rb.GetComponents<Joint>();
                foreach (var j in joints)
                {
                    if (j != null && j.connectedBody == null) return true;
                }
            }

            return false;
        }

        private void ApplyStabilizationForces()
        {
            // ถ้าตัว TMD เองพัง หรือหลุดร่วง ให้หยุดทำงานทันที
            var myStress = GetComponent<StructuralStress>();
            if (myStress != null && (myStress.IsBroken || myStress.IsDetached))
            {
                return;
            }

            // ถ้าโครงสร้างไม่ได้เชื่อมต่อกับพื้นดิน (ลอยอยู่กลางอากาศหรือพังครืนลงมาทั้งหมด) ให้งดการพยุงแรง
            if (!IsBuildingGrounded())
            {
                return;
            }

            for (int i = _connectedRigidbodies.Count - 1; i >= 0; i--)
            {
                Rigidbody rb = _connectedRigidbodies[i];
                if (rb == null || !rb.gameObject.activeInHierarchy)
                {
                    _connectedRigidbodies.RemoveAt(i);
                    continue;
                }

                // ข้ามโครงสร้างที่หลุดร่วง/ถล่มไปแล้ว (ไม่มี Joint เกาะ หรือสถานะพัง/หลุด)
                var stress = rb.GetComponent<StructuralStress>();
                if (stress != null && (stress.IsBroken || stress.IsDetached))
                {
                    continue;
                }
                if (rb.GetComponent<Joint>() == null)
                {
                    continue;
                }

                // 1. DAMPING FORCE: ต้านความเร็วเชิงเส้น (เฉพาะแนวนอน X-Z เพื่อหน่วงการแกว่งโยก)
                Vector3 vel = rb.linearVelocity;
                Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
                rb.AddForce(-horizontalVel * dampingCoefficient, ForceMode.Acceleration);

                // 2. DAMPING TORQUE: ต้านความเร็วเชิงมุมเพื่อหยุดการหมุนสะบัด
                rb.AddTorque(-rb.angularVelocity * dampingCoefficient, ForceMode.Acceleration);

                // 3. RESTORING TORQUE: พยายามพยุงให้อาคารกลับมามีมุมเดิมก่อนเริ่มจำลอง (ป้องกันการเอนล้ม)
                if (_originalRotations.TryGetValue(rb, out Quaternion origRot))
                {
                    // คำนวณหาความแตกต่างในการหมุน
                    Quaternion deltaRot = origRot * Quaternion.Inverse(rb.transform.rotation);
                    deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
                    
                    if (angle > 180f) angle -= 360f;

                    // ป้องกันความผิดพลาดของแกนที่อาจเกิดค่า NaN หรือเวกเตอร์ศูนย์ซึ่งจะส่งผลให้ฟิสิกส์ระเบิด/พังหมด
                    if (Mathf.Abs(angle) > 0.1f && axis.sqrMagnitude > 0.001f)
                    {
                        Vector3 restoringTorque = axis.normalized * (angle * restoringStrength * Mathf.Deg2Rad);
                        restoringTorque.y = 0f; // เอาแรงพยุงในแกน Y ออก (ไม่พยุง/บิดคืนในแกนดิ่ง)

                        if (!float.IsNaN(restoringTorque.x) && !float.IsNaN(restoringTorque.y) && !float.IsNaN(restoringTorque.z))
                        {
                            rb.AddTorque(restoringTorque, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }
    }
}
