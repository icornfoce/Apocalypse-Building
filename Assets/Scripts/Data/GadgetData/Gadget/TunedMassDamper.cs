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

        [Tooltip("แรงดึงลงเพิ่มเติมเพื่อช่วยชดเชยอาการตึกตกช้าเวลาพังหรือเอียง (ยิ่งเยอะยิ่งตกเร็ว)")]
        public float extraGravityCompensation = 5f;

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
        private Quaternion _myOriginalRotation;
        private bool _isInitialized = false;
        private ConfigurableJoint _pendulumJoint;
        private Rigidbody _pendulumRb;

        // ตัวแปรสำหรับ Backup เชือกและลูกตุ้มเพื่อกู้คืนเมื่อซิมพัง/รีเซ็ต
        private GameObject _ropeBackup;
        private GameObject _pendulumBackup;
        private Transform _ropeOriginalParent;
        private Vector3 _ropeOriginalLocalPos;
        private Quaternion _ropeOriginalLocalRot;
        private Vector3 _ropeOriginalLocalScale;
        private Transform _pendulumOriginalParent;
        private Vector3 _pendulumOriginalLocalPos;
        private Quaternion _pendulumOriginalLocalRot;
        private Vector3 _pendulumOriginalLocalScale;
        private string _pendulumOriginalName;
        private bool _isPendulumChildOfRope;

        private struct SavedTransform
        {
            public Transform transform;
            public Vector3 localPos;
            public Quaternion localRot;
            public Rigidbody rb;
        }
        private List<SavedTransform> _savedTransforms = new List<SavedTransform>();
        private bool _hasSavedOriginalTransform = false;
        private bool _wasSimulatingLastFrame = false;

        private void Start()
        {
            AutoFindReferences();
            SaveOriginalPendulumTransform();
            
            // ล็อกตำแหน่งชิ้นส่วนทั้งหมดทันทีที่ถูกสร้างขึ้นมา (กันมันร่วงก่อนเริ่มจำลอง)
            foreach (var st in _savedTransforms)
            {
                if (st.rb != null) st.rb.isKinematic = true;
            }
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
                if (_hasSavedOriginalTransform)
                {
                    foreach (var st in _savedTransforms)
                    {
                        if (st.rb != null)
                        {
                            if (!st.rb.isKinematic)
                            {
                                st.rb.linearVelocity = Vector3.zero;
                                st.rb.angularVelocity = Vector3.zero;
                                st.rb.isKinematic = true;
                            }
                        }
                        if (st.transform != null)
                        {
                            st.transform.localPosition = st.localPos;
                            st.transform.localRotation = st.localRot;
                        }
                    }
                }
            }
        }

        private void SaveOriginalPendulumTransform()
        {
            if (!_hasSavedOriginalTransform)
            {
                // 1. บันทึกข้อมูลตำแหน่ง พ่อ และสเกลเริ่มต้น
                if (ropeTransform != null)
                {
                    _ropeOriginalParent = ropeTransform.parent;
                    _ropeOriginalLocalPos = ropeTransform.localPosition;
                    _ropeOriginalLocalRot = ropeTransform.localRotation;
                    _ropeOriginalLocalScale = ropeTransform.localScale;
                }

                if (pendulumTransform != null)
                {
                    _pendulumOriginalParent = pendulumTransform.parent;
                    _pendulumOriginalLocalPos = pendulumTransform.localPosition;
                    _pendulumOriginalLocalRot = pendulumTransform.localRotation;
                    _pendulumOriginalLocalScale = pendulumTransform.localScale;
                    _pendulumOriginalName = pendulumTransform.name;
                }

                if (ropeTransform != null && pendulumTransform != null)
                {
                    _isPendulumChildOfRope = pendulumTransform.IsChildOf(ropeTransform);
                }

                // 2. สร้างวัตถุสำรอง (Backup) ไว้ใต้ TMD เพื่อคัดลอกคืนยามเริ่มซิมใหม่
                if (ropeTransform != null)
                {
                    _ropeBackup = Instantiate(ropeTransform.gameObject, this.transform);
                    _ropeBackup.name = ropeTransform.name + "_Backup";
                    _ropeBackup.SetActive(false);
                }

                if (pendulumTransform != null && !_isPendulumChildOfRope)
                {
                    _pendulumBackup = Instantiate(pendulumTransform.gameObject, this.transform);
                    _pendulumBackup.name = pendulumTransform.name + "_Backup";
                    _pendulumBackup.SetActive(false);
                }

                // 3. บันทึกรายการ Transforms และ Rigidbody ที่ใช้งานอยู่
                RebuildSavedTransformsList();

                _hasSavedOriginalTransform = true;
            }
        }

        private void RebuildSavedTransformsList()
        {
            _savedTransforms.Clear();

            if (ropeTransform != null)
            {
                var rbs = ropeTransform.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (rb.gameObject == this.gameObject) continue;
                    
                    _savedTransforms.Add(new SavedTransform { 
                        transform = rb.transform, 
                        localPos = rb.transform.localPosition, 
                        localRot = rb.transform.localRotation, 
                        rb = rb
                    });
                }
            }

            if (pendulumTransform != null)
            {
                var rbs = pendulumTransform.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (rb.gameObject == this.gameObject) continue;
                    if (!_savedTransforms.Exists(x => x.rb == rb))
                    {
                        _savedTransforms.Add(new SavedTransform { 
                            transform = rb.transform, 
                            localPos = rb.transform.localPosition, 
                            localRot = rb.transform.localRotation, 
                            rb = rb 
                        });
                    }
                }
            }
        }

        public void ResetPendulum()
        {
            _isInitialized = false;

            if (_hasSavedOriginalTransform)
            {
                // ลบ Joint เก่าเฉพาะที่ถูกสร้างด้วยโค้ดตอน SetupPendulumJoint
                if (_pendulumJoint != null)
                {
                    DestroyImmediate(_pendulumJoint);
                    _pendulumJoint = null;
                }

                // 1. ทำลายเชือกและลูกตุ้มตัวปัจจุบันที่อาจพัง/กระจัดกระจายไปแล้ว
                if (ropeTransform != null)
                {
                    ropeTransform.gameObject.name = "Rope_Destroying";
                    if (Application.isPlaying) Destroy(ropeTransform.gameObject);
                    else DestroyImmediate(ropeTransform.gameObject);
                    ropeTransform = null;
                }
                if (pendulumTransform != null)
                {
                    pendulumTransform.gameObject.name = "Pendulum_Destroying";
                    if (Application.isPlaying) Destroy(pendulumTransform.gameObject);
                    else DestroyImmediate(pendulumTransform.gameObject);
                    pendulumTransform = null;
                }

                // 2. สร้างใหม่จากตัวสำรอง (Backup) เพื่อให้ได้เชือกที่มี ConfigurableJoint สมบูรณ์เหมือนเดิมร้อยเปอร์เซ็นต์
                if (_ropeBackup != null)
                {
                    GameObject newRope = Instantiate(_ropeBackup, _ropeOriginalParent);
                    newRope.name = _ropeBackup.name.Replace("_Backup", "");
                    newRope.transform.localPosition = _ropeOriginalLocalPos;
                    newRope.transform.localRotation = _ropeOriginalLocalRot;
                    newRope.transform.localScale = _ropeOriginalLocalScale;
                    newRope.SetActive(true);
                    ropeTransform = newRope.transform;
                }

                if (_pendulumBackup != null)
                {
                    GameObject newPendulum = Instantiate(_pendulumBackup, _pendulumOriginalParent);
                    newPendulum.name = _pendulumBackup.name.Replace("_Backup", "");
                    newPendulum.transform.localPosition = _pendulumOriginalLocalPos;
                    newPendulum.transform.localRotation = _pendulumOriginalLocalRot;
                    newPendulum.transform.localScale = _pendulumOriginalLocalScale;
                    newPendulum.SetActive(true);
                    pendulumTransform = newPendulum.transform;
                }
                else if (_ropeBackup != null && _isPendulumChildOfRope)
                {
                    // หากลูกตุ้มเป็นลูกของเชือก ให้ค้นหาจากเชือกอันใหม่ที่สร้างขึ้นมา
                    pendulumTransform = FindChildRecursive(ropeTransform, _pendulumOriginalName);
                }

                // 3. ปรับปรุงรายการ Saved Transforms ให้ชี้ไปยังวัตถุและ Rigidbody ตัวใหม่ที่ถูกโคลน
                RebuildSavedTransformsList();

                // 4. ล็อกชิ้นส่วนทั้งหมดให้คงที่ (isKinematic = true) ป้องกันมันร่วงก่อนกดเริ่มซิม
                foreach (var st in _savedTransforms)
                {
                    if (st.rb != null)
                    {
                        st.rb.linearVelocity = Vector3.zero;
                        st.rb.angularVelocity = Vector3.zero;
                        st.rb.isKinematic = true;
                    }
                }
            }
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
            _pendulumRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // ปลดล็อก (isKinematic = false) ให้กับชิ้นส่วนเชือกทุกชิ้น (16 joints) ที่ถูกล็อกไว้
            // พร้อมตั้งค่า Break Force เพื่อให้เชือกขาดได้เมื่อโดนกระชากหรือตึกถล่ม
            foreach (var st in _savedTransforms)
            {
                if (st.rb != null && st.rb != _pendulumRb)
                {
                    st.rb.isKinematic = false;
                    st.rb.useGravity = true;
                    st.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                    Joint[] ropeJoints = st.rb.GetComponents<Joint>();
                    foreach (var rj in ropeJoints)
                    {
                        // ตั้งค่าให้เชือกทนแรงกระชากได้ระดับหนึ่ง (ถ้ากระชากแรงมาก หรือตึกถล่ม เชือกจะขาด)
                        rj.breakForce = 2000f; 
                        rj.breakTorque = 2000f;
                    }
                }
            }

            // สั่งให้ Collider ของลูกตุ้มและเชือก มองข้ามการชนกับโครงสร้างของตัว TMD เองทั้งหมด
            // (แก้ปัญหาทะลุพื้น เพราะถ้าชนกันเองจะเกิดแรงผลักมหาศาลทำให้วัตถุบั๊กทะลุพื้น)
            Collider[] pendulumCols = ropeTransform != null ? ropeTransform.GetComponentsInChildren<Collider>() : pendulumTransform.GetComponentsInChildren<Collider>();
            Collider[] allTmdCols = GetComponentsInChildren<Collider>(); 

            foreach (var sCol in allTmdCols)
            {
                // ตรวจสอบว่า sCol ไม่ใช่ส่วนหนึ่งของลูกตุ้มหรือเชือก
                bool isPendulumPart = false;
                foreach (var pCol in pendulumCols)
                {
                    if (sCol == pCol)
                    {
                        isPendulumPart = true;
                        break;
                    }
                }

                if (isPendulumPart) continue;

                foreach (var pCol in pendulumCols)
                {
                    if (sCol != null && pCol != null && sCol != pCol)
                    {
                        UnityEngine.Physics.IgnoreCollision(sCol, pCol, true);
                    }
                }
            }

            Debug.Log($"<color=green>[TunedMassDamper]</color> Joints unlocked for pendulum '{pendulumTransform.name}'");
        }

        private void InitializeConnectedBuilding()
        {
            _connectedRigidbodies.Clear();
            _originalRotations.Clear();
            _myOriginalRotation = transform.rotation;

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

            // 4. GRAVITY COMPENSATION: เพิ่มแรงดึงลงที่ตัว TMD เพื่อชดเชยอาการตกช้า (ดึงเฉพาะตัว TMD)
            // ให้ทำงานเฉพาะเมื่อมี restoringStrength และโครงสร้างมีการเอียง (แปลว่า Restoring กำลังออกแรงฝืนอยู่)
            if (extraGravityCompensation > 0f && restoringStrength > 0f)
            {
                Rigidbody myRb = GetComponent<Rigidbody>();
                if (myRb != null)
                {
                    Quaternion deltaRot = _myOriginalRotation * Quaternion.Inverse(myRb.transform.rotation);
                    deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 180f) angle -= 360f;

                    // ถ้ามีการเอียงเกิน 0.1 องศา แสดงว่า restoringTorque กำลังพยายามดึงกลับ
                    if (Mathf.Abs(angle) > 0.1f)
                    {
                        // สัดส่วนแรงตามมุมตก (ยิ่งเอียงยิ่งดึงลงมากสุดที่ 100%) เพื่อความสมูท
                        float fallMultiplier = Mathf.Clamp01(Mathf.Abs(angle) / 10f);
                        myRb.AddForce(Vector3.down * (extraGravityCompensation * fallMultiplier), ForceMode.Acceleration);
                    }
                }
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
