using UnityEngine;
using System.Collections.Generic;
using Simulation.Data;

namespace Simulation.Building
{
    public class StructureUnit : MonoBehaviour
    {
        [SerializeField] private StructureData data;
        [SerializeField] private MaterialData currentMaterial;
        
        private float _currentHP;
        private float _rotation;
        private float _additionalRefundValue; // ใช้เก็บมูลค่าของที่ถูกแทนที่ (เช่น กำแพงที่โดนประตูทับ)

        // Highlight
        private List<Renderer> _renderers = new List<Renderer>();
        private List<Color> _originalColors = new List<Color>();
        private bool _isHighlighted;

        [Header("Highlight")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

        [Tooltip("เปิด Outline (toon) ตอนถูกไฮไลต์ (hover ตอนย้าย/ลบ หรือเลือกหลายชิ้น)")]
        [SerializeField] private bool useOutline = true;
        [SerializeField] private float outlineWidth = 5f;

        public StructureData Data => data;
        public MaterialData CurrentMaterial => currentMaterial;
        public float CurrentHP => _currentHP;
        public float Rotation => _rotation;
        public bool IsHighlighted => _isHighlighted;
        public float AdditionalRefundValue => _additionalRefundValue;
        
        public void SetAdditionalRefundValue(float value) { _additionalRefundValue = value; }

        public void Initialize(StructureData structureData, MaterialData materialData, float rotation = 0f)
        {
            data = structureData;
            
            bool isGadget = data != null && data.isGadget;
            currentMaterial = isGadget ? null : materialData;

            // HP = Base * Multiplier
            float maxHP = data.baseHP * (currentMaterial != null ? currentMaterial.hpMultiplier : 1f);
            _currentHP = maxHP;

            _rotation = rotation;
            CacheRenderers();
            if (!isGadget)
            {
                ApplyMaterial();
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            if (rb != null)
            {
                bool isSimulating = Simulation.Physics.SimulationManager.Instance != null && Simulation.Physics.SimulationManager.Instance.IsSimulating;
                rb.isKinematic = !isSimulating;
                rb.useGravity = true;
                rb.mass = (data.baseMass * (currentMaterial != null ? currentMaterial.massMultiplier : 1f)) / 100f;

                // เพิ่มรอบการแก้สมการฟิสิกส์ ให้ Joint แข็ง/นิ่งขึ้น (กันคานเอียง/สั่นจากภาระหนักเยื้องศูนย์)
                rb.solverIterations = 24;
                rb.solverVelocityIterations = 12;

                // Concave Mesh Colliders are not supported with dynamic Rigidbodies.
                // We must ensure all MeshColliders are convex if we intend to simulate physics.
                foreach (var meshCol in GetComponentsInChildren<MeshCollider>())
                {
                    meshCol.convex = true;
                }
            }

            var stress = GetComponent<Simulation.Physics.StructuralStress>();
            if (stress == null)
            {
                stress = gameObject.AddComponent<Simulation.Physics.StructuralStress>();
            }
            if (stress != null)
            {
                // Final limit = Structure base * Material multiplier
                float compLimit = data.baseMaxCompression * (currentMaterial != null ? currentMaterial.compressionMultiplier : 1f);
                float tenLimit  = data.baseMaxTension     * (currentMaterial != null ? currentMaterial.tensionMultiplier : 1f);
                stress.InitializeStress(maxHP, compLimit, tenLimit);
            }

            // Auto-inject Gadget behaviors if this unit is a Gadget
            if (data != null && data.isGadget)
            {
                string lowerName = data.structureName.ToLower();
                if (lowerName.Contains("balloon") || lowerName.Contains("launcher") || lowerName.Contains("shooter"))
                {
                    if (GetComponent<BalloonLauncher>() == null)
                    {
                        var launcher = gameObject.AddComponent<BalloonLauncher>();
                        launcher.shootRange = 20f;
                        launcher.shootCooldown = 1.5f;
                    }
                }
                else if (lowerName.Contains("damper") || lowerName.Contains("tuned") || lowerName.Contains("tmd"))
                {
                    if (GetComponent<TunedMassDamper>() == null)
                    {
                        var damper = gameObject.AddComponent<TunedMassDamper>();
                        damper.dampingCoefficient = 0.8f;
                        damper.restoringStrength = 15f;
                    }
                }
            }

            // Auto-attach ระบบ aerodynamic ปลอม (WindDeflector) ให้บันได/ทางลาด
            // บันไดที่หันแกน Z ตรงกับลมจะลดดาเมจลม + บังของที่อยู่ปลายลม (ดู WindDeflector.cs)
            if (data != null && !data.isGadget)
            {
                string rawName = data.structureName ?? "";
                string lowerStair = rawName.ToLower();
                bool isStair = lowerStair.Contains("stair") || lowerStair.Contains("ramp")
                               || lowerStair.Contains("ladder") || rawName.Contains("บันได");
                if (isStair && GetComponent<Simulation.Physics.WindDeflector>() == null)
                {
                    gameObject.AddComponent<Simulation.Physics.WindDeflector>();
                }
            }
        }

        private void CacheRenderers()
        {
            _renderers.Clear();
            _originalColors.Clear();

            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                _renderers.Add(rend);
                _originalColors.Add(rend.material.color);
            }
        }

        public void ApplyMaterial()
        {
            if (currentMaterial == null || currentMaterial.material == null) return;

            foreach (var rend in _renderers)
            {
                if (rend == null) continue;
                rend.material = currentMaterial.material;
            }

            // Update original colors for highlight system
            _originalColors.Clear();
            foreach (var rend in _renderers)
            {
                _originalColors.Add(rend.material.color);
            }
        }

        public void ChangeMaterial(MaterialData newMaterial)
        {
            if (data != null && data.isGadget) return;
            currentMaterial = newMaterial;
            ApplyMaterial();

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = (data.baseMass * (currentMaterial != null ? currentMaterial.massMultiplier : 1f)) / 100f;
            }
        }

        public void SetRotation(float newRotation)
        {
            _rotation = newRotation;
        }

        /// <summary>
        /// Highlight this structure (e.g. when hovered in idle mode).
        /// </summary>
        public void SetHighlight(bool highlighted, Color? color = null)
        {
            if (_isHighlighted == highlighted && color == null) return;
            _isHighlighted = highlighted;

            // Outline (toon) ตอนถูกไฮไลต์ — ใช้ตอน hover เพื่อย้าย/ลบ หรือเลือกหลายชิ้น
            if (useOutline)
            {
                if (highlighted) OutlineHelper.Apply(gameObject, color ?? highlightColor, outlineWidth);
                else OutlineHelper.Disable(gameObject);
            }

            if (_renderers.Count == 0) CacheRenderers();

            var stress = GetComponent<Simulation.Physics.StructuralStress>();
            Color targetColor = color ?? highlightColor;

            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] == null) continue;

                if (highlighted)
                {
                    // ผสมสี Highlight เข้าไป (หรือใช้สี Highlight ไปเลย)
                    _renderers[i].material.color = targetColor;
                }
                else
                {
                    // เมื่อเอา Highlight ออก ให้กลับไปเป็นสีที่ควรจะเป็น
                    if (stress != null && Simulation.Physics.StructuralStress.ShowHPVisualsGlobal)
                    {
                        // ถ้าเปิดระบบ Stress อยู่ ให้ Stress เป็นคนจัดการสีใหม่
                        stress.RefreshVisual();
                    }
                    else if (i < _originalColors.Count)
                    {
                        // ถ้าไม่มี Stress ให้กลับไปเป็นสีเดิมของวัสดุ
                        _renderers[i].material.color = _originalColors[i];
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            var stress = GetComponent<Simulation.Physics.StructuralStress>();
            if (stress != null)
            {
                stress.ApplyExternalDamage(amount);
                _currentHP = stress.CurrentHP;
            }
            else
            {
                _currentHP -= amount;
                if (_currentHP <= 0)
                {
                    DestroyStructure();
                }
            }
        }

        public void TakeMaxHPDamage(float amount)
        {
            var stress = GetComponent<Simulation.Physics.StructuralStress>();
            if (stress != null)
            {
                stress.ApplyMaxHPDamage(amount);
                _currentHP = stress.CurrentHP;
            }
            else
            {
                _currentHP -= amount;
                if (_currentHP <= 0) DestroyStructure();
            }
        }

        public void DestroyStructure()
        {
            if (currentMaterial != null)
            {
                if (currentMaterial.breakSound != null) AudioSource.PlayClipAtPoint(currentMaterial.breakSound, transform.position);
                if (currentMaterial.breakVFX != null) Instantiate(currentMaterial.breakVFX, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
