using UnityEngine;
using Simulation.Building;

namespace Simulation.Character
{
    /// <summary>
    /// สคริปต์สำหรับแปะที่ Prefab คนที่ให้ผู้เล่นวาง (Target Marker)
    /// จะทำให้ตัวมันใสๆ (Transparent) เมื่อถูกวางลงในฉาก
    /// </summary>
    public class PersonTarget : MonoBehaviour
    {
        [Header("Mission Settings")]
        [Tooltip("ถ้านับเป็นคนในภารกิจ จะถูกนำไปคำนวณจำนวนคนรอดและดาว")]
        public bool countsTowardsPopulation = true;

        [Header("Bobbing & Rotation Settings")]
        [SerializeField] private float floatSpeed = 2.0f;
        [Tooltip("ระยะลอยขึ้นลง (มากไป = ดูลอย/จมพื้น) — ปรับได้")]
        [SerializeField] private float floatAmplitude = 0.06f;
        [SerializeField] private float rotationSpeed = 45.0f; // degrees per second

        private float _localStartRawY;
        private bool _isInitialized = false;

        private void Start()
        {
            _localStartRawY = transform.localPosition.y;
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                // Reset local Y position when re-enabled to prevent offsets stacking
                transform.localPosition = new Vector3(transform.localPosition.x, _localStartRawY, transform.localPosition.z);
            }
        }

        private void Update()
        {
            // Bobbing (floating up and down locally)
            float newLocalY = _localStartRawY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = new Vector3(transform.localPosition.x, newLocalY, transform.localPosition.z);

            // Rotating
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }

        public void StartFadeOut(PersonAI person)
        {
            // ปิดการแสดงผลทั้ง Parent (StructureUnit)
            // ระบบ Snapshot ของ SimulationManager จะเปิดกลับมาเองตอน Stop (RestoreSnapshots)
            StructureUnit unit = GetComponent<StructureUnit>();
            if (unit == null) unit = GetComponentInParent<StructureUnit>();

            if (unit != null)
            {
                unit.gameObject.SetActive(false);
                Debug.Log($"<color=cyan>[PersonTarget]</color> Parent StructureUnit {unit.gameObject.name} occupied by {person.name} and disabled.");
            }
            else
            {
                gameObject.SetActive(false);
                Debug.Log($"<color=cyan>[PersonTarget]</color> {gameObject.name} occupied by {person.name} and disabled (No parent StructureUnit found).");
            }
        }

        public void ResetTarget()
        {
            // เช็คว่าตัวมันหรือพ่อมันเป็น StructureUnit ที่ถูกวางอยู่จริง ไม่ใช่ถูกลบ (sold/inactive)
            StructureUnit unit = GetComponent<StructureUnit>();
            if (unit == null) unit = GetComponentInParent<StructureUnit>();
            
            if (unit != null && Simulation.Building.BuildingSystem.Instance != null)
            {
                if (!Simulation.Building.BuildingSystem.Instance.IsStructurePlaced(unit))
                {
                    // ถ้าไม่อยู่ใน PlacedStructures แสดงว่าถูกลบ (sold) ไปแล้ว ห้ามเปิดคืน!
                    return;
                }
            }
            gameObject.SetActive(true);
        }
    }
}
