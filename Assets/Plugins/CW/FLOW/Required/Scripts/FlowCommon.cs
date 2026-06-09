using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This class contains some useful methods used by this asset.</summary>
	internal static partial class FlowCommon
	{
		public const string HelpUrlPrefix = "https://carloswilkes.com/Documentation/FLOW#";

		public const string ComponentMenuPrefix = "FLOW/FLOW ";

		public const string GameObjectMenuPrefix = "GameObject/FLOW/";

		private static Mesh quadMesh;
		private static bool quadMeshSet;

		public static Vector4 ChannelToVector(FlowChannel channel)
		{
			switch (channel)
			{
				case FlowChannel.Red:   return new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
				case FlowChannel.Green: return new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
				case FlowChannel.Blue:  return new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
				case FlowChannel.Alpha: return new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
			}

			return Vector4.zero;
		}

		public static Bounds CalculateWorldBounds(Transform transform, Bounds localBounds)
		{
			var center  = transform.TransformPoint(localBounds.center);
			var extents = localBounds.extents;
			var axisX   = transform.TransformVector(extents.x, 0, 0);
			var axisY   = transform.TransformVector(0, extents.y, 0);
			var axisZ   = transform.TransformVector(0, 0, extents.z);

			extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
			extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
			extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
 
			return new Bounds { center = center, extents = extents };
		}

		public static Mesh GetQuadMesh()
		{
			if (quadMeshSet == false)
			{
				var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);

				quadMeshSet = true;
				quadMesh    = gameObject.GetComponent<MeshFilter>().sharedMesh;

				Object.DestroyImmediate(gameObject);
			}

			return quadMesh;
		}

		private static Matrix4x4 identityMatrix = Matrix4x4.identity;

		public static void Draw(Material material, int pass)
		{
			if (material.SetPass(pass) == true)
			{
				Graphics.DrawMeshNow(GetQuadMesh(), identityMatrix, 0);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	internal static partial class FlowCommon
	{
		private static Material cachedShapeOutline;

		private static readonly int _FlowShapeTex     = Shader.PropertyToID("_FlowShapeTex");
		private static readonly int _FlowShapeChannel = Shader.PropertyToID("_FlowShapeChannel");

		public static void DrawShapeOutline(Texture shapeTexture, FlowChannel shapeChannel, Matrix4x4 shapeMatrix)
		{
			if (shapeTexture != null)
			{
				if (cachedShapeOutline == null)
				{
					cachedShapeOutline = CwHelper.CreateTempMaterial("Modifier Shape", "Hidden/FLOW/ShapeOutline");
				}

				cachedShapeOutline.SetTexture(_FlowShapeTex, shapeTexture);
				cachedShapeOutline.SetVector(_FlowShapeChannel, ChannelToVector(shapeChannel));

				if (cachedShapeOutline.SetPass(0) == true)
				{
					Graphics.DrawMeshNow(GetQuadMesh(), shapeMatrix);
				}
			}
		}
	}
}
#endif