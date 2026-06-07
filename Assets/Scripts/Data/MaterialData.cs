using UnityEngine;

namespace Simulation.Data
{
    [CreateAssetMenu(fileName = "New Material Data", menuName = "Simulation/Material Data")]
    public class MaterialData : ScriptableObject
    {
        [Header("General Info")]
        public string materialName;
        public float priceModifier;
        [Tooltip("Mass multiplier applied to the structure's base mass.")]
        public float massMultiplier = 1f;
        [Tooltip("HP multiplier applied to the structure's base HP.")]
        public float hpMultiplier = 1f;

        [Header("Assets")]
        public Material material;
        public AudioClip placeSound;
        public AudioClip breakSound;
        public GameObject placeVFX;
        public GameObject breakVFX;

        [Header("Limits (Multipliers)")]
        [Tooltip("Compression capacity multiplier applied to the structure's base maxCompression.")]
        public float compressionMultiplier = 1f;
        [Tooltip("Tension capacity multiplier applied to the structure's base maxTension.")]
        public float tensionMultiplier = 1f;

        [Header("Fire")]
        [Tooltip("วัสดุติดไฟและลามไฟได้ (เช่น ไม้)")]
        public bool flammable = false;
        [Tooltip("ตัวคูณดาเมจตอนกำลังไหม้ (ไม้ควร > 1 เพื่อให้ไหม้เร็ว)")]
        public float burnDamageMultiplier = 1f;
    }
}
