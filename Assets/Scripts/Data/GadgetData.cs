using UnityEngine;

namespace Simulation.Data
{
    [CreateAssetMenu(fileName = "New Gadget Data", menuName = "Simulation/Gadget Data")]
    public class GadgetData : ScriptableObject
    {
        [Header("General Info")]
        public string GadgetName;
        public float Price;
        public float Mass;
        public float HP;
        public Vector3 size = Vector3.one;
        [Tooltip("If true, this Gadget can overlap with others")]
        public bool allowOverlap = false;

        [Header("Gadget Settings")]
        public bool isSpikeTrap = false;
        public float trapDamage = 20f;

        [Header("Gadget Settings - Balloon Launcher")]
        public bool isBalloonLauncher = false;
        public float shootRange = 20f;
        public float shootCooldown = 1.5f;
        public GameObject muzzleFlashPrefab;
        public AudioClip shootSound;

        [Header("Gadget Settings - Tuned Mass Damper")]
        public bool isTunedMassDamper = false;
        public float dampingCoefficient = 0.5f;
        public float restoringStrength = 10f;

        [Header("Assets")]
        public GameObject prefab;
        public MaterialData defaultMaterial;
        public AudioClip placeSound;
        public AudioClip breakSound;
        public GameObject placeVFX;
        public GameObject breakVFX;
    }
}
