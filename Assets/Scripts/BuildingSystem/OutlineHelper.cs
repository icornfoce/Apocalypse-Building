using UnityEngine;

namespace Simulation.Building
{
    /// <summary>
    /// ตัวช่วยเปิด/ปิด Outline (toon) ของ QuickOutline บน GameObject
    /// ใช้ตอนกำลัง "ลาก/วาง", "ย้าย", หรือ "ลบ" โครงสร้าง
    /// (Outline เป็นคลาส global ของ QuickOutline — ไม่มี namespace)
    /// </summary>
    public static class OutlineHelper
    {
        /// <summary>เพิ่ม/เปิด Outline บน go (ครอบคลุม mesh ลูกทั้งหมด)</summary>
        public static void Apply(GameObject go, Color color, float width)
        {
            if (go == null) return;

            if (!CanUseQuickOutline(go))
            {
                Disable(go);
                return;
            }

            Outline o = go.GetComponent<Outline>();
            if (o == null) o = go.AddComponent<Outline>();
            o.OutlineMode = Outline.Mode.OutlineAll;
            o.OutlineColor = color;
            o.OutlineWidth = width;
            o.enabled = true;
        }

        /// <summary>ปิด Outline (ไม่ลบ component เพื่อกัน re-bake — แค่ปิดการแสดงผล)</summary>
        public static void Disable(GameObject go)
        {
            if (go == null) return;
            Outline o = go.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        private static bool CanUseQuickOutline(GameObject go)
        {
            foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null && !mesh.isReadable) return false;
            }

            foreach (var skinnedMeshRenderer in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                if (mesh != null && !mesh.isReadable) return false;
            }

            return true;
        }
    }
}
