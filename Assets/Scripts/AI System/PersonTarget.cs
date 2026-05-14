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

        [Header("Mission Settings")]
        [Tooltip("ถ้านับเป็นคนในภารกิจ จะถูกนำไปคำนวณจำนวนคนรอดและดาว")]
        public bool countsTowardsPopulation = true;

        private void Start()
        {
            InitializeMaterials();
            
            // ปิด Collider ไม่ให้เป็นส่วนหนึ่งของฟิสิกส์สิ่งก่อสร้าง
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
            {
                col.isTrigger = true; 
            }
        }

        public void StartFadeOut(PersonAI person)
        {
            // ปิดการแสดงผลทันที (เหมือนระบบสิ่งก่อสร้างอื่นที่โดนทำลาย)
            // ระบบ Snapshot ของ SimulationManager จะเปิดกลับมาเองตอน Stop (RestoreSnapshots)
            gameObject.SetActive(false);
            Debug.Log($"<color=cyan>[PersonTarget]</color> {gameObject.name} occupied by {person.name} and disabled.");
        }

        public void ResetTarget()
        {
            // ฟังก์ชันเผื่อโดนเรียกจากที่อื่น ให้เปิดตัวเองกลับมา
            gameObject.SetActive(true);
        }

        private void InitializeMaterials()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                {
                    Material mat = r.material;
                    
                    // --- ตั้งค่าให้เป็น Transparent (Standard Shader) ---
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    // --- สำหรับ URP Lit Shader ---
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                    
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", c);
                    }
                }
            }
        }
    }
}
