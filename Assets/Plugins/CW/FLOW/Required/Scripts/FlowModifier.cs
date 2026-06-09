using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;
using Unity.Mathematics;

namespace FLOW
{
	/// <summary>This component allows you to modify a small area of a fluid. For example, to add fluid, remove fluid, etc.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowModifier")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Modifier")]
	public class FlowModifier : MonoBehaviour, ISampleHandler
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		[System.Serializable] public class SimulationFloatEvent : UnityEvent<FlowSimulation, float> {}

		public enum ApplyType
		{
			Manually,
			Once,
			Continuously
		}

		public enum ModeType
		{
			AddFluid             = 10,
			AddFluidClip         = 11,
			AddFluidClipInv      = 12,
			AddFluidBelow        = 13,
			RemoveFluid          = 20,
			RemoveFluidClip      = 21,
			RemoveFluidAbove     = 22,
			RemoveFluidAboveClip = 23,
			AddForce             = 30,
			AddForceUniform      = 31,
			DampenForce          = 40,
			AddFoam              = 50,
			AddFoamMax           = 51,
			RemoveFoam           = 60,
			ChangeColor          = 70,
			ChangeESMV           = 71,

			RangeAddForce        = 130,
			RangeAddForceUniform = 131,
			RangeDampenForce     = 140,
			RangeAddFoam         = 150,
			RangeAddFoamMax      = 151,
			RangeRemoveFoam      = 160,
			RangeChangeColor     = 170,
			RangeChangeESMV      = 171,
		}

		/// <summary>This allows you to set the size of the modifier boundary in local space.
		/// NOTE: The Y value is ignored.</summary>
		public Vector3 Size { set { size = value; } get { return size; } } [SerializeField] private Vector3 size = new Vector3(1.0f, 0.0f, 1.0f);

		/// <summary>The strength of this modifier will be multiplied by a channel of this texture specified by the <b>ShapeChannel</b> setting.</summary>
		public Texture Shape { set { shape = value; } get { return shape; } } [SerializeField] private Texture shape;

		/// <summary>This allows you to choose which channel from the <b>Shape</b> texture will be used.</summary>
		public FlowChannel ShapeChannel { set { shapeChannel = value; } get { return shapeChannel; } } [SerializeField] private FlowChannel shapeChannel = FlowChannel.Alpha;

		/// <summary>Should the modifier's boundary be centered, or start from the corner</summary>
		public bool Center { set { center = value; } get { return center; } } [SerializeField] private bool center = true;

		/// <summary>How often should this component modify the underlying fluid?
		/// Manually = You must manually call the <b>ApplyNow</b> method from code or inspector event.
		/// Once = The modifier will apply on the first frame after it gets activated.
		/// Continuously = This modifier will apply every time the underlying fluid updates.</summary>
		public ApplyType Apply { set { apply = value; } get { return apply; } } [SerializeField] private ApplyType apply = ApplyType.Continuously;

		/// <summary>This allows you to choose how this component will modify the underlying fluid simulation.
		/// AddFluid = Fluid will be added under and above this modifier's local XZ position.
		/// AddFluidClip = Like <b>AddFluid</b>, but areas of the modifier that are underground will be ignored.
		/// AddFluidClipInv = Like <b>AddFluid</b>, but areas of the modifier that are overground will be ignored.
		/// RemoveFluid = Fluid will be removed under and above this modifier's local XZ position.
		/// RemoveFluidClip = Like <b>RemoveFluid</b>, but areas of the modifier that are underground will be ignored.
		/// RemoveFluidAbove = Like <b>RemoveFluid</b>, but the removal will stop once the fluid level reaches that of the modifier's Y position.
		/// RemoveFluidAboveClip = Like <b>RemoveFluidAbove</b>, but areas of the modifier that are underground will be ignored.
		/// AddForce = Fluid within the boundary of this modifier will be given force based on the specified normal map.
		/// AddForceUniform = Fluid within the boundary of this modifier will be given forward (local +Z) force.
		/// DampenForce = Fluid within the boundary of this modifier will have force removed from it. A <b>Strength</b> value of 1 will result in all force being removed.
		/// AddFoam = Fluid within the boundary of this modifier will have foam added to it.
		/// AddFoamMax = Like <b>AddFoam</b>, but the foam will only increase if the added amount is greater than the current amount.
		/// RemoveFoam = Fluid within the boundary of this modifier will have foam removed from it.
		/// ChangeColor = Fluid within the boundary of this modifier will have its color transition toward the specified color.
		/// ChangeESMV = Fluid within the boundary of this modifier will have its Emission/Smoothness/Metallic/Viscosity transition toward the specified values.</summary>
		public ModeType Mode { set { mode = value; } get { return mode; } } [SerializeField] private ModeType mode;

		/// <summary>This modifier will modify fluids above this height in local space.</summary>
		public float HeightMin { set { heightMin = value; } get { return heightMin; } } [SerializeField] private float heightMin;

		/// <summary>This modifier will modify fluids below this height in local space.</summary>
		public float HeightMax { set { heightMax = value; } get { return heightMax; } } [SerializeField] private float heightMax = 1.0f;

		/// <summary>When using one of the <b>AddFluid</b> modes, this allows you to specify the fluid properties that get added.</summary>
		public FlowFluid Fluid { set { fluid = value; } get { return fluid; } } [SerializeField] private FlowFluid fluid;

		/// <summary>When using <b>Mode = AddForce/Uniform</b>, this allows you to specify the direction of the force relative to the forward direction of the modifier.
		/// 0 = Forward.
		/// 90 = Right.
		/// 180 = Back.
		/// 270 = Left.</summary>
		public float Angle { set { angle = value; } get { return angle; } } [SerializeField] private float angle;

		/// <summary>When using <b>Mode = AddForce</b>, this allows you to specify the fluid properties that get added.</summary>
		public Texture Directions { set { directions = value; } get { return directions; } } [SerializeField] private Texture directions;

		/// <summary>When using <b>Mode = ChangeColor</b>, this allows you to specify the target color.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>When using <b>Mode = ChangeESMV</b>, this allows you to specify the target emission/smoothness/metallic/visosity.</summary>
		public Vector4 ESMV { set { esmv = value; } get { return esmv; } } [SerializeField] private Vector4 esmv;

		/// <summary>When using <b>Mode = ChangeColor</b>, this allows you to specify which channels in the target color will be used.</summary>
		public bool4 Channels { set { channels = value; } get { return channels; } } [SerializeField] [UnityEngine.Serialization.FormerlySerializedAs("colorChannels")] private bool4 channels = new bool4(true, true, true, true);

		/// <summary>The region modification strength will be multiplied by this amount.</summary>
		public float Strength { set { strength = value; } get { return strength; } } [SerializeField] private float strength = 1.0f;

		/// <summary>After this component applies its changes to the scene, it will invoke this event.
		/// Float = The strength of the modifier application.</summary>
		public FloatEvent OnApplied { get { if (onApplied == null) onApplied = new FloatEvent(); return onApplied; } } [SerializeField] private FloatEvent onApplied;

		/// <summary>If this modifier's mode is set to <b>AddFluid</b> or <b>RemoveFluid</b>, do you want to track how much this modifier adds or removes?</summary>
		public bool MonitorFluidDepth { set { monitorFluidDepth = value; } get { return monitorFluidDepth; } } [SerializeField] private bool monitorFluidDepth;

		/// <summary>If this modifier's mode is set to <b>AddFluid</b> or <b>RemoveFluid</b>, this event tells you how much total fluid column depth was added or removed.
		/// Float = Amount of fluid that was added (positive) or removed (negative).
		/// NOTE: The <b>MonitorFluidDepth</b> setting must be enabled for this event to be invoked.</summary>
		public SimulationFloatEvent OnFluidDepthDelta { get { if (onFluidDepthDelta == null) onFluidDepthDelta = new SimulationFloatEvent(); return onFluidDepthDelta; } } [SerializeField] private SimulationFloatEvent onFluidDepthDelta;

		/// <summary>This works like <b>OnFluidDepthDelta</b>, but the delta value will be the fluid volume in meters cubed.</summary>
		public SimulationFloatEvent OnFluidVolumeDelta { get { if (onFluidVolumeDelta == null) onFluidVolumeDelta = new SimulationFloatEvent(); return onFluidVolumeDelta; } } [SerializeField] private SimulationFloatEvent onFluidVolumeDelta;

		/// <summary>This stores all activate and enabled <b>FlowModifier</b> instances in the scene.</summary>
		public static LinkedList<FlowModifier> Instances { get { return instances; } } private static LinkedList<FlowModifier> instances = new LinkedList<FlowModifier>(); private LinkedListNode<FlowModifier> instanceNode;

		private bool primed = true;
		
		private static Material cachedMaterial_Copy;
		private static Material cachedMaterial_AddFluid;
		private static Material cachedMaterial_RemoveFluid;
		private static Material cachedMaterial_AddForce;
		private static Material cachedMaterial_DampenForce;
		private static Material cachedMaterial_AddFoam;
		private static Material cachedMaterial_RemoveFoam;
		private static Material cachedMaterial_ChangeColor;
		private static Material cachedMaterial_ChangeESMV;

		private static readonly int _ModifierMatrix   = Shader.PropertyToID("_ModifierMatrix");
		private static readonly int _ModifierInverse  = Shader.PropertyToID("_ModifierInverse");
		private static readonly int _ModifierStrength = Shader.PropertyToID("_ModifierStrength");
		private static readonly int _ModifierChannel  = Shader.PropertyToID("_ModifierChannel");
		private static readonly int _ModifierShape    = Shader.PropertyToID("_ModifierShape");
		private static readonly int _ModifierNormal   = Shader.PropertyToID("_ModifierNormal");
		private static readonly int _ModifierRGBA     = Shader.PropertyToID("_ModifierRGBA");
		private static readonly int _ModifierESMV     = Shader.PropertyToID("_ModifierESMV");
		private static readonly int _ModifierF123     = Shader.PropertyToID("_ModifierF123");
		private static readonly int _ModifierAngle    = Shader.PropertyToID("_ModifierAngle");
		private static readonly int _ModifierChannels = Shader.PropertyToID("_ModifierChannels");
		private static readonly int _ModifierBuffer   = Shader.PropertyToID("_ModifierBuffer");

		public bool AddsFluid
		{
			get
			{
				return mode == ModeType.AddFluid || mode == ModeType.AddFluidClip || mode == ModeType.AddFluidClipInv || mode == ModeType.AddFluidBelow;
			}
		}

		public bool RemovesFluid
		{
			get
			{
				return mode == ModeType.RemoveFluid || mode == ModeType.RemoveFluidClip || mode == ModeType.RemoveFluidAbove || mode == ModeType.RemoveFluidAboveClip;
			}
		}

		public bool HasRange
		{
			get
			{
				switch (mode)
				{
					case ModeType.RangeAddForce: return true;
					case ModeType.RangeAddForceUniform: return true;
					case ModeType.RangeDampenForce: return true;
					case ModeType.RangeAddFoam: return true;
					case ModeType.RangeAddFoamMax: return true;
					case ModeType.RangeRemoveFoam: return true;
					case ModeType.RangeChangeColor: return true;
					case ModeType.RangeChangeESMV: return true;
				}

				return false;
			}
		}

		protected virtual void OnEnable()
		{
			instanceNode = instances.AddLast(this);

			primed = true;
		}

		protected virtual void OnDisable()
		{
			instances.Remove(instanceNode); instanceNode = null;
		}

		protected virtual void Update()
		{
			if (apply == ApplyType.Once && primed == true)
			{
				primed = false;

				ApplyNow();
			}
		}

		/// <summary>This method will apply this region to all volumes in the scene using the specified <b>Strength</b> value.</summary>
		[ContextMenu("Apply Now")]
		public void ApplyNow()
		{
			ApplyNow(1.0f);
		}

		private bool CheckMonitorFluidDepth()
		{
			if (monitorFluidDepth == true)
			{
				if (mode == ModeType.AddFluid || mode == ModeType.AddFluidBelow || mode == ModeType.AddFluidClip || mode == ModeType.AddFluidClipInv)
				{
					return true;
				}

				if (mode == ModeType.RemoveFluid || mode == ModeType.RemoveFluidAbove || mode == ModeType.RemoveFluidAboveClip || mode == ModeType.RemoveFluidClip)
				{
					return true;
				}
			}

			return false;
		}

		public void ApplyNow(float multiplier)
		{
			if (multiplier > 0.0f && strength != 0)
			{
				var localBounds     = CalculateLocalBounds();
				var worldBounds     = FlowCommon.CalculateWorldBounds(transform, localBounds);
				var heightMid       = (heightMin + heightMax) * 0.5f;
				var heightDiff      = (heightMax - heightMin) * 0.5f;
				var modifierMatrix  = transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(localBounds.min.x, 0.0f, localBounds.min.z)) * Matrix4x4.Scale(localBounds.size);
				var modifierInverse = Matrix4x4.Scale(new Vector3(1.0f, heightDiff != 0.0f ? 1.0f / heightDiff : 0.0f, 1.0f)) * Matrix4x4.Translate(new Vector3(0.0f, -heightMid, 0.0f)) * transform.worldToLocalMatrix;
				var monitor         = CheckMonitorFluidDepth();

				foreach (var simulation in FlowSimulation.Instances)
				{
					if (simulation.Activated == true)
					{
						var pixels = simulation.CalculatePixelRect(worldBounds);

						if (pixels.width * pixels.height > 0)
						{
							var reader = default(FlowReader);

							if (monitor == true)
							{
								reader = FlowReader.SampleFluidAreaDepth(simulation, pixels, this, reader);
							}

							switch (mode)
							{
								case ModeType.AddFluid:
									Check(ref cachedMaterial_AddFluid, "FLOW/Modifier_AddFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, simulation.CurrentFlowDataD, simulation.CurrentFlowDataE, simulation.CurrentFlowDataF, cachedMaterial_AddFluid, 0);
									break;

								case ModeType.AddFluidClip:
									Check(ref cachedMaterial_AddFluid, "FLOW/Modifier_AddFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, simulation.CurrentFlowDataD, simulation.CurrentFlowDataE, simulation.CurrentFlowDataF, cachedMaterial_AddFluid, 1);
									break;

								case ModeType.AddFluidClipInv:
									Check(ref cachedMaterial_AddFluid, "FLOW/Modifier_AddFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, simulation.CurrentFlowDataD, simulation.CurrentFlowDataE, simulation.CurrentFlowDataF, cachedMaterial_AddFluid, 2);
									break;

								case ModeType.AddFluidBelow:
									Check(ref cachedMaterial_AddFluid, "FLOW/Modifier_AddFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, simulation.CurrentFlowDataD, simulation.CurrentFlowDataE, simulation.CurrentFlowDataF, cachedMaterial_AddFluid, 3);
									break;

								case ModeType.RemoveFluid:
									Check(ref cachedMaterial_RemoveFluid, "FLOW/Modifier_RemoveFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, cachedMaterial_RemoveFluid, 0);
									break;

								case ModeType.RemoveFluidClip:
									Check(ref cachedMaterial_RemoveFluid, "FLOW/Modifier_RemoveFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, cachedMaterial_RemoveFluid, 1);
									break;

								case ModeType.RemoveFluidAbove:
									Check(ref cachedMaterial_RemoveFluid, "FLOW/Modifier_RemoveFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, cachedMaterial_RemoveFluid, 2);
									break;

								case ModeType.RemoveFluidAboveClip:
									Check(ref cachedMaterial_RemoveFluid, "FLOW/Modifier_RemoveFluid");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataC, cachedMaterial_RemoveFluid, 3);
									break;

								case ModeType.AddForce:
									Check(ref cachedMaterial_AddForce, "FLOW/Modifier_AddForce");
									if (cachedMaterial_AddForce == null) cachedMaterial_AddForce = CwHelper.CreateTempMaterial("Modifier_AddFluid (AddForce)", Resources.Load<Shader>("FLOW/Modifier_AddForce"));
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_AddForce, 0);
									break;

								case ModeType.AddForceUniform:
									Check(ref cachedMaterial_AddForce, "FLOW/Modifier_AddForce");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_AddForce, 1);
									break;

								case ModeType.DampenForce:
									Check(ref cachedMaterial_DampenForce, "FLOW/Modifier_DampenForce");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_DampenForce, 0);
									break;

								case ModeType.AddFoam:
									Check(ref cachedMaterial_AddFoam, "FLOW/Modifier_AddFoam");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_AddFoam, 0);
									break;

								case ModeType.AddFoamMax:
									Check(ref cachedMaterial_AddFoam, "FLOW/Modifier_AddFoam");
									ApplyNow(1.0f, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_AddFoam, 1); // NOTE: Override multiplier to 1 because it's not being applied continuously
									break;

								case ModeType.RemoveFoam:
									Check(ref cachedMaterial_RemoveFoam, "FLOW/Modifier_RemoveFoam");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_RemoveFoam, 0);
									break;

								case ModeType.ChangeColor:
									Check(ref cachedMaterial_ChangeColor, "FLOW/Modifier_ChangeColor");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataD, cachedMaterial_ChangeColor, 0);
									break;

								case ModeType.ChangeESMV:
									Check(ref cachedMaterial_ChangeESMV, "FLOW/Modifier_ChangeESMV");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataE, cachedMaterial_ChangeESMV, 0);
									break;

								case ModeType.RangeAddForce:
									Check(ref cachedMaterial_AddForce, "FLOW/Modifier_AddForce");
									if (cachedMaterial_AddForce == null) cachedMaterial_AddForce = CwHelper.CreateTempMaterial("Modifier_AddFluid (AddForce)", Resources.Load<Shader>("FLOW/Modifier_AddForce"));
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_AddForce, 2);
									break;

								case ModeType.RangeAddForceUniform:
									Check(ref cachedMaterial_AddForce, "FLOW/Modifier_AddForce");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_AddForce, 3);
									break;

								case ModeType.RangeDampenForce:
									Check(ref cachedMaterial_DampenForce, "FLOW/Modifier_DampenForce");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataB, cachedMaterial_DampenForce, 1);
									break;

								case ModeType.RangeAddFoam:
									Check(ref cachedMaterial_AddFoam, "FLOW/Modifier_AddFoam");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_AddFoam, 2);
									break;

								case ModeType.RangeAddFoamMax:
									Check(ref cachedMaterial_AddFoam, "FLOW/Modifier_AddFoam");
									ApplyNow(1.0f, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_AddFoam, 3); // NOTE: Override multiplier to 1 because it's not being applied continuously
									break;

								case ModeType.RangeRemoveFoam:
									Check(ref cachedMaterial_RemoveFoam, "FLOW/Modifier_RemoveFoam");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataF, cachedMaterial_RemoveFoam, 1);
									break;

								case ModeType.RangeChangeColor:
									Check(ref cachedMaterial_ChangeColor, "FLOW/Modifier_ChangeColor");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataD, cachedMaterial_ChangeColor, 1);
									break;

								case ModeType.RangeChangeESMV:
									Check(ref cachedMaterial_ChangeESMV, "FLOW/Modifier_ChangeESMV");
									ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, simulation.CurrentFlowDataE, cachedMaterial_ChangeESMV, 1);
									break;
							}

							if (monitor == true)
							{
								FlowReader.SampleFluidAreaDepth(simulation, pixels, this, reader);
							}
						}
					}
				}

				if (onApplied != null)
				{
					onApplied.Invoke(multiplier);
				}
			}
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowModifier</b> component attached.</summary>
		public static FlowModifier Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowModifier</b> component attached.</summary>
		public static FlowModifier Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Modifier", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowModifier>();
		}

		public void HandleSamples(FlowSimulation simulation, List<Color> samples)
		{
			var half  = samples.Count / 2;
			var delta = 0.0;

			for (var i = 0; i < half; i++)
			{
				delta -= samples[i].b;
			}

			for (var i = half; i < samples.Count; i++)
			{
				delta += samples[i].b;
			}

			if (onFluidDepthDelta != null)
			{
				onFluidDepthDelta.Invoke(simulation, (float)delta);
			}

			if (onFluidVolumeDelta != null)
			{
				onFluidVolumeDelta.Invoke(simulation, (float)delta * simulation.VolumePerColumn);
			}
		}

		private void Check(ref Material cachedMaterial, string materialPath)
		{
			if (cachedMaterial == null)
			{
				cachedMaterial = CwHelper.CreateTempMaterial(materialPath, Resources.Load<Shader>(materialPath));
			}
		}

		private void ApplyNow(float multiplier, Matrix4x4 modifierMatrix, Matrix4x4 modifierInverse, FlowSimulation simulation, RenderTexture textureA, Material cachedMaterial, int pass)
		{
			FlowBuffer.Size1.Set(0, textureA);

			ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, FlowBuffer.Size1, cachedMaterial, pass);
		}

		private void ApplyNow(float multiplier, Matrix4x4 modifierMatrix, Matrix4x4 modifierInverse, FlowSimulation simulation, RenderTexture textureA, RenderTexture textureB, RenderTexture textureC, RenderTexture textureD, Material cachedMaterial, int pass)
		{
			FlowBuffer.Size4.Set(0, textureA);
			FlowBuffer.Size4.Set(1, textureB);
			FlowBuffer.Size4.Set(2, textureC);
			FlowBuffer.Size4.Set(3, textureD);

			ApplyNow(multiplier, modifierMatrix, modifierInverse, simulation, FlowBuffer.Size4, cachedMaterial, pass);
		}

		private void ApplyNow(float multiplier, Matrix4x4 modifierMatrix, Matrix4x4 modifierInverse, FlowSimulation simulation, FlowBuffer buffer, Material cachedMaterial, int pass)
		{
			cachedMaterial.SetMatrix(_ModifierMatrix, modifierMatrix);
			cachedMaterial.SetMatrix(_ModifierInverse, modifierInverse);
			cachedMaterial.SetFloat(_ModifierStrength, strength * multiplier);
			cachedMaterial.SetVector(_ModifierChannel, FlowCommon.ChannelToVector(shapeChannel));
			cachedMaterial.SetTexture(_ModifierShape, shape != null ? shape : Texture2D.whiteTexture);

			if (mode == ModeType.AddForce || mode == ModeType.RangeAddForce)
			{
				cachedMaterial.SetTexture(_ModifierNormal, directions);
			}

			if (mode == ModeType.AddFluid || mode == ModeType.AddFluidClip || mode == ModeType.AddFluidClipInv || mode == ModeType.AddFluidBelow)
			{
				if (fluid != null)
				{
					var foam = 0.0f;

					cachedMaterial.SetVector(_ModifierRGBA, CwHelper.ToLinear(fluid.Color));
					cachedMaterial.SetVector(_ModifierESMV, new Vector4(fluid.Emission, fluid.Smoothness, fluid.Metallic, fluid.Viscosity));
					cachedMaterial.SetVector(_ModifierF123, new Vector4(foam, fluid.Custom1, fluid.Custom2, fluid.Custom3));
				}
			}

			if (mode == ModeType.AddForceUniform || mode == ModeType.RangeAddForceUniform)
			{
				cachedMaterial.SetFloat(_ModifierAngle, (-Vector3.SignedAngle(transform.forward, Vector3.forward, Vector3.up) + angle) * Mathf.Deg2Rad);
			}

			if (mode == ModeType.ChangeColor || mode == ModeType.RangeChangeColor)
			{
				cachedMaterial.SetVector(_ModifierRGBA, CwHelper.ToLinear(color));
				cachedMaterial.SetVector(_ModifierChannels, (float4)channels);
			}

			if (mode == ModeType.ChangeESMV || mode == ModeType.RangeChangeESMV)
			{
				cachedMaterial.SetVector(_ModifierESMV, esmv);
				cachedMaterial.SetVector(_ModifierChannels, (float4)channels);
			}

			simulation.SetVariables(cachedMaterial);

			buffer.SetRenderTargets();

			Graphics.Blit(null, cachedMaterial, pass);

			for (var i = 0; i < buffer.Count; i++)
			{
				CopyBack(simulation, modifierMatrix, buffer.SourceTextures[i], buffer.TempTextures[i]);
			}

			buffer.ReleaseAll();
		}

		private void CopyBack(FlowSimulation simulation, Matrix4x4 modifierMatrix, RenderTexture target, RenderTexture tempBuffer)
		{
			if (cachedMaterial_Copy == null) cachedMaterial_Copy = new Material(Resources.Load<Shader>("FLOW/Modifier_Copy"));

			cachedMaterial_Copy.SetMatrix(_ModifierMatrix, modifierMatrix);
			cachedMaterial_Copy.SetTexture(_ModifierBuffer, tempBuffer);

			simulation.SetVariables(cachedMaterial_Copy);

			Graphics.Blit(null, target, cachedMaterial_Copy, 0);
		}

		public Bounds CalculateLocalBounds(bool useRange = true)
		{
			var boundsCenter = Vector3.zero;
			var boundsSize   = new Vector3(size.x, 0.0f, size.z);

			if (center == false)
			{
				boundsCenter.x = boundsSize.x * 0.5f;
				boundsCenter.z = boundsSize.z * 0.5f;
			}

			if (useRange == true && HasRange == true)
			{
				boundsCenter.y = (heightMin + heightMax) * 0.5f;
				boundsSize.y = heightMax - heightMin;
			}

			return new Bounds(boundsCenter, boundsSize);
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (HasRange == true)
			{
				DrawGizmosHeight(0.0f, heightMin);
				DrawGizmosHeight(0.0f, heightMax);

				DrawGizmosEdges(heightMin, heightMax);
			}
			else
			{
				DrawGizmosHeight(0.0f, 0.0f);

				if (mode == ModeType.AddFluidBelow)
				{
					DrawGizmosEdges(0.0f, -1000.0f);
				}
				else if (mode == ModeType.RemoveFluidAbove || mode == ModeType.RemoveFluidAboveClip)
				{
					DrawGizmosEdges(0.0f, 1000.0f);
				}
				else
				{
					DrawGizmosEdges(-1000.0f, 1000.0f);
				}
			}

			if (shape != null)
			{
				var localBounds = CalculateLocalBounds();
				var matrix      = transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(localBounds.min.x, 0.0f, localBounds.min.z)) * Matrix4x4.Scale(new Vector3(localBounds.size.x, 1.0f, localBounds.size.z));

				matrix *= Matrix4x4.Rotate(Quaternion.Euler(90.0f, 0.0f, 0.0f));
				matrix *= Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.0f));

				if (HasRange == true)
				{
					for (var i = 0; i < 16; i++)
					{
						var subMatrix = Matrix4x4.Translate(new Vector3(0.0f, Mathf.Lerp(heightMin, heightMax, i / 15.0f), 0.0f)) * matrix;

						FlowCommon.DrawShapeOutline(shape, shapeChannel, subMatrix);
					}
				}
				else
				{
					FlowCommon.DrawShapeOutline(shape, shapeChannel, matrix);
				}
			}
		}

		private void DrawGizmosEdges(float verticalA, float verticalB)
		{
			var bounds  = CalculateLocalBounds(false);
			var corner  = transform.TransformPoint(bounds.min);
			var right   = transform.TransformPoint(bounds.min + Vector3.right   * bounds.size.x) - corner;
			var forward = transform.TransformPoint(bounds.min + Vector3.forward * bounds.size.z) - corner;
			var cornerA = corner + Vector3.up * verticalA;
			var cornerB = corner + Vector3.up * verticalB;

			Gizmos.DrawLine(cornerA, cornerB);
			Gizmos.DrawLine(cornerA + right, cornerB + right);
			Gizmos.DrawLine(cornerA + forward, cornerB + forward);
			Gizmos.DrawLine(cornerA + right + forward, cornerB + right + forward);
		}

		private void DrawGizmosHeight(float normal, float vertical)
		{
			var bounds  = CalculateLocalBounds(false);
			var corner  = transform.TransformPoint(bounds.min);
			var right   = transform.TransformPoint(bounds.min + Vector3.right   * bounds.size.x) - corner;
			var forward = transform.TransformPoint(bounds.min + Vector3.forward * bounds.size.z) - corner;

			corner += transform.up * normal;
			corner += Vector3.up * vertical;

			Gizmos.DrawLine(corner, corner + right);
			Gizmos.DrawLine(corner, corner + forward);
			Gizmos.DrawLine(corner + right + forward, corner + right);
			Gizmos.DrawLine(corner + right + forward, corner + forward);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowModifier;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowModifier_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => System.Enum.IsDefined(typeof(TARGET.ModeType), t.Mode) == false));
				Draw("mode", "This allows you to choose how this component will modify the underlying fluid simulation.\n\nAddFluid = Fluid will be added under and above this modifier's local XZ position.\n\nAddFluidClip = Like <b>AddFluid</b>, but areas of the modifier that are underground will be ignored.\n\nAddFluidClipInv = Like <b>AddFluid</b>, but areas of the modifier that are overground will be ignored.\n\nRemoveFluid = Fluid will be removed under and above this modifier's local XZ position.\n\nRemoveFluidClip = Like <b>RemoveFluid</b>, but areas of the modifier that are underground will be ignored.\n\nRemoveFluidAbove = Like <b>RemoveFluid</b>, but the removal will stop once the fluid level reaches that of the modifier's Y position.\n\nRemoveFluidAboveClip = Like <b>RemoveFluidAbove</b>, but areas of the modifier that are underground will be ignored.\n\nAddForce = Fluid within the boundary of this modifier will be given force based on the specified normal map.\n\nAddForceUniform = Fluid within the boundary of this modifier will be given forward (local +Z) force.\n\nDampenForce = Fluid within the boundary of this modifier will have force removed from it. A <b>Strength</b> value of 1 will result in all force being removed.\n\nAddFoam = Fluid within the boundary of this modifier will have foam added to it.\n\nAddFoamMax = Like <b>AddFoam</b>, but the foam will only increase if the added amount is greater than the current amount.\n\nRemoveFoam = Fluid within the boundary of this modifier will have foam removed from it.\n\nChangeColor = Fluid within the boundary of this modifier will have its color transition toward the specified color.");
			EndError();

			Separator();

			BeginError(Any(tgts, t => t.Size.x <= 0.0f || t.Size.z <= 0.0f));
				Draw("size", "This allows you to set the size of the modifier boundary in local space.\n\nNOTE: The Y value is ignored.");
			EndError();
			EditorGUILayout.BeginHorizontal();
				Draw("shape", "The strength of this modifier will be multiplied by a channel of this texture specified by the <b>ShapeChannel</b> setting.");
				EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeChannel"), GUIContent.none, GUILayout.Width(60));
			EditorGUILayout.EndHorizontal();
			Draw("center", "Should the modifier's boundary be centered, or start from the corner?");

			if (Any(tgts, t => t.HasRange == true))
			{
				Separator();

				BeginError(Any(tgts, t => t.HeightMax <= t.HeightMin));
					Draw("heightMin", "This modifier will modify fluids above this height in local space.");
					Draw("heightMax", "This modifier will modify fluids below this height in local space.");
				EndError();
			}

			Separator();

			Draw("apply", "How often should this component modify the underlying fluid?\n\nManually = You must manually call the <b>ApplyNow</b> method from code or inspector event.\n\nOnce = The modifier will apply on the first frame after it gets activated.\n\nContinuously = This modifier will apply every time the underlying fluid updates.");

			if (Any(tgts, t => t.Mode == TARGET.ModeType.AddFluid || t.Mode == TARGET.ModeType.AddFluidClip || t.Mode == TARGET.ModeType.AddFluidClipInv || t.Mode == TARGET.ModeType.AddFluidBelow))
			{
				BeginError(Any(tgts, t => t.Fluid == null));
					Draw("fluid", "When using one of the <b>AddFluid</b> modes, this allows you to specify the fluid properties that get added.");
				EndError();
			}
			if (Any(tgts, t => t.Mode == TARGET.ModeType.AddForce || t.Mode == TARGET.ModeType.RangeAddForce))
			{
				Draw("directions", "When using <b>Mode = AddForce</b>, this allows you to specify the fluid properties that get added.");
			}
			if (Any(tgts, t => t.Mode == TARGET.ModeType.AddForce || t.Mode == TARGET.ModeType.AddForceUniform || t.Mode == TARGET.ModeType.RangeAddForce || t.Mode == TARGET.ModeType.RangeAddForceUniform))
			{
				Draw("angle", "When using <b>Mode = AddForce/Direction</b>, this allows you to specify the direction of the force relative to the forward direction of the modifier.\n\n0 = Forward.\n\n90 = Right.\n\n180 = Back.\n\n270 = Left.");
			}
			if (Any(tgts, t => t.Mode == TARGET.ModeType.ChangeColor || t.Mode == TARGET.ModeType.RangeChangeColor))
			{
				Draw("color", "When using <b>Mode = ChangeColor</b>, this allows you to specify the target color.");
				Draw("channels", "When using <b>Mode = ChangeColor</b>, this allows you to specify which channels in the target color will be used.");
			}
			if (Any(tgts, t => t.Mode == TARGET.ModeType.ChangeESMV || t.Mode == TARGET.ModeType.RangeChangeESMV))
			{
				DrawVector4("esmv", "When using <b>Mode = ChangeESMV</b>, this allows you to specify the target emission/smoothness/metallic/visosity.", "ESMV");
				Draw("channels", "When using <b>Mode = ChangeColor</b>, this allows you to specify which channels in the target color will be used.");
			}

			Draw("strength", "The region modification strength will be multiplied by this amount.");
			Draw("monitorFluidDepth", "If this modifier's mode is set to <b>AddFluid</b> or <b>RemoveFluid</b>, do you want to track how much this modifier adds or removes?");

			Separator();

			Draw("onApplied");

			if (Any(tgts, t => t.MonitorFluidDepth == true))
			{
				Draw("onFluidDepthDelta");
				Draw("onFluidVolumeDelta");
			}

			//DrawSimulations(tgt);
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Modifier", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowModifier.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif