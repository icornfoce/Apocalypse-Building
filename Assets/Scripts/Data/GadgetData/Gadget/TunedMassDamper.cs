using UnityEngine;
using System.Collections.Generic;
using Simulation.Physics;

namespace Simulation.Building
{
    /// <summary>
    /// Tuned Mass Damper (TMD) — เครื่องลดการสั่นและแกว่งตัวของตึก
    /// 1. ช่วยลดการแกว่งโดยซับและหน่วงแรงสั่นสะเทือน (Damping) บนโครงสร้างที่เชื่อมต่ออยู่ทั้งหมด
    /// 2. ช่วยพยุงและออกแรงพยายามดึงโครงสร้างให้กลับมาอยู่ในแนวตั้งตรง (Restoring Torque)
    /// 3. มี Pendulum Visual (ลูกตุ้มถ่วง) แกว่งไปมาในทิศทางตรงกันข้ามโชว์กราฟิกสวยงาม
    /// </summary>
    public class TunedMassDamper : MonoBehaviour
    {
        [Header("TMD Physics Settings")]
        [Tooltip("สัมประสิทธิ์การหน่วงความเร็ว (ความหน่วงเชิงเส้นและเชิงมุม)")]
        public float dampingCoefficient = 0.8f;
        
        [Tooltip("ความแรงในการดึงโครงสร้างกลับมาแนวตั้งตรง")]
        public float restoringStrength = 15f;

        [Header("Pendulum Visual Settings")]
        public float pendulumLength = 1.5f;
        public float weightSize = 0.4f;
        public float springConstant = 8f;
        public float weightDamping = 1.5f;
        public float inertiaScale = 0.5f;

        // รายการ Rigidbody และมุมเริ่มต้นของอาคารที่เชื่อมต่อกัน
        private List<Rigidbody> _connectedRigidbodies = new List<Rigidbody>();
        private Dictionary<Rigidbody, Quaternion> _originalRotations = new Dictionary<Rigidbody, Quaternion>();
        private bool _isInitialized = false;

        // สำหรับจำลองฟิสิกส์ภายในของลูกตุ้มถ่วง (Pendulum Mass) ใน Local Space
        private Transform _weightTransform;
        private LineRenderer _cableRenderer;
        private Vector3 _localWeightPos;
        private Vector3 _localWeightVelocity;
        private Vector3 _lastTmdPos;
        private Vector3 _lastTmdVelocity;

        private void Start()
        {
            _lastTmdPos = transform.position;
            _lastTmdVelocity = Vector3.zero;

            // สร้างส่วนแสดงผล Pendulum แบบ Procedural
            CreatePendulumVisual();
        }

        private void OnEnable()
        {
            // ลงทะเบียนอีเวนต์ หรือเตรียมตัวเมื่อเริ่มจำลอง
            _isInitialized = false;
        }

        private void FixedUpdate()
        {
            // ตรวจสอบสถานะการจำลอง
            if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating)
            {
                _isInitialized = false;
                ResetPendulumLocalPos();
                return;
            }

            // ถ้าจำลองเริ่มทำงานแล้ว และยังไม่ได้เชื่อมต่อโครงสร้าง
            if (!_isInitialized)
            {
                InitializeConnectedBuilding();
            }

            // 1. ทำงาน Damping และ Restoring บน Rigidbodies ของตึก
            ApplyStabilizationForces();

            // 2. จำลองฟิสิกส์ของลูกตุ้มถ่วง (Pendulum) เพื่อแสดงผลความสวยงาม
            SimulatePendulumPhysics();
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

        private void ApplyStabilizationForces()
        {
            for (int i = _connectedRigidbodies.Count - 1; i >= 0; i--)
            {
                Rigidbody rb = _connectedRigidbodies[i];
                if (rb == null || !rb.gameObject.activeInHierarchy)
                {
                    _connectedRigidbodies.RemoveAt(i);
                    continue;
                }

                // ข้ามโครงสร้างที่หลุดร่วง/ถล่มไปแล้ว (ไม่มี Joint เกาะ)
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
                        if (!float.IsNaN(restoringTorque.x) && !float.IsNaN(restoringTorque.y) && !float.IsNaN(restoringTorque.z))
                        {
                            rb.AddTorque(restoringTorque, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }

        private void CreatePendulumVisual()
        {
            // สร้างเส้นสายสลิง
            GameObject cableGO = new GameObject("TMD_Cable");
            cableGO.transform.SetParent(transform);
            cableGO.transform.localPosition = Vector3.zero;

            _cableRenderer = cableGO.AddComponent<LineRenderer>();
            _cableRenderer.startWidth = 0.05f;
            _cableRenderer.endWidth = 0.05f;
            _cableRenderer.positionCount = 2;
            _cableRenderer.useWorldSpace = true;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            Material mat = new Material(shader != null ? shader : Shader.Find("Hidden/Internal-Colored"));
            mat.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            _cableRenderer.material = mat;

            // สร้างลูกตุ้มถ่วงสีเงิน
            GameObject weightGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            weightGO.name = "TMD_Weight";
            weightGO.transform.SetParent(transform);
            
            // ลบ Collider ออกแบบทันทีเพื่อป้องกันการชนกระแทกโครงสร้างอื่นก่อนทำลายเสร็จสิ้น
            var col = weightGO.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            weightGO.transform.localScale = Vector3.one * weightSize;
            
            var renderer = weightGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material sphereMat = new Material(Shader.Find("Standard"));
                sphereMat.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                sphereMat.SetFloat("_Metallic", 0.9f);
                sphereMat.SetFloat("_Glossiness", 0.8f);
                renderer.material = sphereMat;
            }

            _weightTransform = weightGO.transform;
            ResetPendulumLocalPos();
        }

        private void ResetPendulumLocalPos()
        {
            _localWeightPos = Vector3.down * pendulumLength;
            _localWeightVelocity = Vector3.zero;
            if (_weightTransform != null)
            {
                _weightTransform.localPosition = _localWeightPos;
            }
            UpdateCableVisual();
        }

        private void SimulatePendulumPhysics()
        {
            if (_weightTransform == null) return;

            float dt = Time.fixedDeltaTime;

            // คำนวณความเร่งของตัว TMD (ตัวฐานของตึก) เพื่อส่งแรงเฉื่อยไปยังลูกตุ้มในทิศตรงข้าม
            Vector3 currentVel = (transform.position - _lastTmdPos) / (dt > 0 ? dt : 0.02f);
            Vector3 tmdAcceleration = (currentVel - _lastTmdVelocity) / (dt > 0 ? dt : 0.02f);
            
            _lastTmdPos = transform.position;
            _lastTmdVelocity = currentVel;

            // แปลงความเร่งเป็นทิศทางภายใน (Local Space) เพื่อคำนวณง่ายขึ้น
            Vector3 localAccel = transform.InverseTransformDirection(tmdAcceleration);

            // 1. แรงดึงกลับจากสปริง (Spring force towards bottom center)
            Vector3 restPos = Vector3.down * pendulumLength;
            Vector3 springForce = (restPos - _localWeightPos) * springConstant;

            // 2. แรงเฉื่อยต้านการเคลื่อนที่ (Inertia force due to base acceleration)
            Vector3 inertiaForce = -localAccel * inertiaScale;

            // 3. แรงต้านหน่วงอากาศ (Damping force)
            Vector3 dampForce = -_localWeightVelocity * weightDamping;

            // ผลรวมความเร่งของลูกตุ้ม
            Vector3 totalAccel = springForce + inertiaForce + dampForce;
            
            // เพิ่มความเร่งจากแรงโน้มถ่วงจำลองดึงหัวตกเสมอ (ถ้าร่างเอียง)
            Vector3 localGravity = transform.InverseTransformDirection(UnityEngine.Physics.gravity);
            totalAccel += (localGravity - Vector3.Project(localGravity, Vector3.up)) * 0.5f;

            // ทำการ Integrate ความเร็วและตำแหน่ง
            _localWeightVelocity += totalAccel * dt;
            _localWeightPos += _localWeightVelocity * dt;

            // จำกัดทิศไม่ให้ยืดเกินความยาวสาย (Spherical constraint)
            _localWeightPos = _localWeightPos.normalized * pendulumLength;

            // อัปเดตตำแหน่งลูกตุ้มและสาย
            _weightTransform.localPosition = _localWeightPos;
            UpdateCableVisual();
        }

        private void UpdateCableVisual()
        {
            if (_cableRenderer == null || _weightTransform == null) return;
            
            // วาดเส้นจากจุดหมุน TMD ด้านบน ไปหาลูกตุ้มด้านล่าง
            _cableRenderer.SetPosition(0, transform.position);
            _cableRenderer.SetPosition(1, _weightTransform.position);
        }
    }
}
