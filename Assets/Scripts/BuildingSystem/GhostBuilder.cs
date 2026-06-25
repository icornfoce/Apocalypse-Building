using UnityEngine;
using System.Collections.Generic;

namespace Simulation.Building
{
    /// <summary>
    /// Manages the ghost (preview) object for the building system.
    /// Handles creation, color feedback (green/red), rotation, and cleanup.
    /// </summary>
    public class GhostBuilder : MonoBehaviour
    {
        [Header("Ghost Colors")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Outline (Toon) — ขณะลาก/วาง/ย้าย")]
        [Tooltip("เปิด Outline รอบ ghost ที่กำลังลาก/ย้าย")]
        [SerializeField] private bool useOutline = true;
        [SerializeField] private Color outlineValidColor = Color.white;
        [SerializeField] private Color outlineInvalidColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float outlineWidth = 5f;

        private List<GameObject> _ghostTemplates = new List<GameObject>();
        private List<Vector3> _templateOffsets = new List<Vector3>();
        private List<float> _templateRotations = new List<float>();
        private List<GameObject> _ghostInstances = new List<GameObject>();
        private List<Renderer> _ghostRenderers = new List<Renderer>();
        private List<Material> _ghostMaterials = new List<Material>();
        private float _currentRotation = 0f;
        private bool _isValid = true;

        public bool HasGhost => _ghostTemplates.Count > 0;
        public float CurrentRotation => _currentRotation;

        /// <summary>
        /// Create a ghost preview from a prefab.
        /// </summary>
        public void CreateGhost(GameObject prefab)
        {
            DestroyGhost();
            AddTemplate(prefab, Vector3.zero, 0f);
            UpdateGhosts(new List<Vector3> { Vector3.zero }, 0f, true);
        }

        /// <summary>
        /// Create a group ghost from multiple units.
        /// </summary>
        public void CreateGroupGhost(List<GameObject> prefabs, List<Vector3> offsets, List<float> rotations)
        {
            DestroyGhost();
            for (int i = 0; i < prefabs.Count; i++)
            {
                AddTemplate(prefabs[i], offsets[i], rotations[i]);
            }
            UpdateGroupGhosts(Vector3.zero, 0f, true);
        }

        private void AddTemplate(GameObject prefab, Vector3 offset, float rotation)
        {
            GameObject template = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            template.name = "Ghost_Template";
            template.SetActive(false);
            
            SetupGhost(template);
            
            _ghostTemplates.Add(template);
            _templateOffsets.Add(offset);
            _templateRotations.Add(rotation);
        }

        private void SetupGhost(GameObject obj)
        {
            // Disable all colliders
            foreach (var col in obj.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            // Disable all Rigidbodies
            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;

            // Remove logic components in children as well
            foreach (var comp in obj.GetComponentsInChildren<StructureUnit>(true)) DestroyImmediate(comp);
            foreach (var comp in obj.GetComponentsInChildren<Simulation.Physics.StructuralStress>(true)) DestroyImmediate(comp);
            foreach (var comp in obj.GetComponentsInChildren<Simulation.Character.PersonTarget>(true)) DestroyImmediate(comp);
            foreach (var comp in obj.GetComponentsInChildren<Simulation.Character.PersonSpawner>(true)) DestroyImmediate(comp);
            foreach (var comp in obj.GetComponentsInChildren<Simulation.Character.PersonAI>(true)) DestroyImmediate(comp);
            foreach (var comp in obj.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) DestroyImmediate(comp);
            foreach (var j in obj.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
        }

        private void AddInstance(GameObject template, Vector3 pos, float rotation)
        {
            GameObject inst = Instantiate(template, pos, Quaternion.Euler(0f, rotation, 0f));
            inst.name = "Ghost_Instance";
            inst.SetActive(true);
            _ghostInstances.Add(inst);

            foreach (var rend in inst.GetComponentsInChildren<Renderer>())
            {
                _ghostRenderers.Add(rend);
                foreach (var mat in rend.materials)
                {
                    SetupTransparentMaterial(mat);
                    _ghostMaterials.Add(mat);
                }
            }

            // Outline (toon) รอบ ghost ที่กำลังลาก/ย้าย
            if (useOutline) OutlineHelper.Apply(inst, _isValid ? outlineValidColor : outlineInvalidColor, outlineWidth);
        }

        private void SetupTransparentMaterial(Material mat)
        {
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        /// <summary>
        /// Update ghosts for single-prefab mode (e.g. dragging to place multiple items of same type).
        /// </summary>
        public void UpdateGhosts(List<Vector3> positions, float rotation, bool isValid)
        {
            if (_ghostTemplates.Count == 0) return;
            _currentRotation = rotation;

            GameObject template = _ghostTemplates[0];

            // Sync instance count
            while (_ghostInstances.Count < positions.Count) AddInstance(template, Vector3.zero, rotation);
            while (_ghostInstances.Count > positions.Count)
            {
                GameObject last = _ghostInstances[_ghostInstances.Count - 1];
                _ghostInstances.RemoveAt(_ghostInstances.Count - 1);
                Renderer[] rends = last.GetComponentsInChildren<Renderer>();
                foreach(var r in rends) _ghostRenderers.Remove(r);
                Destroy(last);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                _ghostInstances[i].transform.position = positions[i];
                _ghostInstances[i].transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            SetValid(isValid);
        }

        /// <summary>
        /// Update ghosts for group mode (different prefabs with relative offsets).
        /// </summary>
        public void UpdateGroupGhosts(Vector3 anchorPos, float groupRotation, bool isValid)
        {
            if (_ghostTemplates.Count == 0) return;
            _currentRotation = groupRotation;

            // Sync instance count to template count
            while (_ghostInstances.Count < _ghostTemplates.Count)
            {
                int idx = _ghostInstances.Count;
                AddInstance(_ghostTemplates[idx], Vector3.zero, 0f);
            }

            Quaternion groupRotQ = Quaternion.Euler(0, groupRotation, 0);

            for (int i = 0; i < _ghostTemplates.Count; i++)
            {
                // Apply group rotation to the offset
                Vector3 rotatedOffset = groupRotQ * _templateOffsets[i];
                Vector3 targetPos = anchorPos + rotatedOffset;
                _ghostInstances[i].transform.position = targetPos;
                
                // Apply both group rotation and original template rotation
                _ghostInstances[i].transform.rotation = groupRotQ * Quaternion.Euler(0, _templateRotations[i], 0);
            }

            SetValid(isValid);
        }

        public void Rotate()
        {
            SetRotation((_currentRotation + 90f) % 360f);
        }

        public void SetRotation(float angle)
        {
            _currentRotation = angle;
            if (_ghostTemplates.Count == 1 && _ghostInstances.Count > 1)
            {
                // Multi-instance single template mode
                foreach (var inst in _ghostInstances) inst.transform.rotation = Quaternion.Euler(0f, _currentRotation, 0f);
            }
            else
            {
                // Re-run UpdateGroupGhosts to handle offset rotation
                // We'll let BuildingSystem call UpdateGroupGhosts properly
            }
        }

        public void UpdatePosition(Vector3 snappedPosition)
        {
            // If we have templates (even just one), use the group update logic
            // which respects the relative offsets from the anchor.
            if (_ghostTemplates.Count > 0) UpdateGroupGhosts(snappedPosition, _currentRotation, _isValid);
            else UpdateGhosts(new List<Vector3> { snappedPosition }, _currentRotation, _isValid);
        }

        public void DestroyGhost()
        {
            foreach (var t in _ghostTemplates) if (t != null) Destroy(t);
            foreach (var inst in _ghostInstances) if (inst != null) Destroy(inst);
            
            _ghostTemplates.Clear();
            _templateOffsets.Clear();
            _templateRotations.Clear();
            _ghostInstances.Clear();
            _ghostRenderers.Clear();
            _ghostMaterials.Clear();
            _currentRotation = 0f;
        }

        public void SetValid(bool isValid)
        {
            _isValid = isValid;
            Color targetColor = isValid ? validColor : invalidColor;

            if (_ghostMaterials.Count == 0 && _ghostRenderers.Count > 0)
            {
                foreach(var rend in _ghostRenderers)
                {
                    if (rend == null) continue;
                    foreach(var mat in rend.materials) _ghostMaterials.Add(mat);
                }
            }

            for (int i = _ghostMaterials.Count - 1; i >= 0; i--)
            {
                if (_ghostMaterials[i] == null) { _ghostMaterials.RemoveAt(i); continue; }
                _ghostMaterials[i].color = targetColor;
            }

            // อัปเดตสี Outline ตามสถานะวาง (valid/invalid)
            if (useOutline)
            {
                Color oc = isValid ? outlineValidColor : outlineInvalidColor;
                foreach (var inst in _ghostInstances)
                    if (inst != null) OutlineHelper.Apply(inst, oc, outlineWidth);
            }
        }

        public bool IsValid => _isValid;
    }
}
