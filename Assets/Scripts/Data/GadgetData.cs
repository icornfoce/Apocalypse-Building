using UnityEngine;

namespace Simulation.Data
{
    [CreateAssetMenu(fileName = "New Gadget Data", menuName = "Simulation/Gadget Data")]
    public class GadgetData : ScriptableObject
    {
        [Header("General Info")]
        public string furnitureName;
        public float Price;
        public float Mass;
        public float HP;
        public Vector3 size = Vector3.one;
        [Tooltip("If true, this furniture can overlap with others")]
        public bool allowOverlap = false;

        [Header("Gadget Settings")]
        public bool isSpikeTrap = false;
        public float trapDamage = 20f;

        [Header("Assets")]
        public GameObject prefab;
        public MaterialData defaultMaterial;
        public AudioClip placeSound;
        public AudioClip breakSound;
        public GameObject placeVFX;
        public GameObject breakVFX;
    }
}
