using UnityEngine;

namespace Simulation.Character
{
    /// <summary>
    /// สคริปต์สำหรับแปะที่ Prefab คนที่ให้ผู้เล่นวาง (Target Marker)
    /// จะทำให้ตัวมันใสๆ (Transparent) เมื่อถูกวางลงในฉาก
    /// </summary>
    public class PersonTarget : MonoBehaviour
    {
        [Header("Transparency")]
        [SerializeField] private float alpha = 0.15f;
        [SerializeField] private float fadeSpeed = 2f;

        [Header("Mission Settings")]
        [Tooltip("ถ้านับเป็นคนในภารกิจ จะถูกนำไปคำนวณจำนวนคนรอดและดาว")]
        public bool countsTowardsPopulation = true;

        private bool _shouldFade = false;
        private Material[] _materials;
        private float _currentAlpha;
        private PersonAI _occupant; // NPC ที่เดินมาถึงเป้าหมายนี้แล้ว

        private void Awake()
        {
        }

        private void Start()
        {
            _currentAlpha = alpha;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            System.Collections.Generic.List<Material> mats = new System.Collections.Generic.List<Material>();

            foreach (var r in renderers)
            {
                if (r.material != null)
                {
                    Material mat = r.material; // สร้าง instance ของ material
                    
                    // --- พยายามตั้งค่าให้เป็น Transparent (สำหรับ Standard Shader) ---
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    // --- สำหรับ URP Lit Shader ---
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);     // 0 = Alpha
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                    // ตั้งค่าสีเริ่มต้น
                    Color c = mat.color;
                    c.a = _currentAlpha;
                    mat.color = c;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);

                    mats.Add(mat);
                }
            }
            _materials = mats.ToArray();

            // ปิด Collider ไม่ให้เป็นส่วนหนึ่งของฟิสิกส์สิ่งก่อสร้าง
            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var col in cols)
            {
                col.isTrigger = true; 
            }
        }

        public void StartFadeOut(PersonAI person)
        {
            if (!_shouldFade || _occupant == null)
            {
                _occupant = person;
                _shouldFade = true;
                Debug.Log($"<color=yellow>[PersonTarget]</color> {gameObject.name} starting fade out (Occupied by {person.name}).");
            }
        }

        private void Update()
        {
            // ตรวจสอบว่ามี materials หรือไม่ (เผื่อกรณีมีการ Instantiate ใหม่หรือ Start ยังไม่ทำงาน)
            if (_materials == null || _materials.Length == 0)
            {
                InitializeMaterials();
            }

            // หาเป้าหมายความทึบตามสถานะของการจำลอง
            bool isSimulating = Simulation.Physics.SimulationManager.Instance != null && Simulation.Physics.SimulationManager.Instance.IsSimulating;
            
            // รีเซ็ตสถานะการ Fade
            if (!isSimulating)
            {
                // หากหยุดจำลอง ให้กลับมาแสดงผลทันทีและล้างข้อมูลผู้อยู่อาศัย
                _shouldFade = false;
                _occupant = null;
                _currentAlpha = alpha; 
            }
            else if (_shouldFade)
            {
                // หากกำลังจำลอง และคนที่เคยมาถึงตาย (ถูกกิน) ให้กลับมาแสดงผล
                if (_occupant == null || _occupant.IsDead)
                {
                    _shouldFade = false;
                    _occupant = null;
                    Debug.Log($"<color=yellow>[PersonTarget]</color> {gameObject.name} reappearing because occupant is gone/dead.");
                }
            }

            // คำนวณ Alpha เป้าหมาย
            float targetAlpha = _shouldFade ? 0f : alpha;

            // ค่อยๆ Fade สีถ้ากำลังจำลอง
            if (isSimulating)
            {
                if (Mathf.Abs(_currentAlpha - targetAlpha) > 0.001f)
                {
                    _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
                }
            }

            // อัปเดตสีของ Material
            ApplyAlphaToMaterials();
        }

        private void InitializeMaterials()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Material> mats = new System.Collections.Generic.List<Material>();

            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                {
                    mats.Add(r.material);
                }
            }
            _materials = mats.ToArray();
        }

        private void ApplyAlphaToMaterials()
        {
            if (_materials == null) return;
            
            foreach (var mat in _materials)
            {
                if (mat != null)
                {
                    Color c = mat.color;
                    c.a = _currentAlpha;
                    mat.color = c;

                    // รองรับ URP/HDRP
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", c);
                    }
                }
            }
        }
    }
}
