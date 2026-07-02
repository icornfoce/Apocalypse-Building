using UnityEngine;
using FLOW;
using Simulation.Building;
using Simulation.NPC;

namespace Simulation.Mission
{
    /// <summary>
    /// น้ำท่วม — น้ำค่อยๆ สูงขึ้น ใส่ดาเมจโครงสร้างที่อยู่ใต้น้ำ
    /// ใช้ floodMaxHeight เป็นระดับน้ำสูงสุด, intensity เป็นความเร็วที่น้ำขึ้น
    /// VFX Prefab ควรเป็น Water Plane ที่ scale ได้
    /// </summary>
    public class FloodDisaster : DisasterBase
    {
        private const float DefaultDrowningDamagePerSecond = 25f;
        private const float DrowningHeightRatio = 0.75f;
        private const float FallbackDrowningHeight = 1.2f;

        private float _currentWaterLevel;
        private float _flowAppliedWaterLevel;
        private float _elapsedTime;
        private Transform _waterTransform;
        private FlowSimulation _flowSimulation;
        private FlowModifier _flowFillModifier;
        private GameObject _runtimeFlowRoot;
        private Material _runtimeSurfaceMaterial;

        public FloodDisaster(DisasterData data, MonoBehaviour runner) : base(data, runner) { }

        protected override void OnStart()
        {
            _currentWaterLevel = 0f;
            _flowAppliedWaterLevel = 0f;
            _elapsedTime = 0f;

            // หา Water VFX ที่ spawn มาแล้ว (จาก base class)
            if (spawnedVFX.Count > 0 && spawnedVFX[0] != null)
            {
                _waterTransform = spawnedVFX[0].transform;
                _waterTransform.position = new Vector3(data.centerOffset.x, 0f, data.centerOffset.z);
                _flowSimulation = spawnedVFX[0].GetComponentInChildren<FlowSimulation>();
            }

            if (_flowSimulation == null)
            {
                CreateRuntimeFlowWater();
            }
            else
            {
                CreateFlowFillModifier(_flowSimulation.transform);
            }
        }

        protected override void OnUpdate(float dt)
        {
            _elapsedTime += dt;

            float duration = Mathf.Max(0.01f, data.duration);
            float maxWaterLevel = Mathf.Max(0f, data.floodMaxHeight);
            float targetWaterLevel = Mathf.SmoothStep(0f, maxWaterLevel, Mathf.Clamp01(_elapsedTime / duration));

            _currentWaterLevel = targetWaterLevel;

            UpdateFlowWater(_currentWaterLevel);

            // ขยับ Water VFX ขึ้น
            if (_waterTransform != null)
            {
                Vector3 pos = _waterTransform.position;
                pos.y = _currentWaterLevel;
                _waterTransform.position = pos;
            }

            // ใส่ดาเมจโครงสร้างที่อยู่ใต้ระดับน้ำ
            var structures = GetStructuresInRadius(data.centerOffset, data.radius);
            foreach (var unit in structures)
            {
                if (unit == null) continue;
                if (unit.transform.position.y < _currentWaterLevel)
                {
                    DamageStructure(unit, data.damagePerSecond * dt);

                    // แรงดันน้ำผลักโครงสร้าง
                    Rigidbody rb = unit.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        float submergedDepth = _currentWaterLevel - unit.transform.position.y;
                        float buoyancy = Mathf.Clamp01(submergedDepth / Mathf.Max(0.01f, data.floodMaxHeight));
                        rb.AddForce(Vector3.up * buoyancy * data.intensity, ForceMode.Force);
                    }
                }
            }

            // ใส่ดาเมจคนที่จมน้ำ
            float drowningDamage = GetDrowningDamagePerSecond() * dt;

            var people = GetAllPeople();
            foreach (var person in people)
            {
                if (person != null && IsDrowning(person.transform))
                {
                    DamagePerson(person, drowningDamage);
                }
            }

            var npcs = GetAllNPCs();
            foreach (var npc in npcs)
            {
                if (npc != null && IsDrowning(npc.transform))
                {
                    DamageNPC(npc, drowningDamage);
                }
            }

            var zombies = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            foreach (var zombie in zombies)
            {
                if (zombie != null && IsDrowning(zombie.transform))
                {
                    zombie.TakeDamage(drowningDamage);
                }
            }
        }

        protected override void OnStop()
        {
            if (_runtimeFlowRoot != null)
            {
                Object.Destroy(_runtimeFlowRoot);
                _runtimeFlowRoot = null;
            }

            if (_runtimeSurfaceMaterial != null)
            {
                Object.Destroy(_runtimeSurfaceMaterial);
                _runtimeSurfaceMaterial = null;
            }

            _flowSimulation = null;
            _flowFillModifier = null;
            _waterTransform = null;
        }

        private void CreateRuntimeFlowWater()
        {
            float diameter = Mathf.Max(1f, data.radius > 0f ? data.radius * 2f : 50f);
            float maxHeight = Mathf.Max(0.01f, data.floodMaxHeight);
            Vector3 center = data.centerOffset;

            _runtimeFlowRoot = new GameObject($"FLOW_Flood_{data.disasterName}");
            _runtimeFlowRoot.transform.position = new Vector3(center.x, 0f, center.z);

            _flowSimulation = _runtimeFlowRoot.AddComponent<FlowSimulation>();
            _flowSimulation.Center = true;
            _flowSimulation.Size = new Vector3(diameter, 0f, diameter);
            _flowSimulation.Separation = Mathf.Max(0.5f, diameter / 64f);
            _flowSimulation.HeightMin = -1f;
            _flowSimulation.HeightMax = maxHeight + 2f;
            _flowSimulation.PhysicsModel = FlowSimulation.PhysicsType.Alive;
            _flowSimulation.PhysicsInstability = 0.04f;
            _flowSimulation.PhysicsDamping = 0.12f;
            _flowSimulation.PhysicsSpeed = 0.35f;

            var surface = FlowSurface.Create(_runtimeFlowRoot.layer, _runtimeFlowRoot.transform);
            surface.Simulation = _flowSimulation;
            surface.Edge = true;
            _runtimeSurfaceMaterial = CreateFlowSurfaceMaterial();
            surface.CachedMeshRenderer.sharedMaterial = _runtimeSurfaceMaterial;

            CreateFlowFillModifier(_runtimeFlowRoot.transform);
        }

        private void CreateFlowFillModifier(Transform parent)
        {
            float diameter = Mathf.Max(1f, data.radius > 0f ? data.radius * 2f : 50f);

            var fluidObject = new GameObject("Flood Water Fluid");
            fluidObject.transform.SetParent(parent, false);
            var fluid = fluidObject.AddComponent<FlowFluid>();
            fluid.Color = new Color(0.2f, 0.55f, 0.75f, 0.75f);
            fluid.Smoothness = 0.7f;
            fluid.Viscosity = 0.08f;

            _flowFillModifier = FlowModifier.Create(parent.gameObject.layer, parent);
            _flowFillModifier.name = "Flood Fill";
            _flowFillModifier.Mode = FlowModifier.ModeType.AddFluidBelow;
            _flowFillModifier.Apply = FlowModifier.ApplyType.Manually;
            _flowFillModifier.Center = true;
            _flowFillModifier.Size = new Vector3(diameter, 0f, diameter);
            _flowFillModifier.Fluid = fluid;
        }

        private void UpdateFlowWater(float targetWaterLevel)
        {
            if (_flowSimulation == null || _flowFillModifier == null || _flowSimulation.Activated == false)
            {
                return;
            }

            float waterDelta = Mathf.Max(0f, targetWaterLevel - _flowAppliedWaterLevel);

            if (waterDelta <= 0f)
            {
                return;
            }

            _flowFillModifier.transform.position = new Vector3(data.centerOffset.x, _currentWaterLevel, data.centerOffset.z);
            _flowFillModifier.Strength = waterDelta;
            _flowFillModifier.ApplyNow();
            _flowAppliedWaterLevel = targetWaterLevel;
        }

        private Material CreateFlowSurfaceMaterial()
        {
            var shader = Shader.Find("FLOW/Surface_Transparent");

            if (shader != null)
            {
                return new Material(shader);
            }

            shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.color = new Color(0.2f, 0.55f, 0.75f, 0.55f);

            return material;
        }

        private float GetDrowningDamagePerSecond()
        {
            return data.peopleDamagePerSecond > 0f ? data.peopleDamagePerSecond : DefaultDrowningDamagePerSecond;
        }

        private bool IsDrowning(Transform target)
        {
            if (target == null || IsOutsideFloodRadius(target.position))
            {
                return false;
            }

            return _currentWaterLevel >= GetDrowningHeight(target);
        }

        private bool IsOutsideFloodRadius(Vector3 position)
        {
            if (data.radius <= 0f)
            {
                return false;
            }

            Vector2 center = new Vector2(data.centerOffset.x, data.centerOffset.z);
            Vector2 target = new Vector2(position.x, position.z);

            return Vector2.Distance(center, target) > data.radius;
        }

        private float GetDrowningHeight(Transform target)
        {
            var colliders = target.GetComponentsInChildren<Collider>();
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (var collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            if (hasBounds)
            {
                return Mathf.Lerp(bounds.min.y, bounds.max.y, DrowningHeightRatio);
            }

            return target.position.y + FallbackDrowningHeight;
        }
    }
}
