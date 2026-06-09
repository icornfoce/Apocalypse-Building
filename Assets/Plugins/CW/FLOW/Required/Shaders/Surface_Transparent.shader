//<HASH>-1517209280</HASH>
////////////////////////////////////////
// Generated with Better Shaders
//
// Auto-generated shader code, don't hand edit!
//
//   Unity Version: 2021.3.0f1
//   Render Pipeline: URP2023
//   Platform: WindowsEditor
////////////////////////////////////////


Shader "FLOW/Surface_Transparent"
{
   Properties
   {
      [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
      [HideInInspector]_QueueControl("_QueueControl", Float) = -1
      [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
      [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
      [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
      


	_Sink("Sink", Float) = 1
	_Emission("Emission", Float) = 1

	[Header(WAVE)]
	[NoScaleOffset]_NormalFoamMap("	Normal (RGB) Foam (A)", 2D) = "bump" {}
	_WaveStrengthMin("	Strength Min", Range(0,2)) = 0.0
	_WaveStrengthMax("	Strength Max", Range(0,2)) = 0.5
	_WaveStrengthScale("	Strength Scale", Float) = 1

	_Tiling ("	Tiling", Float) = 1
	_AnimationSpeed ("	Animation Speed", Float) = 1

	[Header(STOCHASTIC)]
	[Toggle(_STOCHASTIC_ON)] _HasStochastic ("	Enabled", Float) = 0
	[NoScaleOffset]_NoiseMap("	Noise (A)", 2D) = "black" {}

	[Header(FOAM)]
	[KeywordEnum(Off, Alpha, Cutout)] _Foam ("	Mode", Float) = 0
	_FoamBrightness("	Brightness", Range(0,2)) = 1


	[Header(FACETED)]
	[Toggle(_FACETED_ON)] _HasFaceted ("	Enabled", Float) = 0
	_FlatShadingBlend("	Flat Amount", Range(0,3)) = 0.9


	[Header(TRANSPARENCY)]
	[KeywordEnum(Off, Vertical, Depth, March Fluid, March Fluid And Depth)] _Alpha ("	Mode", Float) = 0
	_RangeMax ("	Range Max", Float) = 25
	_AlphaStep ("	Step", Float) = 2.0
	_AlphaMaxSteps ("	Max Steps", Int) = 100
	_AlphaDepthScale ("	Depth Scale", Float) = 0.1


    [Header(UNITY FOG)]
    [Toggle(DISABLEFOG)] _CW_DisableFog("	Disable", Float) = 0


   }
   SubShader
   {
      Tags { "RenderPipeline"="UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

      

      
ZWrite Off ColorMask RGB


        Pass
        {
            Name "Universal Forward"
            Tags 
            { 
                "LightMode" = "UniversalForward"
            }
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
Cull Back
 ZTest LEqual
ZWrite Off

            	ZWrite On


            HLSLPROGRAM

               #pragma vertex Vert
   #pragma fragment Frag

            #if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES30)) 
            #pragma target 3.0
#else
            #pragma target 4.5
#endif

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
    
            // Keywords
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_MEDIUM
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ EVALUATE_SH_VERTEX
            #pragma multi_compile _ EVALUATE_SH_MIXED
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

        
            // GraphKeywords: <None>

            #define SHADER_PASS SHADERPASS_FORWARD
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define _PASSFORWARD 1
            #define _FOG_FRAGMENT 1
            

            
	#include "Flow.cginc"


	#pragma shader_feature_local _ _STOCHASTIC_ON
	#pragma shader_feature_local _FOAM_OFF _FOAM_ALPHA _FOAM_CUTOUT


	#pragma shader_feature_local _ _FACETED_ON


	#pragma shader_feature_local _ALPHA_OFF _ALPHA_VERTICAL _ALPHA_DEPTH _ALPHA_MARCH_FLUID _ALPHA_MARCH_FLUID_AND_DEPTH


    #pragma shader_feature_local DISABLEFOG    


   #define _URP 1

   #define _ALPHABLEND_ON 1
#define _ALPHABLEND_ON 1
#define _SURFACE_TYPE_TRANSPARENT 1
#define _GRABPASSUSED 1
#define REQUIRE_OPAQUE_TEXTURE
#define REQUIRE_DEPTH_TEXTURE


            // this has to be here or specular color will be ignored. Not in SG code
            #if _SIMPLELIT
               #define _SPECULAR_COLOR
            #endif


            // Includes
          
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
        

               #undef WorldNormalVector
      #define WorldNormalVector(data, normal) mul(normal, data.TBNMatrix)
      
      #define UnityObjectToWorldNormal(normal) mul(GetObjectToWorldMatrix(), normal)

      #define _WorldSpaceLightPos0 _MainLightPosition
      
      #define UNITY_DECLARE_TEX2D(name) TEXTURE2D(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2D_NOSAMPLER(name) TEXTURE2D(name);
      #define UNITY_DECLARE_TEX2DARRAY(name) TEXTURE2D_ARRAY(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(name) TEXTURE2D_ARRAY(name);

      #define UNITY_SAMPLE_TEX2DARRAY(tex,coord)            SAMPLE_TEXTURE2D_ARRAY(tex, sampler##tex, coord.xy, coord.z)
      #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##tex, coord.xy, coord.z, lod)
      #define UNITY_SAMPLE_TEX2D(tex, coord)                SAMPLE_TEXTURE2D(tex, sampler##tex, coord)
      #define UNITY_SAMPLE_TEX2D_SAMPLER(tex, samp, coord)  SAMPLE_TEXTURE2D(tex, sampler##samp, coord)

      #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod)   SAMPLE_TEXTURE2D_LOD(tex, sampler_##tex, coord, lod)
      #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) SAMPLE_TEXTURE2D_LOD (tex, sampler##samplertex,coord, lod)
     
      #if defined(UNITY_COMPILER_HLSL)
         #define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
      #else
         #define UNITY_INITIALIZE_OUTPUT(type,name)
      #endif

      #define sampler2D_float sampler2D
      #define sampler2D_half sampler2D

      

      // data across stages, stripped like the above.
      struct VertexToPixel
      {
         float4 pos : SV_POSITION;
         float3 worldPos : TEXCOORD0;
         float3 worldNormal : TEXCOORD1;
         float4 worldTangent : TEXCOORD2;
          float4 texcoord0 : TEXCOORD3;
         // float4 texcoord1 : TEXCOORD4;
         // float4 texcoord2 : TEXCOORD5;

         // #if %TEXCOORD3REQUIREKEY%
         // float4 texcoord3 : TEXCOORD6;
         // #endif

         // #if %SCREENPOSREQUIREKEY%
          float4 screenPos : TEXCOORD7;
         // #endif

         // #if %VERTEXCOLORREQUIREKEY%
          half4 vertexColor : COLOR;
         // #endif

         #if defined(LIGHTMAP_ON)
            float2 lightmapUV : TEXCOORD8;
         #endif
         #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV : TEXCOORD9;
         #endif
         #if !defined(LIGHTMAP_ON)
            float4 probeOcclusion : TEXCOORD8;
            float3 sh : TEXCOORD10;
         #endif

         #if defined(VARYINGS_NEED_FOG_AND_VERTEX_LIGHT)
            float4 fogFactorAndVertexLight : TEXCOORD11;
         #endif

         #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
           float4 shadowCoord : TEXCOORD12;
         #endif

         // #if %EXTRAV2F0REQUIREKEY%
          float4 extraV2F0 : TEXCOORD13;
         // #endif

         // #if %EXTRAV2F1REQUIREKEY%
          float4 extraV2F1 : TEXCOORD14;
         // #endif

         // #if %EXTRAV2F2REQUIREKEY%
         // float4 extraV2F2 : TEXCOORD15;
         // #endif

         // #if %EXTRAV2F3REQUIREKEY%
         // float4 extraV2F3 : TEXCOORD16;
         // #endif

         // #if %EXTRAV2F4REQUIREKEY%
         // float4 extraV2F4 : TEXCOORD17;
         // #endif

         // #if %EXTRAV2F5REQUIREKEY%
         // float4 extraV2F5 : TEXCOORD18;
         // #endif

         // #if %EXTRAV2F6REQUIREKEY%
         // float4 extraV2F6 : TEXCOORD19;
         // #endif

         // #if %EXTRAV2F7REQUIREKEY%
         // float4 extraV2F7 : TEXCOORD20;
         // #endif

         #if UNITY_ANY_INSTANCING_ENABLED
         uint instanceID : CUSTOM_INSTANCE_ID;
         #endif
         #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
         uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
         #endif
         #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
         uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
         #endif
         #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
         FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
         #endif

         #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
            float4 previousPositionCS : TEXCOORD21; // Contain previous transform position (in case of skinning for example)
            float4 positionCS : TEXCOORD22;
         #endif
      };


         
            
            // data describing the user output of a pixel
            struct Surface
            {
               half3 Albedo;
               half Height;
               half3 Normal;
               half Smoothness;
               half3 Emission;
               half Metallic;
               half3 Specular;
               half Occlusion;
               half SpecularPower; // for simple lighting
               half Alpha;
               float outputDepth; // if written, SV_Depth semantic is used. ShaderData.clipPos.z is unused value
               // HDRP Only
               half SpecularOcclusion;
               half SubsurfaceMask;
               half Thickness;
               half CoatMask;
               half CoatSmoothness;
               half Anisotropy;
               half IridescenceMask;
               half IridescenceThickness;
               int DiffusionProfileHash;
               float SpecularAAThreshold;
               float SpecularAAScreenSpaceVariance;
               // requires _OVERRIDE_BAKEDGI to be defined, but is mapped in all pipelines
               float3 DiffuseGI;
               float3 BackDiffuseGI;
               float3 SpecularGI;
               float ior;
               float3 transmittanceColor;
               float atDistance;
               float transmittanceMask;
               // requires _OVERRIDE_SHADOWMASK to be defines
               float4 ShadowMask;

               // for decals
               float NormalAlpha;
               float MAOSAlpha;


            };

            // Data the user declares in blackboard blocks
            struct Blackboard
            {
                
	float  groundHeight;
	float  surfaceHeight;
	float3 surfaceNormal;

	Fluid fluid;


	float foam;


                float blackboardDummyData;
            };

            // data the user might need, this will grow to be big. But easy to strip
            struct ShaderData
            {
               float4 clipPos; // SV_POSITION
               float3 localSpacePosition;
               float3 localSpaceNormal;
               float3 localSpaceTangent;
        
               float3 worldSpacePosition;
               float3 worldSpaceNormal;
               float3 worldSpaceTangent;
               float tangentSign;

               float3 worldSpaceViewDir;
               float3 tangentSpaceViewDir;

               float4 texcoord0;
               float4 texcoord1;
               float4 texcoord2;
               float4 texcoord3;

               float2 screenUV;
               float4 screenPos;

               float4 vertexColor;
               bool isFrontFace;

               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;

               float3x3 TBNMatrix;
               Blackboard blackboard;
            };

            struct VertexData
            {
               #if SHADER_TARGET > 30
               // uint vertexID : SV_VertexID;
               #endif
               float4 vertex : POSITION;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;

               // optimize out mesh coords when not in use by user or lighting system
               #if _URP && (_USINGTEXCOORD1 || _PASSMETA || _PASSFORWARD || _PASSGBUFFER)
                  float4 texcoord1 : TEXCOORD1;
               #endif

               #if _URP && (_USINGTEXCOORD2 || _PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && defined(DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif

               #if _STANDARD && (_USINGTEXCOORD1 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER || _PASSFORWARDADD) && LIGHTMAP_ON)))
                  float4 texcoord1 : TEXCOORD1;
               #endif
               #if _STANDARD && (_USINGTEXCOORD2 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif


               #if _HDRP
                  float4 texcoord1 : TEXCOORD1;
                  float4 texcoord2 : TEXCOORD2;
               #endif

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessVertex 
            {
               float4 vertex : INTERNALTESSPOS;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;
               float4 texcoord1 : TEXCOORD1;
               float4 texcoord2 : TEXCOORD2;

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               // #if %EXTRAV2F0REQUIREKEY%
                float4 extraV2F0 : TEXCOORD5;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                float4 extraV2F1 : TEXCOORD6;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // float4 extraV2F2 : TEXCOORD7;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // float4 extraV2F3 : TEXCOORD8;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // float4 extraV2F4 : TEXCOORD9;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // float4 extraV2F5 : TEXCOORD10;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // float4 extraV2F6 : TEXCOORD11;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // float4 extraV2F7 : TEXCOORD12;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD13; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity : TEXCOORD14;
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ExtraV2F
            {
               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;
               Blackboard blackboard;
               float4 time;
            };


            float3 WorldToTangentSpace(ShaderData d, float3 normal)
            {
               return mul(d.TBNMatrix, normal);
            }

            float3 TangentToWorldSpace(ShaderData d, float3 normal)
            {
               return mul(normal, d.TBNMatrix);
            }

            // in this case, make standard more like SRPs, because we can't fix
            // unity_WorldToObject in HDRP, since it already does macro-fu there

            #if _STANDARD
               float3 TransformWorldToObject(float3 p) { return mul(unity_WorldToObject, float4(p, 1)); };
               float3 TransformObjectToWorld(float3 p) { return mul(unity_ObjectToWorld, float4(p, 1)); };
               float4 TransformWorldToObject(float4 p) { return mul(unity_WorldToObject, p); };
               float4 TransformObjectToWorld(float4 p) { return mul(unity_ObjectToWorld, p); };
               float4x4 GetWorldToObjectMatrix() { return unity_WorldToObject; }
               float4x4 GetObjectToWorldMatrix() { return unity_ObjectToWorld; }
               #if (defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (SHADER_TARGET_SURFACE_ANALYSIS && !SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod) tex.SampleLevel (sampler##tex,coord, lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) tex.SampleLevel (sampler##samplertex,coord, lod)
              #else
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex2D (tex,coord,0,lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord,lod) tex2D (tex,coord,0,lod)
              #endif

               #undef GetWorldToObjectMatrix()

               #define GetWorldToObjectMatrix()   unity_WorldToObject


            #endif

            float3 GetCameraWorldPosition()
            {
               #if _HDRP
                  return GetCameraRelativePositionWS(_WorldSpaceCameraPos);
               #else
                  return _WorldSpaceCameraPos;
               #endif
            }

            #if _GRABPASSUSED
               #if _STANDARD
                  TEXTURE2D(_Grab);
                  SAMPLER(sampler__Grab);
               #endif

               half3 GetSceneColor(float2 uv)
               {
                  #if _STANDARD
                     return SAMPLE_TEXTURE2D(_Grab, sampler__Grab, uv).rgb;
                  #else
                     return SHADERGRAPH_SAMPLE_SCENE_COLOR(uv);
                  #endif
               }
            #endif


      
            #if _STANDARD
               UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
               float GetSceneDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv)); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv)); } 
            #else
               float GetSceneDepth(float2 uv) { return SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv), _ZBufferParams); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv), _ZBufferParams); } 
            #endif

            float3 GetWorldPositionFromDepthBuffer(float2 uv, float3 worldSpaceViewDir)
            {
               float eye = GetLinearEyeDepth(uv);
               float3 camView = mul((float3x3)GetObjectToWorldMatrix(), transpose(mul(GetWorldToObjectMatrix(), UNITY_MATRIX_I_V)) [2].xyz);

               float dt = dot(worldSpaceViewDir, camView);
               float3 div = worldSpaceViewDir/dt;
               float3 wpos = (eye * div) + GetCameraWorldPosition();
               return wpos;
            }
			
			float3 GetSceneWorldPosition(float2 screenUV, float sceneDepth)
			{
				#if _STANDARD
					float4 clipPos  = float4(screenUV * 2.0f - 1.0f, 0.0f, 1.0f);
					float4 viewPos  = mul(unity_CameraInvProjection, clipPos);
					float3 worldDir = mul((float3x3)UNITY_MATRIX_I_V, viewPos);
					
					return _WorldSpaceCameraPos + worldDir * LinearEyeDepth(sceneDepth);
				#else
					float4 clipPos = float4(screenUV * 2.0 - 1.0, sceneDepth, 1.0);
					
					#if UNITY_UV_STARTS_AT_TOP
						clipPos.y = -clipPos.y;
					#endif
					
					float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
					
					worldPos.xyz /= worldPos.w;
					
					#if _HDRP
						worldPos.xyz = GetAbsolutePositionWS(worldPos.xyz);
					#endif
					
					return worldPos.xyz;
				#endif
			}
			
			float GetSceneWorldDistance(float2 screenUV, float sceneDepth)
			{
				return distance(_WorldSpaceCameraPos, GetSceneWorldPosition(screenUV, sceneDepth));
			}

            #if _HDRP
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return GetAbsolutePositionWS(TransformObjectToWorld(pos));
            }
            #else
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return TransformObjectToWorld(pos);
            }
            #endif

            #if _STANDARD
               UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  float4 depthNorms = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture, uv);
                  float3 norms = DecodeViewNormalStereo(depthNorms);
                  norms = mul((float3x3)GetWorldToViewMatrix(), norms) * 0.5 + 0.5;
                  return norms;
               }
            #elif _HDRP && !_DECALSHADER
               
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  NormalData nd;
                  DecodeFromNormalBuffer(_ScreenSize.xy * uv, nd);
                  return nd.normalWS;
               }
            #elif _URP
               #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
               #endif

               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                     return SampleSceneNormals(uv);
                  #else
                     float3 wpos = GetWorldPositionFromDepthBuffer(uv, worldSpaceViewDir);
                     return normalize(-cross(ddx(wpos), ddy(wpos))) * 0.5 + 0.5;
                  #endif

                }
             #endif

             #if _HDRP

               half3 UnpackNormalmapRGorAG(half4 packednormal)
               {
                     // This do the trick
                  packednormal.x *= packednormal.w;

                  half3 normal;
                  normal.xy = packednormal.xy * 2 - 1;
                  normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                  return normal;
               }
               half3 UnpackNormal(half4 packednormal)
               {
                  #if defined(UNITY_NO_DXT5nm)
                     return packednormal.xyz * 2 - 1;
                  #else
                     return UnpackNormalmapRGorAG(packednormal);
                  #endif
               }
            #endif
            #if _HDRP || _URP

               half3 UnpackScaleNormal(half4 packednormal, half scale)
               {
                 #ifndef UNITY_NO_DXT5nm
                   // Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
                   // Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
                   packednormal.x *= packednormal.w;
                 #endif
                   half3 normal;
                   normal.xy = (packednormal.xy * 2 - 1) * scale;
                   normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                   return normal;
               }	

             #endif


            void GetSun(out float3 lightDir, out float3 color)
            {
               lightDir = float3(0.5, 0.5, 0);
               color = 1;
               #if _HDRP
                  if (_DirectionalLightCount > 0)
                  {
                     DirectionalLightData light = _DirectionalLightDatas[0];
                     lightDir = -light.forward.xyz;
                     color = light.color;
                  }
               #elif _STANDARD
			         lightDir = normalize(_WorldSpaceLightPos0.xyz);
                  color = _LightColor0.rgb;
               #elif _URP
	               Light light = GetMainLight();
	               lightDir = light.direction;
	               color = light.color;
               #endif
            }


            
         CBUFFER_START(UnityPerMaterial)

            
	float    _FlowDensity;
	float2   _FlowSeparationXZ;
	float    _FlowSimulationHeight;
	float    _FlowCameraHeight;
	float2   _FlowCountXZ;
	float4   _FlowCoordU000;
	float4   _FlowCoord0V00;
	float    _FlowSpeed;
	float4x4 _FlowMatrix;

	float _Sink;


	float _Emission;
	
	float _WaveStrengthMin;
	float _WaveStrengthMax;
	float _WaveStrengthScale;

	float _Tiling;
	float _AnimationSpeed;
	float _FoamBrightness;


	half _FlatShadingBlend;


	float _RangeMax;
	float _AlphaStep;
	int _AlphaMaxSteps;
	float _AlphaDepthScale;





         CBUFFER_END

         

         

         
	TEXTURE2D(_FlowDataA);
	SAMPLER(sampler_FlowDataA);
	TEXTURE2D(_FlowDataB);
	SAMPLER(sampler_FlowDataB);
	TEXTURE2D(_FlowDataC);
	SAMPLER(sampler_FlowDataC);
	TEXTURE2D(_FlowDataD);
	SAMPLER(sampler_FlowDataD);
	TEXTURE2D(_FlowDataE);
	SAMPLER(sampler_FlowDataE);
	TEXTURE2D(_FlowDataF);
	SAMPLER(sampler_FlowDataF);

	float4 SGT_O2W(float4 v)
	{
		v = mul(GetObjectToWorldMatrix(), v);
		#if _HDRP
			v.xyz = GetAbsolutePositionWS(v.xyz);
		#endif
		return v;
	}

	float4 SGT_W2O(float4 v)
	{
		#if _HDRP
			v.xyz = GetCameraRelativePositionWS(v.xyz);
		#endif
		return mul(GetWorldToObjectMatrix(), v);
	}

	float4 SGT_O2V(float4 v)
	{
		#if _STANDARD
			return float4(UnityObjectToViewPos(v.xyz), 1.0f);
		#else
			return float4(TransformWorldToView(TransformObjectToWorld(v.xyz)), 1.0f);
		#endif
	}

	float GetFluidHeight(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return groundHeight + fluidDepth;
	}

	float2 GetHeightAndDepth(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return float2(groundHeight, fluidDepth);
	}

	Column GetColumn(float2 uv)
	{
		return DecodeColumn(SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0), SAMPLE_TEXTURE2D_LOD(_FlowDataB, sampler_FlowDataB, uv, 0));
	}

	Fluid GetColumnFluid(float2 uv)
	{
		float4 c = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0);
		float4 d = SAMPLE_TEXTURE2D_LOD(_FlowDataD, sampler_FlowDataD, uv, 0);
		float4 e = SAMPLE_TEXTURE2D_LOD(_FlowDataE, sampler_FlowDataE, uv, 0);
		float4 f = SAMPLE_TEXTURE2D_LOD(_FlowDataF, sampler_FlowDataF, uv, 0);
		return DecodeFluid(c, d, e, f);
	}

	bool InsideFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y > columnHAD.x && wpos.y < (columnHAD.x + columnHAD.y);
	}

	bool UnderFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y < (columnHAD.x + columnHAD.y);
	}

	float RayMarchInside(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (InsideFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	float RayMarchUnder(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (UnderFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	void Ext_ModifyVertex0 (inout VertexData v, inout ExtraV2F e)
	{
		float4 wpos        = SGT_O2W(v.vertex);
		float2 columnPixel = mul(_FlowMatrix, float4(wpos.xyz, 1.0f)).xy;
		float2 columnCoord = SnapCoordFromPixel(round(columnPixel), _FlowCountXZ);

		Column column0 = GetColumn(columnCoord);
		Column columnL = GetColumn(columnCoord - _FlowCoordU000.xy);
		Column columnR = GetColumn(columnCoord + _FlowCoordU000.xy);
		Column columnB = GetColumn(columnCoord - _FlowCoord0V00.xy);
		Column columnT = GetColumn(columnCoord + _FlowCoord0V00.xy);

		Fluid fluid0 = GetColumnFluid(columnCoord);
		Fluid fluidL = GetColumnFluid(columnCoord - _FlowCoordU000.xy);
		Fluid fluidR = GetColumnFluid(columnCoord + _FlowCoordU000.xy);
		Fluid fluidB = GetColumnFluid(columnCoord - _FlowCoord0V00.xy);
		Fluid fluidT = GetColumnFluid(columnCoord + _FlowCoord0V00.xy);

		float ww = fluidL.Depth + fluidR.Depth + fluidB.Depth + fluidT.Depth + 0.001f;
		float wL = fluidL.Depth / ww;
		float wR = fluidR.Depth / ww;
		float wB = fluidB.Depth / ww;
		float wT = fluidT.Depth / ww;
		float w0 = saturate(fluid0.Depth * 10);

		float hL = columnL.GroundHeight + fluidL.Depth;
		float hR = columnR.GroundHeight + fluidR.Depth;
		float hB = columnB.GroundHeight + fluidB.Depth;
		float hT = columnT.GroundHeight + fluidT.Depth;
		float hh = hL * wL + hR * wR + hB * wB + hT * wT;
		float h0 = column0.GroundHeight + fluid0.Depth;

		hh = lerp(hh, h0 - _Sink, saturate(0.01f / ww)); // Prevent skirts going down too far

		Fluid fluid = fluid0;

		fluid.Depth = lerp(fluidL.Depth * wL + fluidR.Depth * wR + fluidB.Depth * wB + fluidT.Depth * wT, fluid.Depth, w0);
		fluid.RGBA  = lerp(fluidL.RGBA  * wL + fluidR.RGBA  * wR + fluidB.RGBA  * wB + fluidT.RGBA  * wT, fluid.RGBA , w0);
		fluid.ESMV  = lerp(fluidL.ESMV  * wL + fluidR.ESMV  * wR + fluidB.ESMV  * wB + fluidT.ESMV  * wT, fluid.ESMV , w0);
		fluid.F123  = lerp(fluidL.F123  * wL + fluidR.F123  * wR + fluidB.F123  * wB + fluidT.F123  * wT, fluid.F123 , w0);

		e.blackboard.groundHeight  = column0.GroundHeight;
		e.blackboard.surfaceHeight = lerp(hh, h0, w0);
		e.blackboard.surfaceNormal = normalize(float3((hL - hR) / _FlowSeparationXZ.x, 2.0f, (hB - hT) / _FlowSeparationXZ.y));
		e.blackboard.fluid         = fluid;
	}


	TEXTURE2D(_NoiseMap);
	SAMPLER(sampler_NoiseMap);

	TEXTURE2D(_NormalFoamMap);
	SAMPLER(sampler_NormalFoamMap);

	void Ext_ModifyVertex3 (inout VertexData v, inout ExtraV2F e)
	{
		v.vertex.y = lerp(e.blackboard.groundHeight, e.blackboard.surfaceHeight, v.texcoord0.w) - _FlowSimulationHeight;

		v.normal = lerp(e.blackboard.surfaceNormal, v.normal, v.texcoord0.z);

		v.vertexColor = e.blackboard.fluid.RGBA;

		e.extraV2F0.xyz = SGT_O2V(v.vertex).xyz;
		e.extraV2F0.w   = e.blackboard.fluid.Depth;

		e.extraV2F1.xyz = e.blackboard.fluid.ESMV.xyz;
		e.extraV2F1.w = e.blackboard.fluid.F123.x;
	}

	float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float tiling, float time, float phaseOffset)
	{
		float progress = frac(time + phaseOffset);
		float3 uvw;
		uvw.xy = uv - flowVector * (progress - 0.5f);
		uvw.xy *= tiling;
		uvw.xy += phaseOffset;
		uvw.xy += (time - progress) * jump;
		uvw.z = 1 - abs(1 - 2 * progress);
		return uvw;
	}

	float3 CombineNormals(float3 n1, float3 n2)
	{
		return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
	}

	float3 UnpackNormalAndScale(float2 xy, float scale)
	{
		xy = xy * 2.0f - 1.0f; xy *= scale; return float3(xy, sqrt(1.0 - saturate(dot(xy, xy))));
	}

	float4 SampleStochastic(float2 uv, float noise)
	{
		float cur_height = uv.y + noise;
		float this_index = floor(cur_height);
		float next_index = this_index + 1.0f;

		float2 uvA     = uv + sin(float2(1.0f, 2.0f) * this_index);
		float2 uvB     = uv + sin(float2(1.0f, 2.0f) * next_index);
		float2 gradX   = ddx(uv);
		float2 gradY   = ddy(uv);

		float4 sampleA = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvA, gradX, gradY);
		float4 sampleB = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvB, gradX, gradY);

		return lerp(sampleA, sampleB, cur_height - this_index);
	}

	void Ext_SurfaceFunction3 (inout Surface o, inout ShaderData d)
	{
		d.blackboard.fluid.RGBA     = d.vertexColor;
		d.blackboard.fluid.Depth    = d.extraV2F0.w;
		d.blackboard.fluid.ESMV.xyz = d.extraV2F1.xyz;
		d.blackboard.fluid.F123.x   = d.extraV2F1.w;

		if (d.blackboard.fluid.Depth < 0.01f)
		{
			discard;
		}

		float2 uv          = d.worldSpacePosition.xz;
		float2 columnPixel = mul(_FlowMatrix, float4(d.worldSpacePosition, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		Column column      = GetColumn(columnCoord);

		float  time = _Time.y * _AnimationSpeed;
		float2 jump = float2(0.13f, 0.17f);
		float2 fvec = (column.Outflow.yw - column.Outflow.xz) * _FlowSeparationXZ * _FlowSpeed;
		float  fmag = log10(1.0f + length(fvec)) * 0.1f;

		float3 flowA = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.0f);
		float3 flowB = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.5f);

		#if _STOCHASTIC_ON
			float  noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 0.0025f).a * 4.0f;
			float4 nfmA  = SampleStochastic(flowA, noise);
			float4 nfmB  = SampleStochastic(flowB, noise);
		#else
			float4 nfmA = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowA.xy);
			float4 nfmB = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowB.xy);
		#endif

		// Water normals
		float  normalStr    = lerp(_WaveStrengthMin, _WaveStrengthMax, saturate(_WaveStrengthScale * fmag));
		float3 waterNormalA = UnpackNormalAndScale(nfmA.xy, flowA.z * normalStr);
		float3 waterNormalB = UnpackNormalAndScale(nfmB.xy, flowB.z * normalStr);
		float3 waterNormals = CombineNormals(waterNormalA, waterNormalB);

		o.Normal   = lerp(waterNormals, o.Normal, d.texcoord0.z);
		o.Albedo   = d.blackboard.fluid.RGBA.xyz;
		o.Emission = d.blackboard.fluid.ESMV.x * _Emission * o.Albedo;

		#if _FOAM_OFF
			d.blackboard.foam = 0.0f;
		#else
			d.blackboard.foam = d.blackboard.fluid.F123.x * (1.0f - d.texcoord0.z);

			#if _FOAM_ALPHA
				float3 foamAlbedo = (nfmA.w * flowA.z + nfmB.w * flowB.z) * _FoamBrightness;
			#elif _FOAM_CUTOUT
				float3 foamAlbedo = _FoamBrightness;
				float  foamRamp   = nfmA.w * flowA.z + nfmB.w * flowB.z;
				d.blackboard.foam = saturate((d.blackboard.foam - foamRamp) * 10.0f);
			#endif

			float3 foamNormal = float3(0.0f, 0.0f, 1.0f);

			o.Albedo = lerp(o.Albedo, foamAlbedo, d.blackboard.foam);
			o.Normal = lerp(o.Normal, foamNormal, d.blackboard.foam);
		#endif

		o.Smoothness = d.blackboard.fluid.ESMV.y;
		o.Metallic   = d.blackboard.fluid.ESMV.z;
	}


	void Ext_SurfaceFunction4 (inout Surface o, inout ShaderData d)
	{
	#if _FACETED_ON
		// lets just affect the TBN data, so we flat shade the original polygons, not the normal map
		float3 dx = ddx(d.worldSpacePosition);
		float3 dy = ddy(d.worldSpacePosition);
		float3 worldNormal = normalize(cross(dy, dx));
		worldNormal = lerp(d.worldSpaceNormal, worldNormal, _FlatShadingBlend);
		d.worldSpaceNormal = worldNormal;
		d.TBNMatrix[2] = worldNormal;
	#endif
	}


	void Ext_SurfaceFunction5 (inout Surface o, ShaderData d)
	{
		#if _ALPHA_OFF
		#else
			float3 position  = d.worldSpacePosition;
			float3 step      = d.worldSpacePosition - _WorldSpaceCameraPos;
			float2 bentScrUV = d.screenUV + o.Normal.xy * 0.05f * log(1.0f + d.blackboard.fluid.Depth * 1.0f);
			float  distMax   = max(_RangeMax * 0.001f, _RangeMax * (1.0f - d.blackboard.fluid.RGBA.w));
			float  distRange = distMax;
			float  vertDist  = length(d.extraV2F0.xyz);
			float  camtDist  = GetLinearEyeDepth(bentScrUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);

			if (camtDist < vertDist)
			{
				camtDist = GetLinearEyeDepth(d.screenUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);
			}

			float diffDist = max(0.0f, camtDist - vertDist);

			// The depth buffer becomes unusable at certain ranges, so fade it out
			diffDist += max(0.0f, camtDist * _AlphaDepthScale - _RangeMax);
			distRange = min(distRange, diffDist);

			#if _ALPHA_VERTICAL
				float dist = d.blackboard.fluid.Depth;
			#elif _ALPHA_DEPTH
				float dist = diffDist;
			#elif _ALPHA_MARCH_FLUID
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distMax);
			#elif _ALPHA_MARCH_FLUID_AND_DEPTH
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distRange);
			#endif

			// Set opacity based on distance through fluid relative to the maximum distance, and make sure high opacity fluids become solid
			float opacity = saturate(dist / distMax + d.blackboard.foam + pow(d.blackboard.fluid.RGBA.w, 10.0f));

			o.Albedo    = lerp(0.0f, o.Albedo, opacity);
			o.Emission  = lerp(GetSceneColor(bentScrUV), o.Emission, opacity);
			//o.Emission += o.Albedo * d.blackboard.fluid.ESMV.x * opacity;
		#endif
	}





        
            void ChainSurfaceFunction(inout Surface l, inout ShaderData d)
            {
                 // Ext_SurfaceFunction0(l, d);
                 // Ext_SurfaceFunction1(l, d);
                 // Ext_SurfaceFunction2(l, d);
                  Ext_SurfaceFunction3(l, d);
                  Ext_SurfaceFunction4(l, d);
                  Ext_SurfaceFunction5(l, d);
                 // Ext_SurfaceFunction6(l, d);
                 // Ext_SurfaceFunction7(l, d);
                 // Ext_SurfaceFunction8(l, d);
                 // Ext_SurfaceFunction9(l, d);
		           // Ext_SurfaceFunction10(l, d);
                 // Ext_SurfaceFunction11(l, d);
                 // Ext_SurfaceFunction12(l, d);
                 // Ext_SurfaceFunction13(l, d);
                 // Ext_SurfaceFunction14(l, d);
                 // Ext_SurfaceFunction15(l, d);
                 // Ext_SurfaceFunction16(l, d);
                 // Ext_SurfaceFunction17(l, d);
                 // Ext_SurfaceFunction18(l, d);
		           // Ext_SurfaceFunction19(l, d);
                 // Ext_SurfaceFunction20(l, d);
                 // Ext_SurfaceFunction21(l, d);
                 // Ext_SurfaceFunction22(l, d);
                 // Ext_SurfaceFunction23(l, d);
                 // Ext_SurfaceFunction24(l, d);
                 // Ext_SurfaceFunction25(l, d);
                 // Ext_SurfaceFunction26(l, d);
                 // Ext_SurfaceFunction27(l, d);
                 // Ext_SurfaceFunction28(l, d);
		           // Ext_SurfaceFunction29(l, d);
            }

#if !_DECALSHADER

            void ChainModifyVertex(inout VertexData v, inout VertexToPixel v2p, float4 time)
            {
                 ExtraV2F d;
                 
                 ZERO_INITIALIZE(ExtraV2F, d);
                 ZERO_INITIALIZE(Blackboard, d.blackboard);
                 // due to motion vectors in HDRP, we need to use the last
                 // time in certain spots. So if you are going to use _Time to adjust vertices,
                 // you need to use this time or motion vectors will break. 
                 d.time = time;

                   Ext_ModifyVertex0(v, d);
                 // Ext_ModifyVertex1(v, d);
                 // Ext_ModifyVertex2(v, d);
                  Ext_ModifyVertex3(v, d);
                 // Ext_ModifyVertex4(v, d);
                 // Ext_ModifyVertex5(v, d);
                 // Ext_ModifyVertex6(v, d);
                 // Ext_ModifyVertex7(v, d);
                 // Ext_ModifyVertex8(v, d);
                 // Ext_ModifyVertex9(v, d);
                 // Ext_ModifyVertex10(v, d);
                 // Ext_ModifyVertex11(v, d);
                 // Ext_ModifyVertex12(v, d);
                 // Ext_ModifyVertex13(v, d);
                 // Ext_ModifyVertex14(v, d);
                 // Ext_ModifyVertex15(v, d);
                 // Ext_ModifyVertex16(v, d);
                 // Ext_ModifyVertex17(v, d);
                 // Ext_ModifyVertex18(v, d);
                 // Ext_ModifyVertex19(v, d);
                 // Ext_ModifyVertex20(v, d);
                 // Ext_ModifyVertex21(v, d);
                 // Ext_ModifyVertex22(v, d);
                 // Ext_ModifyVertex23(v, d);
                 // Ext_ModifyVertex24(v, d);
                 // Ext_ModifyVertex25(v, d);
                 // Ext_ModifyVertex26(v, d);
                 // Ext_ModifyVertex27(v, d);
                 // Ext_ModifyVertex28(v, d);
                 // Ext_ModifyVertex29(v, d);


                 // #if %EXTRAV2F0REQUIREKEY%
                  v2p.extraV2F0 = d.extraV2F0;
                 // #endif

                 // #if %EXTRAV2F1REQUIREKEY%
                  v2p.extraV2F1 = d.extraV2F1;
                 // #endif

                 // #if %EXTRAV2F2REQUIREKEY%
                 // v2p.extraV2F2 = d.extraV2F2;
                 // #endif

                 // #if %EXTRAV2F3REQUIREKEY%
                 // v2p.extraV2F3 = d.extraV2F3;
                 // #endif

                 // #if %EXTRAV2F4REQUIREKEY%
                 // v2p.extraV2F4 = d.extraV2F4;
                 // #endif

                 // #if %EXTRAV2F5REQUIREKEY%
                 // v2p.extraV2F5 = d.extraV2F5;
                 // #endif

                 // #if %EXTRAV2F6REQUIREKEY%
                 // v2p.extraV2F6 = d.extraV2F6;
                 // #endif

                 // #if %EXTRAV2F7REQUIREKEY%
                 // v2p.extraV2F7 = d.extraV2F7;
                 // #endif
            }

            void ChainModifyTessellatedVertex(inout VertexData v, inout VertexToPixel v2p)
            {
               ExtraV2F d;
               ZERO_INITIALIZE(ExtraV2F, d);
               ZERO_INITIALIZE(Blackboard, d.blackboard);

               // #if %EXTRAV2F0REQUIREKEY%
                d.extraV2F0 = v2p.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                d.extraV2F1 = v2p.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // d.extraV2F2 = v2p.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // d.extraV2F3 = v2p.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // d.extraV2F4 = v2p.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // d.extraV2F5 = v2p.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // d.extraV2F6 = v2p.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // d.extraV2F7 = v2p.extraV2F7;
               // #endif


               // Ext_ModifyTessellatedVertex0(v, d);
               // Ext_ModifyTessellatedVertex1(v, d);
               // Ext_ModifyTessellatedVertex2(v, d);
               // Ext_ModifyTessellatedVertex3(v, d);
               // Ext_ModifyTessellatedVertex4(v, d);
               // Ext_ModifyTessellatedVertex5(v, d);
               // Ext_ModifyTessellatedVertex6(v, d);
               // Ext_ModifyTessellatedVertex7(v, d);
               // Ext_ModifyTessellatedVertex8(v, d);
               // Ext_ModifyTessellatedVertex9(v, d);
               // Ext_ModifyTessellatedVertex10(v, d);
               // Ext_ModifyTessellatedVertex11(v, d);
               // Ext_ModifyTessellatedVertex12(v, d);
               // Ext_ModifyTessellatedVertex13(v, d);
               // Ext_ModifyTessellatedVertex14(v, d);
               // Ext_ModifyTessellatedVertex15(v, d);
               // Ext_ModifyTessellatedVertex16(v, d);
               // Ext_ModifyTessellatedVertex17(v, d);
               // Ext_ModifyTessellatedVertex18(v, d);
               // Ext_ModifyTessellatedVertex19(v, d);
               // Ext_ModifyTessellatedVertex20(v, d);
               // Ext_ModifyTessellatedVertex21(v, d);
               // Ext_ModifyTessellatedVertex22(v, d);
               // Ext_ModifyTessellatedVertex23(v, d);
               // Ext_ModifyTessellatedVertex24(v, d);
               // Ext_ModifyTessellatedVertex25(v, d);
               // Ext_ModifyTessellatedVertex26(v, d);
               // Ext_ModifyTessellatedVertex27(v, d);
               // Ext_ModifyTessellatedVertex28(v, d);
               // Ext_ModifyTessellatedVertex29(v, d);

               // #if %EXTRAV2F0REQUIREKEY%
                v2p.extraV2F0 = d.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                v2p.extraV2F1 = d.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // v2p.extraV2F2 = d.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // v2p.extraV2F3 = d.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // v2p.extraV2F4 = d.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // v2p.extraV2F5 = d.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // v2p.extraV2F6 = d.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // v2p.extraV2F7 = d.extraV2F7;
               // #endif
            }

            void ChainFinalColorForward(inout Surface l, inout ShaderData d, inout half4 color)
            {
               //   Ext_FinalColorForward0(l, d, color);
               //   Ext_FinalColorForward1(l, d, color);
               //   Ext_FinalColorForward2(l, d, color);
               //   Ext_FinalColorForward3(l, d, color);
               //   Ext_FinalColorForward4(l, d, color);
               //   Ext_FinalColorForward5(l, d, color);
               //   Ext_FinalColorForward6(l, d, color);
               //   Ext_FinalColorForward7(l, d, color);
               //   Ext_FinalColorForward8(l, d, color);
               //   Ext_FinalColorForward9(l, d, color);
               //  Ext_FinalColorForward10(l, d, color);
               //  Ext_FinalColorForward11(l, d, color);
               //  Ext_FinalColorForward12(l, d, color);
               //  Ext_FinalColorForward13(l, d, color);
               //  Ext_FinalColorForward14(l, d, color);
               //  Ext_FinalColorForward15(l, d, color);
               //  Ext_FinalColorForward16(l, d, color);
               //  Ext_FinalColorForward17(l, d, color);
               //  Ext_FinalColorForward18(l, d, color);
               //  Ext_FinalColorForward19(l, d, color);
               //  Ext_FinalColorForward20(l, d, color);
               //  Ext_FinalColorForward21(l, d, color);
               //  Ext_FinalColorForward22(l, d, color);
               //  Ext_FinalColorForward23(l, d, color);
               //  Ext_FinalColorForward24(l, d, color);
               //  Ext_FinalColorForward25(l, d, color);
               //  Ext_FinalColorForward26(l, d, color);
               //  Ext_FinalColorForward27(l, d, color);
               //  Ext_FinalColorForward28(l, d, color);
               //  Ext_FinalColorForward29(l, d, color);
            }

            void ChainFinalGBufferStandard(inout Surface s, inout ShaderData d, inout half4 GBuffer0, inout half4 GBuffer1, inout half4 GBuffer2, inout half4 outEmission, inout half4 outShadowMask)
            {
               //   Ext_FinalGBufferStandard0(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard1(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard2(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard3(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard4(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard5(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard6(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard7(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard8(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard9(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard10(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard11(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard12(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard13(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard14(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard15(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard16(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard17(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard18(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard19(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard20(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard21(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard22(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard23(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard24(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard25(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard26(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard27(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard28(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard29(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
            }
#endif


         


#if _DECALSHADER

        ShaderData CreateShaderData(SurfaceDescriptionInputs IN)
        {
            ShaderData d = (ShaderData)0;
            d.TBNMatrix = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
            d.worldSpaceNormal = IN.WorldSpaceNormal;
            d.worldSpaceTangent = IN.WorldSpaceTangent;

            d.worldSpacePosition = IN.WorldSpacePosition;
            d.texcoord0 = IN.uv0.xyxy;
            d.screenPos = IN.ScreenPosition;

            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - d.worldSpacePosition);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(d.worldSpacePosition), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(d.worldSpacePosition, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenUV = (IN.ScreenPosition.xy / max(0.01, IN.ScreenPosition.w));
            // #endif

            return d;
        }
#else

         ShaderData CreateShaderData(VertexToPixel i
                  #if NEED_FACING
                     , bool facing
                  #endif
         )
         {
            ShaderData d = (ShaderData)0;
            d.clipPos = i.pos;
            d.worldSpacePosition = i.worldPos;

            d.worldSpaceNormal = normalize(i.worldNormal);
            d.worldSpaceTangent.xyz = normalize(i.worldTangent.xyz);

            d.tangentSign = i.worldTangent.w * unity_WorldTransformParams.w;
            float3 bitangent = cross(d.worldSpaceTangent.xyz, d.worldSpaceNormal) * d.tangentSign;
           
            d.TBNMatrix = float3x3(d.worldSpaceTangent, -bitangent, d.worldSpaceNormal);
            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);
             d.texcoord0 = i.texcoord0;
            // d.texcoord1 = i.texcoord1;
            // d.texcoord2 = i.texcoord2;

            // #if %TEXCOORD3REQUIREKEY%
            // d.texcoord3 = i.texcoord3;
            // #endif

            // d.isFrontFace = facing;
            // #if %VERTEXCOLORREQUIREKEY%
             d.vertexColor = i.vertexColor;
            // #endif

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(i.worldPos), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(i.worldPos, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenPos = i.screenPos;
             d.screenUV = (i.screenPos.xy / i.screenPos.w);
            // #endif


            // #if %EXTRAV2F0REQUIREKEY%
             d.extraV2F0 = i.extraV2F0;
            // #endif

            // #if %EXTRAV2F1REQUIREKEY%
             d.extraV2F1 = i.extraV2F1;
            // #endif

            // #if %EXTRAV2F2REQUIREKEY%
            // d.extraV2F2 = i.extraV2F2;
            // #endif

            // #if %EXTRAV2F3REQUIREKEY%
            // d.extraV2F3 = i.extraV2F3;
            // #endif

            // #if %EXTRAV2F4REQUIREKEY%
            // d.extraV2F4 = i.extraV2F4;
            // #endif

            // #if %EXTRAV2F5REQUIREKEY%
            // d.extraV2F5 = i.extraV2F5;
            // #endif

            // #if %EXTRAV2F6REQUIREKEY%
            // d.extraV2F6 = i.extraV2F6;
            // #endif

            // #if %EXTRAV2F7REQUIREKEY%
            // d.extraV2F7 = i.extraV2F7;
            // #endif

            return d;
         }

#endif

         
         #if defined(_PASSSHADOW)
            float3 _LightDirection;
            float3 _LightPosition;
         #endif

         #if (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))

            #define GetWorldToViewMatrix()     _ViewMatrix
            #define UNITY_MATRIX_I_V   _InvViewMatrix
            #define GetViewToHClipMatrix()     OptimizeProjectionMatrix(_ProjMatrix)
            #define UNITY_MATRIX_I_P   _InvProjMatrix
            #define GetWorldToHClipMatrix()    _ViewProjMatrix
            #define UNITY_MATRIX_I_VP  _InvViewProjMatrix
            #define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
            #define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix
            #define UNITY_MATRIX_PREV_I_VP _PrevInvViewProjMatrix

            void MotionVectorPositionZBias(VertexToPixel input)
            {
                #if UNITY_REVERSED_Z
                input.pos.z -= unity_MotionVectorsParams.z * input.pos.w;
                #else
                input.pos.z += unity_MotionVectorsParams.z * input.pos.w;
                #endif
            }

        #endif

         // vertex shader
         VertexToPixel Vert (VertexData v)
         {
           VertexToPixel o = (VertexToPixel)0;

           UNITY_SETUP_INSTANCE_ID(v);
           UNITY_TRANSFER_INSTANCE_ID(v, o);
           UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            
           #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
             VertexData previousMesh = v;
           #endif
           #if !_TESSELLATION_ON
             ChainModifyVertex(v, o, _Time);
           #endif

            o.texcoord0 = v.texcoord0;
           // o.texcoord1 = v.texcoord1;
           // o.texcoord2 = v.texcoord2;

           // #if %TEXCOORD3REQUIREKEY%
           // o.texcoord3 = v.texcoord3;
           // #endif

           // #if %VERTEXCOLORREQUIREKEY%
            o.vertexColor = v.vertexColor;
           // #endif

           // This return the camera relative position (if enable)
           float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
           float3 normalWS = TransformObjectToWorldNormal(v.normal);
           float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
           
           VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
           o.worldPos = positionWS;
           o.worldNormal = normalWS;
           o.worldTangent = tangentWS;


          // For some very odd reason, in 2021.2, we can't use Unity's defines, but have to use our own..
          #if _PASSSHADOW
              #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                 float3 lightDirectionWS = normalize(_LightPosition - o.worldPos);
              #else
                 float3 lightDirectionWS = _LightDirection;
              #endif
              // Define shadow pass specific clip position for Universal
              o.pos = TransformWorldToHClip(ApplyShadowBias(o.worldPos, o.worldNormal, lightDirectionWS));
              #if UNITY_REVERSED_Z
                  o.pos.z = min(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #else
                  o.pos.z = max(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #endif
          #elif _PASSMETA
              o.pos = MetaVertexPosition(float4(v.vertex.xyz, 0), v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #else
              o.pos = TransformWorldToHClip(o.worldPos);
          #endif

          // #if %SCREENPOSREQUIREKEY%
           o.screenPos = ComputeScreenPos(o.pos, _ProjectionParams.x);
          // #endif

          
          #if _PASSFORWARD || _PASSGBUFFER
              float2 uv1 = v.texcoord1.xy;
              OUTPUT_LIGHTMAP_UV(uv1, unity_LightmapST, o.lightmapUV);
              // o.texcoord1.xy = uv1;
              OUTPUT_SH(o.worldNormal, o.sh);
              
              #if defined(DYNAMICLIGHTMAP_ON)
                   o.dynamicLightmapUV.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                   #if UNITY_VERSION >= 60000009
                     OUTPUT_SH(o.worldNormal, o.sh);
                   #endif
              #elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && UNITY_VERSION >= 60000009
                   OUTPUT_SH4(vertexInput.positionWS, o.worldNormal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), o.sh, o.probeOcclusion);
              #endif
          #endif

          #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
              half fogFactor = 0;
              #if defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(o.pos.z);
              #endif
              #if _BAKEDLIT
                 o.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);
              #else
                 half3 vertexLight = VertexLighting(o.worldPos, o.worldNormal);
                 o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
              #endif
          #endif

          #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             o.shadowCoord = GetShadowCoord(vertexInput);
          #endif

          #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
            #if !defined(TESSELLATION_ON)
              MotionVectorPositionZBias(o);
            #endif

            o.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
            // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
            bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

            if (!forceNoMotion)
            {
              #if defined(HAVE_VFX_MODIFICATION)
                float3 previousPositionOS = currentFrameMvData.vfxParticlePositionOS;
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  const bool applyDeformation = false;
                #else
                  const bool applyDeformation = true;
                #endif
              #else
                const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
                float3 previousPositionOS = hasDeformation ? previousMesh.previousPositionOS : previousMesh.vertex.xyz;

                #if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
                  const bool applyDeformation = true;
                #else
                  const bool applyDeformation = hasDeformation;
                #endif
              #endif
              // TODO
              #if defined(FEATURES_GRAPH_VERTEX)
                if (applyDeformation)
                  previousPositionOS = GetLastFrameDeformedPosition(previousMesh, currentFrameMvData, previousPositionOS);
                else
                  previousPositionOS = previousMesh.positionOS;

                #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT)
                  previousPositionOS -= previousMesh.precomputedVelocity;
                #endif
              #endif

              #if defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(DOTS_DEFORMED)
                // Deformed vertices in DOTS are not cumulative with built-in Unity skinning/blend shapes
                // Needs to be called after vertex modification has been applied otherwise it will be
                // overwritten by Compute Deform node
                ApplyPreviousFrameDeformedVertexPosition(previousMesh.vertexID, previousPositionOS);
              #endif
              #if defined (_ADD_PRECOMPUTED_VELOCITY)
                previousPositionOS -= previousMesh.precomputedVelocity;
              #endif
              o.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(positionWS, 1.0f));

              #if defined(HAVE_VFX_MODIFICATION)
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT) || defined(_ADD_PRECOMPUTED_VELOCITY)
                    #error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
                  #endif
                  o.previousPositionCS = VFXGetPreviousClipPosition(previousMesh, currentFrameMvData.vfxElementAttributes, o.positionCS);
                #else
                  #if VFX_WORLD_SPACE
                    //previousPositionOS is already in world space
                    const float3 previousPositionWS = previousPositionOS;
                  #else
                    const float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz;
                  #endif
                  o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionWS, 1.0f));
                #endif
              #else
                o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1)));
              #endif
            }
          #endif

          return o;
         }


         

#if _UNLIT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"  
#endif

         // fragment shader
         void Frag (VertexToPixel IN
              , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
              , out float4 outRenderingLayers : SV_Target1
            #endif
            #ifdef _DEPTHOFFSET_ON
              , out float outputDepth : SV_Depth
            #endif
            #if NEED_FACING
               , bool facing : SV_IsFrontFace
            #endif
         )
         {
           UNITY_SETUP_INSTANCE_ID(IN);
           UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

           #if defined(LOD_FADE_CROSSFADE)
              LODFadeCrossFade(IN.pos);
           #endif


           ShaderData d = CreateShaderData(IN
                  #if NEED_FACING
                     , facing
                  #endif
               );
           Surface l = (Surface)0;

           #ifdef _DEPTHOFFSET_ON
              l.outputDepth = outputDepth;
           #endif

           l.Albedo = half3(0.5, 0.5, 0.5);
           l.Normal = float3(0,0,1);
           l.Occlusion = 1;
           l.Alpha = 1;

           ChainSurfaceFunction(l, d);

           #ifdef _DEPTHOFFSET_ON
              outputDepth = l.outputDepth;
           #endif

           #if _USESPECULAR || _SIMPLELIT
              float3 specular = l.Specular;
              float metallic = 1;
           #else   
              float3 specular = 0;
              float metallic = l.Metallic;
           #endif


            
           
            InputData inputData = (InputData)0;

            inputData.positionWS = IN.worldPos;
            #if _WORLDSPACENORMAL
              inputData.normalWS = l.Normal;
            #else
              inputData.normalWS = normalize(TangentToWorldSpace(d, l.Normal));
            #endif

            inputData.viewDirectionWS = SafeNormalize(d.worldSpaceViewDir);


            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                  inputData.shadowCoord = IN.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                  inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
            #else
                  inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
            
#if _BAKEDLIT
            inputData.fogCoord = IN.fogFactorAndVertexLight.x;
            inputData.vertexLighting = 0;
#else
            inputData.fogCoord = InitializeInputDataFog(float4(IN.worldPos, 1.0), IN.fogFactorAndVertexLight.x);
            inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
#endif    



            #if defined(_OVERRIDE_BAKEDGI)
               inputData.bakedGI = l.DiffuseGI;
               l.Emission += l.SpecularGI;
            #elif _BAKEDLIT
               inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.sh, inputData.normalWS);
            #else
               #if defined(DYNAMICLIGHTMAP_ON)
                  inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.dynamicLightmapUV.xy, IN.sh, inputData.normalWS);
                  inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUV);
               #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                  #if UNITY_VERSION >= 60000009
                     inputData.bakedGI = SAMPLE_GI(IN.sh, IN.worldPos, inputData.normalWS, inputData.viewDirectionWS, IN.pos, IN.probeOcclusion, inputData.shadowMask);
                  #else
                     inputData.bakedGI = SAMPLE_GI(IN.sh, IN.worldPos, inputData.normalWS, inputData.viewDirectionWS, IN.pos);
                  #endif
               #else
                  inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.sh, inputData.normalWS);
                  inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUV);
               #endif
            #endif
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.pos);
            #if !_BAKEDLIT
               inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUV);
           
               #if defined(_OVERRIDE_SHADOWMASK)
                  float4 mulColor = saturate(dot(l.ShadowMask, _MainLightOcclusionProbes)); //unity_OcclusionMaskSelector));
                  inputData.shadowMask = mulColor;
               #endif
            #else
               inputData.shadowMask = float4(1,1,1,1);
            #endif

            #if defined(DEBUG_DISPLAY)
                #if defined(DYNAMICLIGHTMAP_ON)
                  inputData.dynamicLightmapUV = IN.dynamicLightmapUV.xy;
                #endif
                #if defined(LIGHTMAP_ON)
                  inputData.staticLightmapUV = IN.lightmapUV;
                #else
                  inputData.vertexSH = IN.sh;
                #endif
            #endif

            #if _WORLDSPACENORMAL
              float3 normalTS = WorldToTangentSpace(d, l.Normal);
            #else
              float3 normalTS = l.Normal;
            #endif

            SurfaceData surface         = (SurfaceData)0;
            surface.albedo              = l.Albedo;
            surface.metallic            = saturate(metallic);
            surface.specular            = specular;
            surface.smoothness          = saturate(l.Smoothness),
            surface.occlusion           = l.Occlusion,
            surface.emission            = l.Emission,
            surface.alpha               = saturate(l.Alpha);
            surface.clearCoatMask       = 0;
            surface.clearCoatSmoothness = 1;

            #ifdef _CLEARCOAT
                  surface.clearCoatMask       = saturate(l.CoatMask);
                  surface.clearCoatSmoothness = saturate(l.CoatSmoothness);
            #endif

            #if !_UNLIT
               half4 color = half4(l.Albedo, l.Alpha);
               #ifdef _DBUFFER
                  #if _BAKEDLIT
                     half3 bakeColor = color.rgb;
                     float3 bakeNormal = inputData.normalWS.xyz;
                     ApplyDecalToBaseColorAndNormal(IN.pos, bakeColor, bakeNormal);
                     color.rgb = bakeColor;
                     inputData.normalWS.xyz = bakeNormal;
                  #else
                     ApplyDecalToSurfaceData(IN.pos, surface, inputData);
                  #endif
               #endif
               #if _SIMPLELIT
                  color = UniversalFragmentBlinnPhong(
                     inputData,
                     surface);
               #elif _BAKEDLIT
                  color = UniversalFragmentBakedLit(inputData, color.rgb, color.a, normalTS);
               #else
                  color = UniversalFragmentPBR(inputData, surface);
               #endif

               #if !DISABLEFOG
                  color.rgb = MixFog(color.rgb, inputData.fogCoord);
               #endif

            #else // unlit
               #ifdef _DBUFFER
                  ApplyDecalToSurfaceData(IN.pos, surface, inputData);
               #endif
               half4 color = UniversalFragmentUnlit(inputData, l.Albedo, l.Alpha);
               #if !DISABLEFOG
                  color.rgb = MixFog(color.rgb, inputData.fogCoord);
               #endif
            #endif
            ChainFinalColorForward(l, d, color);

            outColor = color;

            #ifdef _WRITE_RENDERING_LAYERS
                uint renderingLayers = GetMeshRenderingLayer();
                outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
            #endif

         }

         ENDHLSL

      }


      
      
      
      
        Pass
        {
            Name "Meta"
            Tags 
            { 
                "LightMode" = "Meta"
            }

            Cull Off
            

            	ZWrite On


            HLSLPROGRAM

               #pragma vertex Vert
   #pragma fragment Frag

            #if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES30)) 
            #pragma target 3.0
#else
            #pragma target 4.5
#endif

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
        
            #define SHADERPASS SHADERPASS_META
            #define _PASSMETA 1


            
	#include "Flow.cginc"


	#pragma shader_feature_local _ _STOCHASTIC_ON
	#pragma shader_feature_local _FOAM_OFF _FOAM_ALPHA _FOAM_CUTOUT


	#pragma shader_feature_local _ _FACETED_ON


	#pragma shader_feature_local _ALPHA_OFF _ALPHA_VERTICAL _ALPHA_DEPTH _ALPHA_MARCH_FLUID _ALPHA_MARCH_FLUID_AND_DEPTH


    #pragma shader_feature_local DISABLEFOG    


   #define _URP 1

   #define _ALPHABLEND_ON 1
#define _ALPHABLEND_ON 1
#define _SURFACE_TYPE_TRANSPARENT 1
#define _GRABPASSUSED 1
#define REQUIRE_OPAQUE_TEXTURE
#define REQUIRE_DEPTH_TEXTURE



            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

                  #undef WorldNormalVector
      #define WorldNormalVector(data, normal) mul(normal, data.TBNMatrix)
      
      #define UnityObjectToWorldNormal(normal) mul(GetObjectToWorldMatrix(), normal)

      #define _WorldSpaceLightPos0 _MainLightPosition
      
      #define UNITY_DECLARE_TEX2D(name) TEXTURE2D(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2D_NOSAMPLER(name) TEXTURE2D(name);
      #define UNITY_DECLARE_TEX2DARRAY(name) TEXTURE2D_ARRAY(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(name) TEXTURE2D_ARRAY(name);

      #define UNITY_SAMPLE_TEX2DARRAY(tex,coord)            SAMPLE_TEXTURE2D_ARRAY(tex, sampler##tex, coord.xy, coord.z)
      #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##tex, coord.xy, coord.z, lod)
      #define UNITY_SAMPLE_TEX2D(tex, coord)                SAMPLE_TEXTURE2D(tex, sampler##tex, coord)
      #define UNITY_SAMPLE_TEX2D_SAMPLER(tex, samp, coord)  SAMPLE_TEXTURE2D(tex, sampler##samp, coord)

      #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod)   SAMPLE_TEXTURE2D_LOD(tex, sampler_##tex, coord, lod)
      #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) SAMPLE_TEXTURE2D_LOD (tex, sampler##samplertex,coord, lod)
     
      #if defined(UNITY_COMPILER_HLSL)
         #define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
      #else
         #define UNITY_INITIALIZE_OUTPUT(type,name)
      #endif

      #define sampler2D_float sampler2D
      #define sampler2D_half sampler2D

      

      // data across stages, stripped like the above.
      struct VertexToPixel
      {
         float4 pos : SV_POSITION;
         float3 worldPos : TEXCOORD0;
         float3 worldNormal : TEXCOORD1;
         float4 worldTangent : TEXCOORD2;
          float4 texcoord0 : TEXCOORD3;
         // float4 texcoord1 : TEXCOORD4;
         // float4 texcoord2 : TEXCOORD5;

         // #if %TEXCOORD3REQUIREKEY%
         // float4 texcoord3 : TEXCOORD6;
         // #endif

         // #if %SCREENPOSREQUIREKEY%
          float4 screenPos : TEXCOORD7;
         // #endif

         // #if %VERTEXCOLORREQUIREKEY%
          half4 vertexColor : COLOR;
         // #endif

         #if defined(LIGHTMAP_ON)
            float2 lightmapUV : TEXCOORD8;
         #endif
         #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV : TEXCOORD9;
         #endif
         #if !defined(LIGHTMAP_ON)
            float4 probeOcclusion : TEXCOORD8;
            float3 sh : TEXCOORD10;
         #endif

         #if defined(VARYINGS_NEED_FOG_AND_VERTEX_LIGHT)
            float4 fogFactorAndVertexLight : TEXCOORD11;
         #endif

         #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
           float4 shadowCoord : TEXCOORD12;
         #endif

         // #if %EXTRAV2F0REQUIREKEY%
          float4 extraV2F0 : TEXCOORD13;
         // #endif

         // #if %EXTRAV2F1REQUIREKEY%
          float4 extraV2F1 : TEXCOORD14;
         // #endif

         // #if %EXTRAV2F2REQUIREKEY%
         // float4 extraV2F2 : TEXCOORD15;
         // #endif

         // #if %EXTRAV2F3REQUIREKEY%
         // float4 extraV2F3 : TEXCOORD16;
         // #endif

         // #if %EXTRAV2F4REQUIREKEY%
         // float4 extraV2F4 : TEXCOORD17;
         // #endif

         // #if %EXTRAV2F5REQUIREKEY%
         // float4 extraV2F5 : TEXCOORD18;
         // #endif

         // #if %EXTRAV2F6REQUIREKEY%
         // float4 extraV2F6 : TEXCOORD19;
         // #endif

         // #if %EXTRAV2F7REQUIREKEY%
         // float4 extraV2F7 : TEXCOORD20;
         // #endif

         #if UNITY_ANY_INSTANCING_ENABLED
         uint instanceID : CUSTOM_INSTANCE_ID;
         #endif
         #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
         uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
         #endif
         #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
         uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
         #endif
         #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
         FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
         #endif

         #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
            float4 previousPositionCS : TEXCOORD21; // Contain previous transform position (in case of skinning for example)
            float4 positionCS : TEXCOORD22;
         #endif
      };


            
            
            // data describing the user output of a pixel
            struct Surface
            {
               half3 Albedo;
               half Height;
               half3 Normal;
               half Smoothness;
               half3 Emission;
               half Metallic;
               half3 Specular;
               half Occlusion;
               half SpecularPower; // for simple lighting
               half Alpha;
               float outputDepth; // if written, SV_Depth semantic is used. ShaderData.clipPos.z is unused value
               // HDRP Only
               half SpecularOcclusion;
               half SubsurfaceMask;
               half Thickness;
               half CoatMask;
               half CoatSmoothness;
               half Anisotropy;
               half IridescenceMask;
               half IridescenceThickness;
               int DiffusionProfileHash;
               float SpecularAAThreshold;
               float SpecularAAScreenSpaceVariance;
               // requires _OVERRIDE_BAKEDGI to be defined, but is mapped in all pipelines
               float3 DiffuseGI;
               float3 BackDiffuseGI;
               float3 SpecularGI;
               float ior;
               float3 transmittanceColor;
               float atDistance;
               float transmittanceMask;
               // requires _OVERRIDE_SHADOWMASK to be defines
               float4 ShadowMask;

               // for decals
               float NormalAlpha;
               float MAOSAlpha;


            };

            // Data the user declares in blackboard blocks
            struct Blackboard
            {
                
	float  groundHeight;
	float  surfaceHeight;
	float3 surfaceNormal;

	Fluid fluid;


	float foam;


                float blackboardDummyData;
            };

            // data the user might need, this will grow to be big. But easy to strip
            struct ShaderData
            {
               float4 clipPos; // SV_POSITION
               float3 localSpacePosition;
               float3 localSpaceNormal;
               float3 localSpaceTangent;
        
               float3 worldSpacePosition;
               float3 worldSpaceNormal;
               float3 worldSpaceTangent;
               float tangentSign;

               float3 worldSpaceViewDir;
               float3 tangentSpaceViewDir;

               float4 texcoord0;
               float4 texcoord1;
               float4 texcoord2;
               float4 texcoord3;

               float2 screenUV;
               float4 screenPos;

               float4 vertexColor;
               bool isFrontFace;

               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;

               float3x3 TBNMatrix;
               Blackboard blackboard;
            };

            struct VertexData
            {
               #if SHADER_TARGET > 30
               // uint vertexID : SV_VertexID;
               #endif
               float4 vertex : POSITION;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;

               // optimize out mesh coords when not in use by user or lighting system
               #if _URP && (_USINGTEXCOORD1 || _PASSMETA || _PASSFORWARD || _PASSGBUFFER)
                  float4 texcoord1 : TEXCOORD1;
               #endif

               #if _URP && (_USINGTEXCOORD2 || _PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && defined(DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif

               #if _STANDARD && (_USINGTEXCOORD1 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER || _PASSFORWARDADD) && LIGHTMAP_ON)))
                  float4 texcoord1 : TEXCOORD1;
               #endif
               #if _STANDARD && (_USINGTEXCOORD2 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif


               #if _HDRP
                  float4 texcoord1 : TEXCOORD1;
                  float4 texcoord2 : TEXCOORD2;
               #endif

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessVertex 
            {
               float4 vertex : INTERNALTESSPOS;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;
               float4 texcoord1 : TEXCOORD1;
               float4 texcoord2 : TEXCOORD2;

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               // #if %EXTRAV2F0REQUIREKEY%
                float4 extraV2F0 : TEXCOORD5;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                float4 extraV2F1 : TEXCOORD6;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // float4 extraV2F2 : TEXCOORD7;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // float4 extraV2F3 : TEXCOORD8;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // float4 extraV2F4 : TEXCOORD9;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // float4 extraV2F5 : TEXCOORD10;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // float4 extraV2F6 : TEXCOORD11;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // float4 extraV2F7 : TEXCOORD12;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD13; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity : TEXCOORD14;
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ExtraV2F
            {
               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;
               Blackboard blackboard;
               float4 time;
            };


            float3 WorldToTangentSpace(ShaderData d, float3 normal)
            {
               return mul(d.TBNMatrix, normal);
            }

            float3 TangentToWorldSpace(ShaderData d, float3 normal)
            {
               return mul(normal, d.TBNMatrix);
            }

            // in this case, make standard more like SRPs, because we can't fix
            // unity_WorldToObject in HDRP, since it already does macro-fu there

            #if _STANDARD
               float3 TransformWorldToObject(float3 p) { return mul(unity_WorldToObject, float4(p, 1)); };
               float3 TransformObjectToWorld(float3 p) { return mul(unity_ObjectToWorld, float4(p, 1)); };
               float4 TransformWorldToObject(float4 p) { return mul(unity_WorldToObject, p); };
               float4 TransformObjectToWorld(float4 p) { return mul(unity_ObjectToWorld, p); };
               float4x4 GetWorldToObjectMatrix() { return unity_WorldToObject; }
               float4x4 GetObjectToWorldMatrix() { return unity_ObjectToWorld; }
               #if (defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (SHADER_TARGET_SURFACE_ANALYSIS && !SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod) tex.SampleLevel (sampler##tex,coord, lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) tex.SampleLevel (sampler##samplertex,coord, lod)
              #else
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex2D (tex,coord,0,lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord,lod) tex2D (tex,coord,0,lod)
              #endif

               #undef GetWorldToObjectMatrix()

               #define GetWorldToObjectMatrix()   unity_WorldToObject


            #endif

            float3 GetCameraWorldPosition()
            {
               #if _HDRP
                  return GetCameraRelativePositionWS(_WorldSpaceCameraPos);
               #else
                  return _WorldSpaceCameraPos;
               #endif
            }

            #if _GRABPASSUSED
               #if _STANDARD
                  TEXTURE2D(_Grab);
                  SAMPLER(sampler__Grab);
               #endif

               half3 GetSceneColor(float2 uv)
               {
                  #if _STANDARD
                     return SAMPLE_TEXTURE2D(_Grab, sampler__Grab, uv).rgb;
                  #else
                     return SHADERGRAPH_SAMPLE_SCENE_COLOR(uv);
                  #endif
               }
            #endif


      
            #if _STANDARD
               UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
               float GetSceneDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv)); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv)); } 
            #else
               float GetSceneDepth(float2 uv) { return SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv), _ZBufferParams); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv), _ZBufferParams); } 
            #endif

            float3 GetWorldPositionFromDepthBuffer(float2 uv, float3 worldSpaceViewDir)
            {
               float eye = GetLinearEyeDepth(uv);
               float3 camView = mul((float3x3)GetObjectToWorldMatrix(), transpose(mul(GetWorldToObjectMatrix(), UNITY_MATRIX_I_V)) [2].xyz);

               float dt = dot(worldSpaceViewDir, camView);
               float3 div = worldSpaceViewDir/dt;
               float3 wpos = (eye * div) + GetCameraWorldPosition();
               return wpos;
            }
			
			float3 GetSceneWorldPosition(float2 screenUV, float sceneDepth)
			{
				#if _STANDARD
					float4 clipPos  = float4(screenUV * 2.0f - 1.0f, 0.0f, 1.0f);
					float4 viewPos  = mul(unity_CameraInvProjection, clipPos);
					float3 worldDir = mul((float3x3)UNITY_MATRIX_I_V, viewPos);
					
					return _WorldSpaceCameraPos + worldDir * LinearEyeDepth(sceneDepth);
				#else
					float4 clipPos = float4(screenUV * 2.0 - 1.0, sceneDepth, 1.0);
					
					#if UNITY_UV_STARTS_AT_TOP
						clipPos.y = -clipPos.y;
					#endif
					
					float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
					
					worldPos.xyz /= worldPos.w;
					
					#if _HDRP
						worldPos.xyz = GetAbsolutePositionWS(worldPos.xyz);
					#endif
					
					return worldPos.xyz;
				#endif
			}
			
			float GetSceneWorldDistance(float2 screenUV, float sceneDepth)
			{
				return distance(_WorldSpaceCameraPos, GetSceneWorldPosition(screenUV, sceneDepth));
			}

            #if _HDRP
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return GetAbsolutePositionWS(TransformObjectToWorld(pos));
            }
            #else
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return TransformObjectToWorld(pos);
            }
            #endif

            #if _STANDARD
               UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  float4 depthNorms = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture, uv);
                  float3 norms = DecodeViewNormalStereo(depthNorms);
                  norms = mul((float3x3)GetWorldToViewMatrix(), norms) * 0.5 + 0.5;
                  return norms;
               }
            #elif _HDRP && !_DECALSHADER
               
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  NormalData nd;
                  DecodeFromNormalBuffer(_ScreenSize.xy * uv, nd);
                  return nd.normalWS;
               }
            #elif _URP
               #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
               #endif

               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                     return SampleSceneNormals(uv);
                  #else
                     float3 wpos = GetWorldPositionFromDepthBuffer(uv, worldSpaceViewDir);
                     return normalize(-cross(ddx(wpos), ddy(wpos))) * 0.5 + 0.5;
                  #endif

                }
             #endif

             #if _HDRP

               half3 UnpackNormalmapRGorAG(half4 packednormal)
               {
                     // This do the trick
                  packednormal.x *= packednormal.w;

                  half3 normal;
                  normal.xy = packednormal.xy * 2 - 1;
                  normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                  return normal;
               }
               half3 UnpackNormal(half4 packednormal)
               {
                  #if defined(UNITY_NO_DXT5nm)
                     return packednormal.xyz * 2 - 1;
                  #else
                     return UnpackNormalmapRGorAG(packednormal);
                  #endif
               }
            #endif
            #if _HDRP || _URP

               half3 UnpackScaleNormal(half4 packednormal, half scale)
               {
                 #ifndef UNITY_NO_DXT5nm
                   // Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
                   // Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
                   packednormal.x *= packednormal.w;
                 #endif
                   half3 normal;
                   normal.xy = (packednormal.xy * 2 - 1) * scale;
                   normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                   return normal;
               }	

             #endif


            void GetSun(out float3 lightDir, out float3 color)
            {
               lightDir = float3(0.5, 0.5, 0);
               color = 1;
               #if _HDRP
                  if (_DirectionalLightCount > 0)
                  {
                     DirectionalLightData light = _DirectionalLightDatas[0];
                     lightDir = -light.forward.xyz;
                     color = light.color;
                  }
               #elif _STANDARD
			         lightDir = normalize(_WorldSpaceLightPos0.xyz);
                  color = _LightColor0.rgb;
               #elif _URP
	               Light light = GetMainLight();
	               lightDir = light.direction;
	               color = light.color;
               #endif
            }


            
            CBUFFER_START(UnityPerMaterial)

               
	float    _FlowDensity;
	float2   _FlowSeparationXZ;
	float    _FlowSimulationHeight;
	float    _FlowCameraHeight;
	float2   _FlowCountXZ;
	float4   _FlowCoordU000;
	float4   _FlowCoord0V00;
	float    _FlowSpeed;
	float4x4 _FlowMatrix;

	float _Sink;


	float _Emission;
	
	float _WaveStrengthMin;
	float _WaveStrengthMax;
	float _WaveStrengthScale;

	float _Tiling;
	float _AnimationSpeed;
	float _FoamBrightness;


	half _FlatShadingBlend;


	float _RangeMax;
	float _AlphaStep;
	int _AlphaMaxSteps;
	float _AlphaDepthScale;





            CBUFFER_END

            

            

            
	TEXTURE2D(_FlowDataA);
	SAMPLER(sampler_FlowDataA);
	TEXTURE2D(_FlowDataB);
	SAMPLER(sampler_FlowDataB);
	TEXTURE2D(_FlowDataC);
	SAMPLER(sampler_FlowDataC);
	TEXTURE2D(_FlowDataD);
	SAMPLER(sampler_FlowDataD);
	TEXTURE2D(_FlowDataE);
	SAMPLER(sampler_FlowDataE);
	TEXTURE2D(_FlowDataF);
	SAMPLER(sampler_FlowDataF);

	float4 SGT_O2W(float4 v)
	{
		v = mul(GetObjectToWorldMatrix(), v);
		#if _HDRP
			v.xyz = GetAbsolutePositionWS(v.xyz);
		#endif
		return v;
	}

	float4 SGT_W2O(float4 v)
	{
		#if _HDRP
			v.xyz = GetCameraRelativePositionWS(v.xyz);
		#endif
		return mul(GetWorldToObjectMatrix(), v);
	}

	float4 SGT_O2V(float4 v)
	{
		#if _STANDARD
			return float4(UnityObjectToViewPos(v.xyz), 1.0f);
		#else
			return float4(TransformWorldToView(TransformObjectToWorld(v.xyz)), 1.0f);
		#endif
	}

	float GetFluidHeight(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return groundHeight + fluidDepth;
	}

	float2 GetHeightAndDepth(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return float2(groundHeight, fluidDepth);
	}

	Column GetColumn(float2 uv)
	{
		return DecodeColumn(SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0), SAMPLE_TEXTURE2D_LOD(_FlowDataB, sampler_FlowDataB, uv, 0));
	}

	Fluid GetColumnFluid(float2 uv)
	{
		float4 c = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0);
		float4 d = SAMPLE_TEXTURE2D_LOD(_FlowDataD, sampler_FlowDataD, uv, 0);
		float4 e = SAMPLE_TEXTURE2D_LOD(_FlowDataE, sampler_FlowDataE, uv, 0);
		float4 f = SAMPLE_TEXTURE2D_LOD(_FlowDataF, sampler_FlowDataF, uv, 0);
		return DecodeFluid(c, d, e, f);
	}

	bool InsideFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y > columnHAD.x && wpos.y < (columnHAD.x + columnHAD.y);
	}

	bool UnderFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y < (columnHAD.x + columnHAD.y);
	}

	float RayMarchInside(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (InsideFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	float RayMarchUnder(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (UnderFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	void Ext_ModifyVertex0 (inout VertexData v, inout ExtraV2F e)
	{
		float4 wpos        = SGT_O2W(v.vertex);
		float2 columnPixel = mul(_FlowMatrix, float4(wpos.xyz, 1.0f)).xy;
		float2 columnCoord = SnapCoordFromPixel(round(columnPixel), _FlowCountXZ);

		Column column0 = GetColumn(columnCoord);
		Column columnL = GetColumn(columnCoord - _FlowCoordU000.xy);
		Column columnR = GetColumn(columnCoord + _FlowCoordU000.xy);
		Column columnB = GetColumn(columnCoord - _FlowCoord0V00.xy);
		Column columnT = GetColumn(columnCoord + _FlowCoord0V00.xy);

		Fluid fluid0 = GetColumnFluid(columnCoord);
		Fluid fluidL = GetColumnFluid(columnCoord - _FlowCoordU000.xy);
		Fluid fluidR = GetColumnFluid(columnCoord + _FlowCoordU000.xy);
		Fluid fluidB = GetColumnFluid(columnCoord - _FlowCoord0V00.xy);
		Fluid fluidT = GetColumnFluid(columnCoord + _FlowCoord0V00.xy);

		float ww = fluidL.Depth + fluidR.Depth + fluidB.Depth + fluidT.Depth + 0.001f;
		float wL = fluidL.Depth / ww;
		float wR = fluidR.Depth / ww;
		float wB = fluidB.Depth / ww;
		float wT = fluidT.Depth / ww;
		float w0 = saturate(fluid0.Depth * 10);

		float hL = columnL.GroundHeight + fluidL.Depth;
		float hR = columnR.GroundHeight + fluidR.Depth;
		float hB = columnB.GroundHeight + fluidB.Depth;
		float hT = columnT.GroundHeight + fluidT.Depth;
		float hh = hL * wL + hR * wR + hB * wB + hT * wT;
		float h0 = column0.GroundHeight + fluid0.Depth;

		hh = lerp(hh, h0 - _Sink, saturate(0.01f / ww)); // Prevent skirts going down too far

		Fluid fluid = fluid0;

		fluid.Depth = lerp(fluidL.Depth * wL + fluidR.Depth * wR + fluidB.Depth * wB + fluidT.Depth * wT, fluid.Depth, w0);
		fluid.RGBA  = lerp(fluidL.RGBA  * wL + fluidR.RGBA  * wR + fluidB.RGBA  * wB + fluidT.RGBA  * wT, fluid.RGBA , w0);
		fluid.ESMV  = lerp(fluidL.ESMV  * wL + fluidR.ESMV  * wR + fluidB.ESMV  * wB + fluidT.ESMV  * wT, fluid.ESMV , w0);
		fluid.F123  = lerp(fluidL.F123  * wL + fluidR.F123  * wR + fluidB.F123  * wB + fluidT.F123  * wT, fluid.F123 , w0);

		e.blackboard.groundHeight  = column0.GroundHeight;
		e.blackboard.surfaceHeight = lerp(hh, h0, w0);
		e.blackboard.surfaceNormal = normalize(float3((hL - hR) / _FlowSeparationXZ.x, 2.0f, (hB - hT) / _FlowSeparationXZ.y));
		e.blackboard.fluid         = fluid;
	}


	TEXTURE2D(_NoiseMap);
	SAMPLER(sampler_NoiseMap);

	TEXTURE2D(_NormalFoamMap);
	SAMPLER(sampler_NormalFoamMap);

	void Ext_ModifyVertex3 (inout VertexData v, inout ExtraV2F e)
	{
		v.vertex.y = lerp(e.blackboard.groundHeight, e.blackboard.surfaceHeight, v.texcoord0.w) - _FlowSimulationHeight;

		v.normal = lerp(e.blackboard.surfaceNormal, v.normal, v.texcoord0.z);

		v.vertexColor = e.blackboard.fluid.RGBA;

		e.extraV2F0.xyz = SGT_O2V(v.vertex).xyz;
		e.extraV2F0.w   = e.blackboard.fluid.Depth;

		e.extraV2F1.xyz = e.blackboard.fluid.ESMV.xyz;
		e.extraV2F1.w = e.blackboard.fluid.F123.x;
	}

	float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float tiling, float time, float phaseOffset)
	{
		float progress = frac(time + phaseOffset);
		float3 uvw;
		uvw.xy = uv - flowVector * (progress - 0.5f);
		uvw.xy *= tiling;
		uvw.xy += phaseOffset;
		uvw.xy += (time - progress) * jump;
		uvw.z = 1 - abs(1 - 2 * progress);
		return uvw;
	}

	float3 CombineNormals(float3 n1, float3 n2)
	{
		return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
	}

	float3 UnpackNormalAndScale(float2 xy, float scale)
	{
		xy = xy * 2.0f - 1.0f; xy *= scale; return float3(xy, sqrt(1.0 - saturate(dot(xy, xy))));
	}

	float4 SampleStochastic(float2 uv, float noise)
	{
		float cur_height = uv.y + noise;
		float this_index = floor(cur_height);
		float next_index = this_index + 1.0f;

		float2 uvA     = uv + sin(float2(1.0f, 2.0f) * this_index);
		float2 uvB     = uv + sin(float2(1.0f, 2.0f) * next_index);
		float2 gradX   = ddx(uv);
		float2 gradY   = ddy(uv);

		float4 sampleA = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvA, gradX, gradY);
		float4 sampleB = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvB, gradX, gradY);

		return lerp(sampleA, sampleB, cur_height - this_index);
	}

	void Ext_SurfaceFunction3 (inout Surface o, inout ShaderData d)
	{
		d.blackboard.fluid.RGBA     = d.vertexColor;
		d.blackboard.fluid.Depth    = d.extraV2F0.w;
		d.blackboard.fluid.ESMV.xyz = d.extraV2F1.xyz;
		d.blackboard.fluid.F123.x   = d.extraV2F1.w;

		if (d.blackboard.fluid.Depth < 0.01f)
		{
			discard;
		}

		float2 uv          = d.worldSpacePosition.xz;
		float2 columnPixel = mul(_FlowMatrix, float4(d.worldSpacePosition, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		Column column      = GetColumn(columnCoord);

		float  time = _Time.y * _AnimationSpeed;
		float2 jump = float2(0.13f, 0.17f);
		float2 fvec = (column.Outflow.yw - column.Outflow.xz) * _FlowSeparationXZ * _FlowSpeed;
		float  fmag = log10(1.0f + length(fvec)) * 0.1f;

		float3 flowA = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.0f);
		float3 flowB = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.5f);

		#if _STOCHASTIC_ON
			float  noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 0.0025f).a * 4.0f;
			float4 nfmA  = SampleStochastic(flowA, noise);
			float4 nfmB  = SampleStochastic(flowB, noise);
		#else
			float4 nfmA = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowA.xy);
			float4 nfmB = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowB.xy);
		#endif

		// Water normals
		float  normalStr    = lerp(_WaveStrengthMin, _WaveStrengthMax, saturate(_WaveStrengthScale * fmag));
		float3 waterNormalA = UnpackNormalAndScale(nfmA.xy, flowA.z * normalStr);
		float3 waterNormalB = UnpackNormalAndScale(nfmB.xy, flowB.z * normalStr);
		float3 waterNormals = CombineNormals(waterNormalA, waterNormalB);

		o.Normal   = lerp(waterNormals, o.Normal, d.texcoord0.z);
		o.Albedo   = d.blackboard.fluid.RGBA.xyz;
		o.Emission = d.blackboard.fluid.ESMV.x * _Emission * o.Albedo;

		#if _FOAM_OFF
			d.blackboard.foam = 0.0f;
		#else
			d.blackboard.foam = d.blackboard.fluid.F123.x * (1.0f - d.texcoord0.z);

			#if _FOAM_ALPHA
				float3 foamAlbedo = (nfmA.w * flowA.z + nfmB.w * flowB.z) * _FoamBrightness;
			#elif _FOAM_CUTOUT
				float3 foamAlbedo = _FoamBrightness;
				float  foamRamp   = nfmA.w * flowA.z + nfmB.w * flowB.z;
				d.blackboard.foam = saturate((d.blackboard.foam - foamRamp) * 10.0f);
			#endif

			float3 foamNormal = float3(0.0f, 0.0f, 1.0f);

			o.Albedo = lerp(o.Albedo, foamAlbedo, d.blackboard.foam);
			o.Normal = lerp(o.Normal, foamNormal, d.blackboard.foam);
		#endif

		o.Smoothness = d.blackboard.fluid.ESMV.y;
		o.Metallic   = d.blackboard.fluid.ESMV.z;
	}


	void Ext_SurfaceFunction4 (inout Surface o, inout ShaderData d)
	{
	#if _FACETED_ON
		// lets just affect the TBN data, so we flat shade the original polygons, not the normal map
		float3 dx = ddx(d.worldSpacePosition);
		float3 dy = ddy(d.worldSpacePosition);
		float3 worldNormal = normalize(cross(dy, dx));
		worldNormal = lerp(d.worldSpaceNormal, worldNormal, _FlatShadingBlend);
		d.worldSpaceNormal = worldNormal;
		d.TBNMatrix[2] = worldNormal;
	#endif
	}


	void Ext_SurfaceFunction5 (inout Surface o, ShaderData d)
	{
		#if _ALPHA_OFF
		#else
			float3 position  = d.worldSpacePosition;
			float3 step      = d.worldSpacePosition - _WorldSpaceCameraPos;
			float2 bentScrUV = d.screenUV + o.Normal.xy * 0.05f * log(1.0f + d.blackboard.fluid.Depth * 1.0f);
			float  distMax   = max(_RangeMax * 0.001f, _RangeMax * (1.0f - d.blackboard.fluid.RGBA.w));
			float  distRange = distMax;
			float  vertDist  = length(d.extraV2F0.xyz);
			float  camtDist  = GetLinearEyeDepth(bentScrUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);

			if (camtDist < vertDist)
			{
				camtDist = GetLinearEyeDepth(d.screenUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);
			}

			float diffDist = max(0.0f, camtDist - vertDist);

			// The depth buffer becomes unusable at certain ranges, so fade it out
			diffDist += max(0.0f, camtDist * _AlphaDepthScale - _RangeMax);
			distRange = min(distRange, diffDist);

			#if _ALPHA_VERTICAL
				float dist = d.blackboard.fluid.Depth;
			#elif _ALPHA_DEPTH
				float dist = diffDist;
			#elif _ALPHA_MARCH_FLUID
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distMax);
			#elif _ALPHA_MARCH_FLUID_AND_DEPTH
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distRange);
			#endif

			// Set opacity based on distance through fluid relative to the maximum distance, and make sure high opacity fluids become solid
			float opacity = saturate(dist / distMax + d.blackboard.foam + pow(d.blackboard.fluid.RGBA.w, 10.0f));

			o.Albedo    = lerp(0.0f, o.Albedo, opacity);
			o.Emission  = lerp(GetSceneColor(bentScrUV), o.Emission, opacity);
			//o.Emission += o.Albedo * d.blackboard.fluid.ESMV.x * opacity;
		#endif
	}





        
            void ChainSurfaceFunction(inout Surface l, inout ShaderData d)
            {
                 // Ext_SurfaceFunction0(l, d);
                 // Ext_SurfaceFunction1(l, d);
                 // Ext_SurfaceFunction2(l, d);
                  Ext_SurfaceFunction3(l, d);
                  Ext_SurfaceFunction4(l, d);
                  Ext_SurfaceFunction5(l, d);
                 // Ext_SurfaceFunction6(l, d);
                 // Ext_SurfaceFunction7(l, d);
                 // Ext_SurfaceFunction8(l, d);
                 // Ext_SurfaceFunction9(l, d);
		           // Ext_SurfaceFunction10(l, d);
                 // Ext_SurfaceFunction11(l, d);
                 // Ext_SurfaceFunction12(l, d);
                 // Ext_SurfaceFunction13(l, d);
                 // Ext_SurfaceFunction14(l, d);
                 // Ext_SurfaceFunction15(l, d);
                 // Ext_SurfaceFunction16(l, d);
                 // Ext_SurfaceFunction17(l, d);
                 // Ext_SurfaceFunction18(l, d);
		           // Ext_SurfaceFunction19(l, d);
                 // Ext_SurfaceFunction20(l, d);
                 // Ext_SurfaceFunction21(l, d);
                 // Ext_SurfaceFunction22(l, d);
                 // Ext_SurfaceFunction23(l, d);
                 // Ext_SurfaceFunction24(l, d);
                 // Ext_SurfaceFunction25(l, d);
                 // Ext_SurfaceFunction26(l, d);
                 // Ext_SurfaceFunction27(l, d);
                 // Ext_SurfaceFunction28(l, d);
		           // Ext_SurfaceFunction29(l, d);
            }

#if !_DECALSHADER

            void ChainModifyVertex(inout VertexData v, inout VertexToPixel v2p, float4 time)
            {
                 ExtraV2F d;
                 
                 ZERO_INITIALIZE(ExtraV2F, d);
                 ZERO_INITIALIZE(Blackboard, d.blackboard);
                 // due to motion vectors in HDRP, we need to use the last
                 // time in certain spots. So if you are going to use _Time to adjust vertices,
                 // you need to use this time or motion vectors will break. 
                 d.time = time;

                   Ext_ModifyVertex0(v, d);
                 // Ext_ModifyVertex1(v, d);
                 // Ext_ModifyVertex2(v, d);
                  Ext_ModifyVertex3(v, d);
                 // Ext_ModifyVertex4(v, d);
                 // Ext_ModifyVertex5(v, d);
                 // Ext_ModifyVertex6(v, d);
                 // Ext_ModifyVertex7(v, d);
                 // Ext_ModifyVertex8(v, d);
                 // Ext_ModifyVertex9(v, d);
                 // Ext_ModifyVertex10(v, d);
                 // Ext_ModifyVertex11(v, d);
                 // Ext_ModifyVertex12(v, d);
                 // Ext_ModifyVertex13(v, d);
                 // Ext_ModifyVertex14(v, d);
                 // Ext_ModifyVertex15(v, d);
                 // Ext_ModifyVertex16(v, d);
                 // Ext_ModifyVertex17(v, d);
                 // Ext_ModifyVertex18(v, d);
                 // Ext_ModifyVertex19(v, d);
                 // Ext_ModifyVertex20(v, d);
                 // Ext_ModifyVertex21(v, d);
                 // Ext_ModifyVertex22(v, d);
                 // Ext_ModifyVertex23(v, d);
                 // Ext_ModifyVertex24(v, d);
                 // Ext_ModifyVertex25(v, d);
                 // Ext_ModifyVertex26(v, d);
                 // Ext_ModifyVertex27(v, d);
                 // Ext_ModifyVertex28(v, d);
                 // Ext_ModifyVertex29(v, d);


                 // #if %EXTRAV2F0REQUIREKEY%
                  v2p.extraV2F0 = d.extraV2F0;
                 // #endif

                 // #if %EXTRAV2F1REQUIREKEY%
                  v2p.extraV2F1 = d.extraV2F1;
                 // #endif

                 // #if %EXTRAV2F2REQUIREKEY%
                 // v2p.extraV2F2 = d.extraV2F2;
                 // #endif

                 // #if %EXTRAV2F3REQUIREKEY%
                 // v2p.extraV2F3 = d.extraV2F3;
                 // #endif

                 // #if %EXTRAV2F4REQUIREKEY%
                 // v2p.extraV2F4 = d.extraV2F4;
                 // #endif

                 // #if %EXTRAV2F5REQUIREKEY%
                 // v2p.extraV2F5 = d.extraV2F5;
                 // #endif

                 // #if %EXTRAV2F6REQUIREKEY%
                 // v2p.extraV2F6 = d.extraV2F6;
                 // #endif

                 // #if %EXTRAV2F7REQUIREKEY%
                 // v2p.extraV2F7 = d.extraV2F7;
                 // #endif
            }

            void ChainModifyTessellatedVertex(inout VertexData v, inout VertexToPixel v2p)
            {
               ExtraV2F d;
               ZERO_INITIALIZE(ExtraV2F, d);
               ZERO_INITIALIZE(Blackboard, d.blackboard);

               // #if %EXTRAV2F0REQUIREKEY%
                d.extraV2F0 = v2p.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                d.extraV2F1 = v2p.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // d.extraV2F2 = v2p.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // d.extraV2F3 = v2p.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // d.extraV2F4 = v2p.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // d.extraV2F5 = v2p.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // d.extraV2F6 = v2p.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // d.extraV2F7 = v2p.extraV2F7;
               // #endif


               // Ext_ModifyTessellatedVertex0(v, d);
               // Ext_ModifyTessellatedVertex1(v, d);
               // Ext_ModifyTessellatedVertex2(v, d);
               // Ext_ModifyTessellatedVertex3(v, d);
               // Ext_ModifyTessellatedVertex4(v, d);
               // Ext_ModifyTessellatedVertex5(v, d);
               // Ext_ModifyTessellatedVertex6(v, d);
               // Ext_ModifyTessellatedVertex7(v, d);
               // Ext_ModifyTessellatedVertex8(v, d);
               // Ext_ModifyTessellatedVertex9(v, d);
               // Ext_ModifyTessellatedVertex10(v, d);
               // Ext_ModifyTessellatedVertex11(v, d);
               // Ext_ModifyTessellatedVertex12(v, d);
               // Ext_ModifyTessellatedVertex13(v, d);
               // Ext_ModifyTessellatedVertex14(v, d);
               // Ext_ModifyTessellatedVertex15(v, d);
               // Ext_ModifyTessellatedVertex16(v, d);
               // Ext_ModifyTessellatedVertex17(v, d);
               // Ext_ModifyTessellatedVertex18(v, d);
               // Ext_ModifyTessellatedVertex19(v, d);
               // Ext_ModifyTessellatedVertex20(v, d);
               // Ext_ModifyTessellatedVertex21(v, d);
               // Ext_ModifyTessellatedVertex22(v, d);
               // Ext_ModifyTessellatedVertex23(v, d);
               // Ext_ModifyTessellatedVertex24(v, d);
               // Ext_ModifyTessellatedVertex25(v, d);
               // Ext_ModifyTessellatedVertex26(v, d);
               // Ext_ModifyTessellatedVertex27(v, d);
               // Ext_ModifyTessellatedVertex28(v, d);
               // Ext_ModifyTessellatedVertex29(v, d);

               // #if %EXTRAV2F0REQUIREKEY%
                v2p.extraV2F0 = d.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                v2p.extraV2F1 = d.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // v2p.extraV2F2 = d.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // v2p.extraV2F3 = d.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // v2p.extraV2F4 = d.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // v2p.extraV2F5 = d.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // v2p.extraV2F6 = d.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // v2p.extraV2F7 = d.extraV2F7;
               // #endif
            }

            void ChainFinalColorForward(inout Surface l, inout ShaderData d, inout half4 color)
            {
               //   Ext_FinalColorForward0(l, d, color);
               //   Ext_FinalColorForward1(l, d, color);
               //   Ext_FinalColorForward2(l, d, color);
               //   Ext_FinalColorForward3(l, d, color);
               //   Ext_FinalColorForward4(l, d, color);
               //   Ext_FinalColorForward5(l, d, color);
               //   Ext_FinalColorForward6(l, d, color);
               //   Ext_FinalColorForward7(l, d, color);
               //   Ext_FinalColorForward8(l, d, color);
               //   Ext_FinalColorForward9(l, d, color);
               //  Ext_FinalColorForward10(l, d, color);
               //  Ext_FinalColorForward11(l, d, color);
               //  Ext_FinalColorForward12(l, d, color);
               //  Ext_FinalColorForward13(l, d, color);
               //  Ext_FinalColorForward14(l, d, color);
               //  Ext_FinalColorForward15(l, d, color);
               //  Ext_FinalColorForward16(l, d, color);
               //  Ext_FinalColorForward17(l, d, color);
               //  Ext_FinalColorForward18(l, d, color);
               //  Ext_FinalColorForward19(l, d, color);
               //  Ext_FinalColorForward20(l, d, color);
               //  Ext_FinalColorForward21(l, d, color);
               //  Ext_FinalColorForward22(l, d, color);
               //  Ext_FinalColorForward23(l, d, color);
               //  Ext_FinalColorForward24(l, d, color);
               //  Ext_FinalColorForward25(l, d, color);
               //  Ext_FinalColorForward26(l, d, color);
               //  Ext_FinalColorForward27(l, d, color);
               //  Ext_FinalColorForward28(l, d, color);
               //  Ext_FinalColorForward29(l, d, color);
            }

            void ChainFinalGBufferStandard(inout Surface s, inout ShaderData d, inout half4 GBuffer0, inout half4 GBuffer1, inout half4 GBuffer2, inout half4 outEmission, inout half4 outShadowMask)
            {
               //   Ext_FinalGBufferStandard0(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard1(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard2(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard3(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard4(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard5(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard6(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard7(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard8(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard9(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard10(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard11(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard12(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard13(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard14(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard15(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard16(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard17(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard18(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard19(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard20(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard21(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard22(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard23(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard24(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard25(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard26(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard27(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard28(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard29(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
            }
#endif


            


#if _DECALSHADER

        ShaderData CreateShaderData(SurfaceDescriptionInputs IN)
        {
            ShaderData d = (ShaderData)0;
            d.TBNMatrix = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
            d.worldSpaceNormal = IN.WorldSpaceNormal;
            d.worldSpaceTangent = IN.WorldSpaceTangent;

            d.worldSpacePosition = IN.WorldSpacePosition;
            d.texcoord0 = IN.uv0.xyxy;
            d.screenPos = IN.ScreenPosition;

            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - d.worldSpacePosition);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(d.worldSpacePosition), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(d.worldSpacePosition, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenUV = (IN.ScreenPosition.xy / max(0.01, IN.ScreenPosition.w));
            // #endif

            return d;
        }
#else

         ShaderData CreateShaderData(VertexToPixel i
                  #if NEED_FACING
                     , bool facing
                  #endif
         )
         {
            ShaderData d = (ShaderData)0;
            d.clipPos = i.pos;
            d.worldSpacePosition = i.worldPos;

            d.worldSpaceNormal = normalize(i.worldNormal);
            d.worldSpaceTangent.xyz = normalize(i.worldTangent.xyz);

            d.tangentSign = i.worldTangent.w * unity_WorldTransformParams.w;
            float3 bitangent = cross(d.worldSpaceTangent.xyz, d.worldSpaceNormal) * d.tangentSign;
           
            d.TBNMatrix = float3x3(d.worldSpaceTangent, -bitangent, d.worldSpaceNormal);
            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);
             d.texcoord0 = i.texcoord0;
            // d.texcoord1 = i.texcoord1;
            // d.texcoord2 = i.texcoord2;

            // #if %TEXCOORD3REQUIREKEY%
            // d.texcoord3 = i.texcoord3;
            // #endif

            // d.isFrontFace = facing;
            // #if %VERTEXCOLORREQUIREKEY%
             d.vertexColor = i.vertexColor;
            // #endif

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(i.worldPos), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(i.worldPos, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenPos = i.screenPos;
             d.screenUV = (i.screenPos.xy / i.screenPos.w);
            // #endif


            // #if %EXTRAV2F0REQUIREKEY%
             d.extraV2F0 = i.extraV2F0;
            // #endif

            // #if %EXTRAV2F1REQUIREKEY%
             d.extraV2F1 = i.extraV2F1;
            // #endif

            // #if %EXTRAV2F2REQUIREKEY%
            // d.extraV2F2 = i.extraV2F2;
            // #endif

            // #if %EXTRAV2F3REQUIREKEY%
            // d.extraV2F3 = i.extraV2F3;
            // #endif

            // #if %EXTRAV2F4REQUIREKEY%
            // d.extraV2F4 = i.extraV2F4;
            // #endif

            // #if %EXTRAV2F5REQUIREKEY%
            // d.extraV2F5 = i.extraV2F5;
            // #endif

            // #if %EXTRAV2F6REQUIREKEY%
            // d.extraV2F6 = i.extraV2F6;
            // #endif

            // #if %EXTRAV2F7REQUIREKEY%
            // d.extraV2F7 = i.extraV2F7;
            // #endif

            return d;
         }

#endif

            
         #if defined(_PASSSHADOW)
            float3 _LightDirection;
            float3 _LightPosition;
         #endif

         #if (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))

            #define GetWorldToViewMatrix()     _ViewMatrix
            #define UNITY_MATRIX_I_V   _InvViewMatrix
            #define GetViewToHClipMatrix()     OptimizeProjectionMatrix(_ProjMatrix)
            #define UNITY_MATRIX_I_P   _InvProjMatrix
            #define GetWorldToHClipMatrix()    _ViewProjMatrix
            #define UNITY_MATRIX_I_VP  _InvViewProjMatrix
            #define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
            #define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix
            #define UNITY_MATRIX_PREV_I_VP _PrevInvViewProjMatrix

            void MotionVectorPositionZBias(VertexToPixel input)
            {
                #if UNITY_REVERSED_Z
                input.pos.z -= unity_MotionVectorsParams.z * input.pos.w;
                #else
                input.pos.z += unity_MotionVectorsParams.z * input.pos.w;
                #endif
            }

        #endif

         // vertex shader
         VertexToPixel Vert (VertexData v)
         {
           VertexToPixel o = (VertexToPixel)0;

           UNITY_SETUP_INSTANCE_ID(v);
           UNITY_TRANSFER_INSTANCE_ID(v, o);
           UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            
           #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
             VertexData previousMesh = v;
           #endif
           #if !_TESSELLATION_ON
             ChainModifyVertex(v, o, _Time);
           #endif

            o.texcoord0 = v.texcoord0;
           // o.texcoord1 = v.texcoord1;
           // o.texcoord2 = v.texcoord2;

           // #if %TEXCOORD3REQUIREKEY%
           // o.texcoord3 = v.texcoord3;
           // #endif

           // #if %VERTEXCOLORREQUIREKEY%
            o.vertexColor = v.vertexColor;
           // #endif

           // This return the camera relative position (if enable)
           float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
           float3 normalWS = TransformObjectToWorldNormal(v.normal);
           float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
           
           VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
           o.worldPos = positionWS;
           o.worldNormal = normalWS;
           o.worldTangent = tangentWS;


          // For some very odd reason, in 2021.2, we can't use Unity's defines, but have to use our own..
          #if _PASSSHADOW
              #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                 float3 lightDirectionWS = normalize(_LightPosition - o.worldPos);
              #else
                 float3 lightDirectionWS = _LightDirection;
              #endif
              // Define shadow pass specific clip position for Universal
              o.pos = TransformWorldToHClip(ApplyShadowBias(o.worldPos, o.worldNormal, lightDirectionWS));
              #if UNITY_REVERSED_Z
                  o.pos.z = min(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #else
                  o.pos.z = max(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #endif
          #elif _PASSMETA
              o.pos = MetaVertexPosition(float4(v.vertex.xyz, 0), v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #else
              o.pos = TransformWorldToHClip(o.worldPos);
          #endif

          // #if %SCREENPOSREQUIREKEY%
           o.screenPos = ComputeScreenPos(o.pos, _ProjectionParams.x);
          // #endif

          
          #if _PASSFORWARD || _PASSGBUFFER
              float2 uv1 = v.texcoord1.xy;
              OUTPUT_LIGHTMAP_UV(uv1, unity_LightmapST, o.lightmapUV);
              // o.texcoord1.xy = uv1;
              OUTPUT_SH(o.worldNormal, o.sh);
              
              #if defined(DYNAMICLIGHTMAP_ON)
                   o.dynamicLightmapUV.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                   #if UNITY_VERSION >= 60000009
                     OUTPUT_SH(o.worldNormal, o.sh);
                   #endif
              #elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && UNITY_VERSION >= 60000009
                   OUTPUT_SH4(vertexInput.positionWS, o.worldNormal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), o.sh, o.probeOcclusion);
              #endif
          #endif

          #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
              half fogFactor = 0;
              #if defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(o.pos.z);
              #endif
              #if _BAKEDLIT
                 o.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);
              #else
                 half3 vertexLight = VertexLighting(o.worldPos, o.worldNormal);
                 o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
              #endif
          #endif

          #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             o.shadowCoord = GetShadowCoord(vertexInput);
          #endif

          #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
            #if !defined(TESSELLATION_ON)
              MotionVectorPositionZBias(o);
            #endif

            o.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
            // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
            bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

            if (!forceNoMotion)
            {
              #if defined(HAVE_VFX_MODIFICATION)
                float3 previousPositionOS = currentFrameMvData.vfxParticlePositionOS;
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  const bool applyDeformation = false;
                #else
                  const bool applyDeformation = true;
                #endif
              #else
                const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
                float3 previousPositionOS = hasDeformation ? previousMesh.previousPositionOS : previousMesh.vertex.xyz;

                #if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
                  const bool applyDeformation = true;
                #else
                  const bool applyDeformation = hasDeformation;
                #endif
              #endif
              // TODO
              #if defined(FEATURES_GRAPH_VERTEX)
                if (applyDeformation)
                  previousPositionOS = GetLastFrameDeformedPosition(previousMesh, currentFrameMvData, previousPositionOS);
                else
                  previousPositionOS = previousMesh.positionOS;

                #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT)
                  previousPositionOS -= previousMesh.precomputedVelocity;
                #endif
              #endif

              #if defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(DOTS_DEFORMED)
                // Deformed vertices in DOTS are not cumulative with built-in Unity skinning/blend shapes
                // Needs to be called after vertex modification has been applied otherwise it will be
                // overwritten by Compute Deform node
                ApplyPreviousFrameDeformedVertexPosition(previousMesh.vertexID, previousPositionOS);
              #endif
              #if defined (_ADD_PRECOMPUTED_VELOCITY)
                previousPositionOS -= previousMesh.precomputedVelocity;
              #endif
              o.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(positionWS, 1.0f));

              #if defined(HAVE_VFX_MODIFICATION)
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT) || defined(_ADD_PRECOMPUTED_VELOCITY)
                    #error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
                  #endif
                  o.previousPositionCS = VFXGetPreviousClipPosition(previousMesh, currentFrameMvData.vfxElementAttributes, o.positionCS);
                #else
                  #if VFX_WORLD_SPACE
                    //previousPositionOS is already in world space
                    const float3 previousPositionWS = previousPositionOS;
                  #else
                    const float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz;
                  #endif
                  o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionWS, 1.0f));
                #endif
              #else
                o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1)));
              #endif
            }
          #endif

          return o;
         }


            

            // fragment shader
            half4 Frag (VertexToPixel IN
               #if NEED_FACING
                  , bool facing : SV_IsFrontFace
               #endif
            ) : SV_Target
            {
               UNITY_SETUP_INSTANCE_ID(IN);

               ShaderData d = CreateShaderData(IN
                  #if NEED_FACING
                     , facing
                  #endif
               );

               Surface l = (Surface)0;

               l.Albedo = half3(0.5, 0.5, 0.5);
               l.Normal = float3(0,0,1);
               l.Occlusion = 1;
               l.Alpha = 1;

               ChainSurfaceFunction(l, d);

               MetaInput metaInput = (MetaInput)0;
               metaInput.Albedo = l.Albedo;
               metaInput.Emission = l.Emission;

               return MetaFragment(metaInput);

            }

         ENDHLSL

      }


      
        Pass
        {
            Name "DepthNormals"
            Tags
            {
               "LightMode" = "DepthNormals"
            }
    
            // Render State
             Cull Back
                ZTest LEqual
                ZWrite On

            	ZWrite On


            HLSLPROGRAM

               #pragma vertex Vert
   #pragma fragment Frag

            #if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES30)) 
            #pragma target 3.0
#else
            #pragma target 4.5
#endif

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
            #define _PASSDEPTH 1
            #define _PASSDEPTHNORMALS 1


            
	#include "Flow.cginc"


	#pragma shader_feature_local _ _STOCHASTIC_ON
	#pragma shader_feature_local _FOAM_OFF _FOAM_ALPHA _FOAM_CUTOUT


	#pragma shader_feature_local _ _FACETED_ON


	#pragma shader_feature_local _ALPHA_OFF _ALPHA_VERTICAL _ALPHA_DEPTH _ALPHA_MARCH_FLUID _ALPHA_MARCH_FLUID_AND_DEPTH


    #pragma shader_feature_local DISABLEFOG    


   #define _URP 1

   #define _ALPHABLEND_ON 1
#define _ALPHABLEND_ON 1
#define _SURFACE_TYPE_TRANSPARENT 1
#define _GRABPASSUSED 1
#define REQUIRE_OPAQUE_TEXTURE
#define REQUIRE_DEPTH_TEXTURE


            // this has to be here or specular color will be ignored. Not in SG code
            #if _SIMPLELIT
               #define _SPECULAR_COLOR
            #endif


            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            
            

        

               #undef WorldNormalVector
      #define WorldNormalVector(data, normal) mul(normal, data.TBNMatrix)
      
      #define UnityObjectToWorldNormal(normal) mul(GetObjectToWorldMatrix(), normal)

      #define _WorldSpaceLightPos0 _MainLightPosition
      
      #define UNITY_DECLARE_TEX2D(name) TEXTURE2D(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2D_NOSAMPLER(name) TEXTURE2D(name);
      #define UNITY_DECLARE_TEX2DARRAY(name) TEXTURE2D_ARRAY(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(name) TEXTURE2D_ARRAY(name);

      #define UNITY_SAMPLE_TEX2DARRAY(tex,coord)            SAMPLE_TEXTURE2D_ARRAY(tex, sampler##tex, coord.xy, coord.z)
      #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##tex, coord.xy, coord.z, lod)
      #define UNITY_SAMPLE_TEX2D(tex, coord)                SAMPLE_TEXTURE2D(tex, sampler##tex, coord)
      #define UNITY_SAMPLE_TEX2D_SAMPLER(tex, samp, coord)  SAMPLE_TEXTURE2D(tex, sampler##samp, coord)

      #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod)   SAMPLE_TEXTURE2D_LOD(tex, sampler_##tex, coord, lod)
      #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) SAMPLE_TEXTURE2D_LOD (tex, sampler##samplertex,coord, lod)
     
      #if defined(UNITY_COMPILER_HLSL)
         #define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
      #else
         #define UNITY_INITIALIZE_OUTPUT(type,name)
      #endif

      #define sampler2D_float sampler2D
      #define sampler2D_half sampler2D

      

      // data across stages, stripped like the above.
      struct VertexToPixel
      {
         float4 pos : SV_POSITION;
         float3 worldPos : TEXCOORD0;
         float3 worldNormal : TEXCOORD1;
         float4 worldTangent : TEXCOORD2;
          float4 texcoord0 : TEXCOORD3;
         // float4 texcoord1 : TEXCOORD4;
         // float4 texcoord2 : TEXCOORD5;

         // #if %TEXCOORD3REQUIREKEY%
         // float4 texcoord3 : TEXCOORD6;
         // #endif

         // #if %SCREENPOSREQUIREKEY%
          float4 screenPos : TEXCOORD7;
         // #endif

         // #if %VERTEXCOLORREQUIREKEY%
          half4 vertexColor : COLOR;
         // #endif

         #if defined(LIGHTMAP_ON)
            float2 lightmapUV : TEXCOORD8;
         #endif
         #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV : TEXCOORD9;
         #endif
         #if !defined(LIGHTMAP_ON)
            float4 probeOcclusion : TEXCOORD8;
            float3 sh : TEXCOORD10;
         #endif

         #if defined(VARYINGS_NEED_FOG_AND_VERTEX_LIGHT)
            float4 fogFactorAndVertexLight : TEXCOORD11;
         #endif

         #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
           float4 shadowCoord : TEXCOORD12;
         #endif

         // #if %EXTRAV2F0REQUIREKEY%
          float4 extraV2F0 : TEXCOORD13;
         // #endif

         // #if %EXTRAV2F1REQUIREKEY%
          float4 extraV2F1 : TEXCOORD14;
         // #endif

         // #if %EXTRAV2F2REQUIREKEY%
         // float4 extraV2F2 : TEXCOORD15;
         // #endif

         // #if %EXTRAV2F3REQUIREKEY%
         // float4 extraV2F3 : TEXCOORD16;
         // #endif

         // #if %EXTRAV2F4REQUIREKEY%
         // float4 extraV2F4 : TEXCOORD17;
         // #endif

         // #if %EXTRAV2F5REQUIREKEY%
         // float4 extraV2F5 : TEXCOORD18;
         // #endif

         // #if %EXTRAV2F6REQUIREKEY%
         // float4 extraV2F6 : TEXCOORD19;
         // #endif

         // #if %EXTRAV2F7REQUIREKEY%
         // float4 extraV2F7 : TEXCOORD20;
         // #endif

         #if UNITY_ANY_INSTANCING_ENABLED
         uint instanceID : CUSTOM_INSTANCE_ID;
         #endif
         #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
         uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
         #endif
         #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
         uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
         #endif
         #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
         FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
         #endif

         #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
            float4 previousPositionCS : TEXCOORD21; // Contain previous transform position (in case of skinning for example)
            float4 positionCS : TEXCOORD22;
         #endif
      };


         
            
            // data describing the user output of a pixel
            struct Surface
            {
               half3 Albedo;
               half Height;
               half3 Normal;
               half Smoothness;
               half3 Emission;
               half Metallic;
               half3 Specular;
               half Occlusion;
               half SpecularPower; // for simple lighting
               half Alpha;
               float outputDepth; // if written, SV_Depth semantic is used. ShaderData.clipPos.z is unused value
               // HDRP Only
               half SpecularOcclusion;
               half SubsurfaceMask;
               half Thickness;
               half CoatMask;
               half CoatSmoothness;
               half Anisotropy;
               half IridescenceMask;
               half IridescenceThickness;
               int DiffusionProfileHash;
               float SpecularAAThreshold;
               float SpecularAAScreenSpaceVariance;
               // requires _OVERRIDE_BAKEDGI to be defined, but is mapped in all pipelines
               float3 DiffuseGI;
               float3 BackDiffuseGI;
               float3 SpecularGI;
               float ior;
               float3 transmittanceColor;
               float atDistance;
               float transmittanceMask;
               // requires _OVERRIDE_SHADOWMASK to be defines
               float4 ShadowMask;

               // for decals
               float NormalAlpha;
               float MAOSAlpha;


            };

            // Data the user declares in blackboard blocks
            struct Blackboard
            {
                
	float  groundHeight;
	float  surfaceHeight;
	float3 surfaceNormal;

	Fluid fluid;


	float foam;


                float blackboardDummyData;
            };

            // data the user might need, this will grow to be big. But easy to strip
            struct ShaderData
            {
               float4 clipPos; // SV_POSITION
               float3 localSpacePosition;
               float3 localSpaceNormal;
               float3 localSpaceTangent;
        
               float3 worldSpacePosition;
               float3 worldSpaceNormal;
               float3 worldSpaceTangent;
               float tangentSign;

               float3 worldSpaceViewDir;
               float3 tangentSpaceViewDir;

               float4 texcoord0;
               float4 texcoord1;
               float4 texcoord2;
               float4 texcoord3;

               float2 screenUV;
               float4 screenPos;

               float4 vertexColor;
               bool isFrontFace;

               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;

               float3x3 TBNMatrix;
               Blackboard blackboard;
            };

            struct VertexData
            {
               #if SHADER_TARGET > 30
               // uint vertexID : SV_VertexID;
               #endif
               float4 vertex : POSITION;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;

               // optimize out mesh coords when not in use by user or lighting system
               #if _URP && (_USINGTEXCOORD1 || _PASSMETA || _PASSFORWARD || _PASSGBUFFER)
                  float4 texcoord1 : TEXCOORD1;
               #endif

               #if _URP && (_USINGTEXCOORD2 || _PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && defined(DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif

               #if _STANDARD && (_USINGTEXCOORD1 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER || _PASSFORWARDADD) && LIGHTMAP_ON)))
                  float4 texcoord1 : TEXCOORD1;
               #endif
               #if _STANDARD && (_USINGTEXCOORD2 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif


               #if _HDRP
                  float4 texcoord1 : TEXCOORD1;
                  float4 texcoord2 : TEXCOORD2;
               #endif

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessVertex 
            {
               float4 vertex : INTERNALTESSPOS;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;
               float4 texcoord1 : TEXCOORD1;
               float4 texcoord2 : TEXCOORD2;

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               // #if %EXTRAV2F0REQUIREKEY%
                float4 extraV2F0 : TEXCOORD5;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                float4 extraV2F1 : TEXCOORD6;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // float4 extraV2F2 : TEXCOORD7;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // float4 extraV2F3 : TEXCOORD8;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // float4 extraV2F4 : TEXCOORD9;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // float4 extraV2F5 : TEXCOORD10;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // float4 extraV2F6 : TEXCOORD11;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // float4 extraV2F7 : TEXCOORD12;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD13; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity : TEXCOORD14;
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ExtraV2F
            {
               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;
               Blackboard blackboard;
               float4 time;
            };


            float3 WorldToTangentSpace(ShaderData d, float3 normal)
            {
               return mul(d.TBNMatrix, normal);
            }

            float3 TangentToWorldSpace(ShaderData d, float3 normal)
            {
               return mul(normal, d.TBNMatrix);
            }

            // in this case, make standard more like SRPs, because we can't fix
            // unity_WorldToObject in HDRP, since it already does macro-fu there

            #if _STANDARD
               float3 TransformWorldToObject(float3 p) { return mul(unity_WorldToObject, float4(p, 1)); };
               float3 TransformObjectToWorld(float3 p) { return mul(unity_ObjectToWorld, float4(p, 1)); };
               float4 TransformWorldToObject(float4 p) { return mul(unity_WorldToObject, p); };
               float4 TransformObjectToWorld(float4 p) { return mul(unity_ObjectToWorld, p); };
               float4x4 GetWorldToObjectMatrix() { return unity_WorldToObject; }
               float4x4 GetObjectToWorldMatrix() { return unity_ObjectToWorld; }
               #if (defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (SHADER_TARGET_SURFACE_ANALYSIS && !SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod) tex.SampleLevel (sampler##tex,coord, lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) tex.SampleLevel (sampler##samplertex,coord, lod)
              #else
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex2D (tex,coord,0,lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord,lod) tex2D (tex,coord,0,lod)
              #endif

               #undef GetWorldToObjectMatrix()

               #define GetWorldToObjectMatrix()   unity_WorldToObject


            #endif

            float3 GetCameraWorldPosition()
            {
               #if _HDRP
                  return GetCameraRelativePositionWS(_WorldSpaceCameraPos);
               #else
                  return _WorldSpaceCameraPos;
               #endif
            }

            #if _GRABPASSUSED
               #if _STANDARD
                  TEXTURE2D(_Grab);
                  SAMPLER(sampler__Grab);
               #endif

               half3 GetSceneColor(float2 uv)
               {
                  #if _STANDARD
                     return SAMPLE_TEXTURE2D(_Grab, sampler__Grab, uv).rgb;
                  #else
                     return SHADERGRAPH_SAMPLE_SCENE_COLOR(uv);
                  #endif
               }
            #endif


      
            #if _STANDARD
               UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
               float GetSceneDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv)); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv)); } 
            #else
               float GetSceneDepth(float2 uv) { return SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv), _ZBufferParams); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv), _ZBufferParams); } 
            #endif

            float3 GetWorldPositionFromDepthBuffer(float2 uv, float3 worldSpaceViewDir)
            {
               float eye = GetLinearEyeDepth(uv);
               float3 camView = mul((float3x3)GetObjectToWorldMatrix(), transpose(mul(GetWorldToObjectMatrix(), UNITY_MATRIX_I_V)) [2].xyz);

               float dt = dot(worldSpaceViewDir, camView);
               float3 div = worldSpaceViewDir/dt;
               float3 wpos = (eye * div) + GetCameraWorldPosition();
               return wpos;
            }
			
			float3 GetSceneWorldPosition(float2 screenUV, float sceneDepth)
			{
				#if _STANDARD
					float4 clipPos  = float4(screenUV * 2.0f - 1.0f, 0.0f, 1.0f);
					float4 viewPos  = mul(unity_CameraInvProjection, clipPos);
					float3 worldDir = mul((float3x3)UNITY_MATRIX_I_V, viewPos);
					
					return _WorldSpaceCameraPos + worldDir * LinearEyeDepth(sceneDepth);
				#else
					float4 clipPos = float4(screenUV * 2.0 - 1.0, sceneDepth, 1.0);
					
					#if UNITY_UV_STARTS_AT_TOP
						clipPos.y = -clipPos.y;
					#endif
					
					float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
					
					worldPos.xyz /= worldPos.w;
					
					#if _HDRP
						worldPos.xyz = GetAbsolutePositionWS(worldPos.xyz);
					#endif
					
					return worldPos.xyz;
				#endif
			}
			
			float GetSceneWorldDistance(float2 screenUV, float sceneDepth)
			{
				return distance(_WorldSpaceCameraPos, GetSceneWorldPosition(screenUV, sceneDepth));
			}

            #if _HDRP
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return GetAbsolutePositionWS(TransformObjectToWorld(pos));
            }
            #else
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return TransformObjectToWorld(pos);
            }
            #endif

            #if _STANDARD
               UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  float4 depthNorms = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture, uv);
                  float3 norms = DecodeViewNormalStereo(depthNorms);
                  norms = mul((float3x3)GetWorldToViewMatrix(), norms) * 0.5 + 0.5;
                  return norms;
               }
            #elif _HDRP && !_DECALSHADER
               
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  NormalData nd;
                  DecodeFromNormalBuffer(_ScreenSize.xy * uv, nd);
                  return nd.normalWS;
               }
            #elif _URP
               #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
               #endif

               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                     return SampleSceneNormals(uv);
                  #else
                     float3 wpos = GetWorldPositionFromDepthBuffer(uv, worldSpaceViewDir);
                     return normalize(-cross(ddx(wpos), ddy(wpos))) * 0.5 + 0.5;
                  #endif

                }
             #endif

             #if _HDRP

               half3 UnpackNormalmapRGorAG(half4 packednormal)
               {
                     // This do the trick
                  packednormal.x *= packednormal.w;

                  half3 normal;
                  normal.xy = packednormal.xy * 2 - 1;
                  normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                  return normal;
               }
               half3 UnpackNormal(half4 packednormal)
               {
                  #if defined(UNITY_NO_DXT5nm)
                     return packednormal.xyz * 2 - 1;
                  #else
                     return UnpackNormalmapRGorAG(packednormal);
                  #endif
               }
            #endif
            #if _HDRP || _URP

               half3 UnpackScaleNormal(half4 packednormal, half scale)
               {
                 #ifndef UNITY_NO_DXT5nm
                   // Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
                   // Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
                   packednormal.x *= packednormal.w;
                 #endif
                   half3 normal;
                   normal.xy = (packednormal.xy * 2 - 1) * scale;
                   normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                   return normal;
               }	

             #endif


            void GetSun(out float3 lightDir, out float3 color)
            {
               lightDir = float3(0.5, 0.5, 0);
               color = 1;
               #if _HDRP
                  if (_DirectionalLightCount > 0)
                  {
                     DirectionalLightData light = _DirectionalLightDatas[0];
                     lightDir = -light.forward.xyz;
                     color = light.color;
                  }
               #elif _STANDARD
			         lightDir = normalize(_WorldSpaceLightPos0.xyz);
                  color = _LightColor0.rgb;
               #elif _URP
	               Light light = GetMainLight();
	               lightDir = light.direction;
	               color = light.color;
               #endif
            }


            
         CBUFFER_START(UnityPerMaterial)

            
	float    _FlowDensity;
	float2   _FlowSeparationXZ;
	float    _FlowSimulationHeight;
	float    _FlowCameraHeight;
	float2   _FlowCountXZ;
	float4   _FlowCoordU000;
	float4   _FlowCoord0V00;
	float    _FlowSpeed;
	float4x4 _FlowMatrix;

	float _Sink;


	float _Emission;
	
	float _WaveStrengthMin;
	float _WaveStrengthMax;
	float _WaveStrengthScale;

	float _Tiling;
	float _AnimationSpeed;
	float _FoamBrightness;


	half _FlatShadingBlend;


	float _RangeMax;
	float _AlphaStep;
	int _AlphaMaxSteps;
	float _AlphaDepthScale;





         CBUFFER_END

         

         

         
	TEXTURE2D(_FlowDataA);
	SAMPLER(sampler_FlowDataA);
	TEXTURE2D(_FlowDataB);
	SAMPLER(sampler_FlowDataB);
	TEXTURE2D(_FlowDataC);
	SAMPLER(sampler_FlowDataC);
	TEXTURE2D(_FlowDataD);
	SAMPLER(sampler_FlowDataD);
	TEXTURE2D(_FlowDataE);
	SAMPLER(sampler_FlowDataE);
	TEXTURE2D(_FlowDataF);
	SAMPLER(sampler_FlowDataF);

	float4 SGT_O2W(float4 v)
	{
		v = mul(GetObjectToWorldMatrix(), v);
		#if _HDRP
			v.xyz = GetAbsolutePositionWS(v.xyz);
		#endif
		return v;
	}

	float4 SGT_W2O(float4 v)
	{
		#if _HDRP
			v.xyz = GetCameraRelativePositionWS(v.xyz);
		#endif
		return mul(GetWorldToObjectMatrix(), v);
	}

	float4 SGT_O2V(float4 v)
	{
		#if _STANDARD
			return float4(UnityObjectToViewPos(v.xyz), 1.0f);
		#else
			return float4(TransformWorldToView(TransformObjectToWorld(v.xyz)), 1.0f);
		#endif
	}

	float GetFluidHeight(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return groundHeight + fluidDepth;
	}

	float2 GetHeightAndDepth(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return float2(groundHeight, fluidDepth);
	}

	Column GetColumn(float2 uv)
	{
		return DecodeColumn(SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0), SAMPLE_TEXTURE2D_LOD(_FlowDataB, sampler_FlowDataB, uv, 0));
	}

	Fluid GetColumnFluid(float2 uv)
	{
		float4 c = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0);
		float4 d = SAMPLE_TEXTURE2D_LOD(_FlowDataD, sampler_FlowDataD, uv, 0);
		float4 e = SAMPLE_TEXTURE2D_LOD(_FlowDataE, sampler_FlowDataE, uv, 0);
		float4 f = SAMPLE_TEXTURE2D_LOD(_FlowDataF, sampler_FlowDataF, uv, 0);
		return DecodeFluid(c, d, e, f);
	}

	bool InsideFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y > columnHAD.x && wpos.y < (columnHAD.x + columnHAD.y);
	}

	bool UnderFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y < (columnHAD.x + columnHAD.y);
	}

	float RayMarchInside(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (InsideFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	float RayMarchUnder(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (UnderFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	void Ext_ModifyVertex0 (inout VertexData v, inout ExtraV2F e)
	{
		float4 wpos        = SGT_O2W(v.vertex);
		float2 columnPixel = mul(_FlowMatrix, float4(wpos.xyz, 1.0f)).xy;
		float2 columnCoord = SnapCoordFromPixel(round(columnPixel), _FlowCountXZ);

		Column column0 = GetColumn(columnCoord);
		Column columnL = GetColumn(columnCoord - _FlowCoordU000.xy);
		Column columnR = GetColumn(columnCoord + _FlowCoordU000.xy);
		Column columnB = GetColumn(columnCoord - _FlowCoord0V00.xy);
		Column columnT = GetColumn(columnCoord + _FlowCoord0V00.xy);

		Fluid fluid0 = GetColumnFluid(columnCoord);
		Fluid fluidL = GetColumnFluid(columnCoord - _FlowCoordU000.xy);
		Fluid fluidR = GetColumnFluid(columnCoord + _FlowCoordU000.xy);
		Fluid fluidB = GetColumnFluid(columnCoord - _FlowCoord0V00.xy);
		Fluid fluidT = GetColumnFluid(columnCoord + _FlowCoord0V00.xy);

		float ww = fluidL.Depth + fluidR.Depth + fluidB.Depth + fluidT.Depth + 0.001f;
		float wL = fluidL.Depth / ww;
		float wR = fluidR.Depth / ww;
		float wB = fluidB.Depth / ww;
		float wT = fluidT.Depth / ww;
		float w0 = saturate(fluid0.Depth * 10);

		float hL = columnL.GroundHeight + fluidL.Depth;
		float hR = columnR.GroundHeight + fluidR.Depth;
		float hB = columnB.GroundHeight + fluidB.Depth;
		float hT = columnT.GroundHeight + fluidT.Depth;
		float hh = hL * wL + hR * wR + hB * wB + hT * wT;
		float h0 = column0.GroundHeight + fluid0.Depth;

		hh = lerp(hh, h0 - _Sink, saturate(0.01f / ww)); // Prevent skirts going down too far

		Fluid fluid = fluid0;

		fluid.Depth = lerp(fluidL.Depth * wL + fluidR.Depth * wR + fluidB.Depth * wB + fluidT.Depth * wT, fluid.Depth, w0);
		fluid.RGBA  = lerp(fluidL.RGBA  * wL + fluidR.RGBA  * wR + fluidB.RGBA  * wB + fluidT.RGBA  * wT, fluid.RGBA , w0);
		fluid.ESMV  = lerp(fluidL.ESMV  * wL + fluidR.ESMV  * wR + fluidB.ESMV  * wB + fluidT.ESMV  * wT, fluid.ESMV , w0);
		fluid.F123  = lerp(fluidL.F123  * wL + fluidR.F123  * wR + fluidB.F123  * wB + fluidT.F123  * wT, fluid.F123 , w0);

		e.blackboard.groundHeight  = column0.GroundHeight;
		e.blackboard.surfaceHeight = lerp(hh, h0, w0);
		e.blackboard.surfaceNormal = normalize(float3((hL - hR) / _FlowSeparationXZ.x, 2.0f, (hB - hT) / _FlowSeparationXZ.y));
		e.blackboard.fluid         = fluid;
	}


	TEXTURE2D(_NoiseMap);
	SAMPLER(sampler_NoiseMap);

	TEXTURE2D(_NormalFoamMap);
	SAMPLER(sampler_NormalFoamMap);

	void Ext_ModifyVertex3 (inout VertexData v, inout ExtraV2F e)
	{
		v.vertex.y = lerp(e.blackboard.groundHeight, e.blackboard.surfaceHeight, v.texcoord0.w) - _FlowSimulationHeight;

		v.normal = lerp(e.blackboard.surfaceNormal, v.normal, v.texcoord0.z);

		v.vertexColor = e.blackboard.fluid.RGBA;

		e.extraV2F0.xyz = SGT_O2V(v.vertex).xyz;
		e.extraV2F0.w   = e.blackboard.fluid.Depth;

		e.extraV2F1.xyz = e.blackboard.fluid.ESMV.xyz;
		e.extraV2F1.w = e.blackboard.fluid.F123.x;
	}

	float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float tiling, float time, float phaseOffset)
	{
		float progress = frac(time + phaseOffset);
		float3 uvw;
		uvw.xy = uv - flowVector * (progress - 0.5f);
		uvw.xy *= tiling;
		uvw.xy += phaseOffset;
		uvw.xy += (time - progress) * jump;
		uvw.z = 1 - abs(1 - 2 * progress);
		return uvw;
	}

	float3 CombineNormals(float3 n1, float3 n2)
	{
		return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
	}

	float3 UnpackNormalAndScale(float2 xy, float scale)
	{
		xy = xy * 2.0f - 1.0f; xy *= scale; return float3(xy, sqrt(1.0 - saturate(dot(xy, xy))));
	}

	float4 SampleStochastic(float2 uv, float noise)
	{
		float cur_height = uv.y + noise;
		float this_index = floor(cur_height);
		float next_index = this_index + 1.0f;

		float2 uvA     = uv + sin(float2(1.0f, 2.0f) * this_index);
		float2 uvB     = uv + sin(float2(1.0f, 2.0f) * next_index);
		float2 gradX   = ddx(uv);
		float2 gradY   = ddy(uv);

		float4 sampleA = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvA, gradX, gradY);
		float4 sampleB = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvB, gradX, gradY);

		return lerp(sampleA, sampleB, cur_height - this_index);
	}

	void Ext_SurfaceFunction3 (inout Surface o, inout ShaderData d)
	{
		d.blackboard.fluid.RGBA     = d.vertexColor;
		d.blackboard.fluid.Depth    = d.extraV2F0.w;
		d.blackboard.fluid.ESMV.xyz = d.extraV2F1.xyz;
		d.blackboard.fluid.F123.x   = d.extraV2F1.w;

		if (d.blackboard.fluid.Depth < 0.01f)
		{
			discard;
		}

		float2 uv          = d.worldSpacePosition.xz;
		float2 columnPixel = mul(_FlowMatrix, float4(d.worldSpacePosition, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		Column column      = GetColumn(columnCoord);

		float  time = _Time.y * _AnimationSpeed;
		float2 jump = float2(0.13f, 0.17f);
		float2 fvec = (column.Outflow.yw - column.Outflow.xz) * _FlowSeparationXZ * _FlowSpeed;
		float  fmag = log10(1.0f + length(fvec)) * 0.1f;

		float3 flowA = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.0f);
		float3 flowB = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.5f);

		#if _STOCHASTIC_ON
			float  noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 0.0025f).a * 4.0f;
			float4 nfmA  = SampleStochastic(flowA, noise);
			float4 nfmB  = SampleStochastic(flowB, noise);
		#else
			float4 nfmA = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowA.xy);
			float4 nfmB = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowB.xy);
		#endif

		// Water normals
		float  normalStr    = lerp(_WaveStrengthMin, _WaveStrengthMax, saturate(_WaveStrengthScale * fmag));
		float3 waterNormalA = UnpackNormalAndScale(nfmA.xy, flowA.z * normalStr);
		float3 waterNormalB = UnpackNormalAndScale(nfmB.xy, flowB.z * normalStr);
		float3 waterNormals = CombineNormals(waterNormalA, waterNormalB);

		o.Normal   = lerp(waterNormals, o.Normal, d.texcoord0.z);
		o.Albedo   = d.blackboard.fluid.RGBA.xyz;
		o.Emission = d.blackboard.fluid.ESMV.x * _Emission * o.Albedo;

		#if _FOAM_OFF
			d.blackboard.foam = 0.0f;
		#else
			d.blackboard.foam = d.blackboard.fluid.F123.x * (1.0f - d.texcoord0.z);

			#if _FOAM_ALPHA
				float3 foamAlbedo = (nfmA.w * flowA.z + nfmB.w * flowB.z) * _FoamBrightness;
			#elif _FOAM_CUTOUT
				float3 foamAlbedo = _FoamBrightness;
				float  foamRamp   = nfmA.w * flowA.z + nfmB.w * flowB.z;
				d.blackboard.foam = saturate((d.blackboard.foam - foamRamp) * 10.0f);
			#endif

			float3 foamNormal = float3(0.0f, 0.0f, 1.0f);

			o.Albedo = lerp(o.Albedo, foamAlbedo, d.blackboard.foam);
			o.Normal = lerp(o.Normal, foamNormal, d.blackboard.foam);
		#endif

		o.Smoothness = d.blackboard.fluid.ESMV.y;
		o.Metallic   = d.blackboard.fluid.ESMV.z;
	}


	void Ext_SurfaceFunction4 (inout Surface o, inout ShaderData d)
	{
	#if _FACETED_ON
		// lets just affect the TBN data, so we flat shade the original polygons, not the normal map
		float3 dx = ddx(d.worldSpacePosition);
		float3 dy = ddy(d.worldSpacePosition);
		float3 worldNormal = normalize(cross(dy, dx));
		worldNormal = lerp(d.worldSpaceNormal, worldNormal, _FlatShadingBlend);
		d.worldSpaceNormal = worldNormal;
		d.TBNMatrix[2] = worldNormal;
	#endif
	}


	void Ext_SurfaceFunction5 (inout Surface o, ShaderData d)
	{
		#if _ALPHA_OFF
		#else
			float3 position  = d.worldSpacePosition;
			float3 step      = d.worldSpacePosition - _WorldSpaceCameraPos;
			float2 bentScrUV = d.screenUV + o.Normal.xy * 0.05f * log(1.0f + d.blackboard.fluid.Depth * 1.0f);
			float  distMax   = max(_RangeMax * 0.001f, _RangeMax * (1.0f - d.blackboard.fluid.RGBA.w));
			float  distRange = distMax;
			float  vertDist  = length(d.extraV2F0.xyz);
			float  camtDist  = GetLinearEyeDepth(bentScrUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);

			if (camtDist < vertDist)
			{
				camtDist = GetLinearEyeDepth(d.screenUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);
			}

			float diffDist = max(0.0f, camtDist - vertDist);

			// The depth buffer becomes unusable at certain ranges, so fade it out
			diffDist += max(0.0f, camtDist * _AlphaDepthScale - _RangeMax);
			distRange = min(distRange, diffDist);

			#if _ALPHA_VERTICAL
				float dist = d.blackboard.fluid.Depth;
			#elif _ALPHA_DEPTH
				float dist = diffDist;
			#elif _ALPHA_MARCH_FLUID
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distMax);
			#elif _ALPHA_MARCH_FLUID_AND_DEPTH
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distRange);
			#endif

			// Set opacity based on distance through fluid relative to the maximum distance, and make sure high opacity fluids become solid
			float opacity = saturate(dist / distMax + d.blackboard.foam + pow(d.blackboard.fluid.RGBA.w, 10.0f));

			o.Albedo    = lerp(0.0f, o.Albedo, opacity);
			o.Emission  = lerp(GetSceneColor(bentScrUV), o.Emission, opacity);
			//o.Emission += o.Albedo * d.blackboard.fluid.ESMV.x * opacity;
		#endif
	}





        
            void ChainSurfaceFunction(inout Surface l, inout ShaderData d)
            {
                 // Ext_SurfaceFunction0(l, d);
                 // Ext_SurfaceFunction1(l, d);
                 // Ext_SurfaceFunction2(l, d);
                  Ext_SurfaceFunction3(l, d);
                  Ext_SurfaceFunction4(l, d);
                  Ext_SurfaceFunction5(l, d);
                 // Ext_SurfaceFunction6(l, d);
                 // Ext_SurfaceFunction7(l, d);
                 // Ext_SurfaceFunction8(l, d);
                 // Ext_SurfaceFunction9(l, d);
		           // Ext_SurfaceFunction10(l, d);
                 // Ext_SurfaceFunction11(l, d);
                 // Ext_SurfaceFunction12(l, d);
                 // Ext_SurfaceFunction13(l, d);
                 // Ext_SurfaceFunction14(l, d);
                 // Ext_SurfaceFunction15(l, d);
                 // Ext_SurfaceFunction16(l, d);
                 // Ext_SurfaceFunction17(l, d);
                 // Ext_SurfaceFunction18(l, d);
		           // Ext_SurfaceFunction19(l, d);
                 // Ext_SurfaceFunction20(l, d);
                 // Ext_SurfaceFunction21(l, d);
                 // Ext_SurfaceFunction22(l, d);
                 // Ext_SurfaceFunction23(l, d);
                 // Ext_SurfaceFunction24(l, d);
                 // Ext_SurfaceFunction25(l, d);
                 // Ext_SurfaceFunction26(l, d);
                 // Ext_SurfaceFunction27(l, d);
                 // Ext_SurfaceFunction28(l, d);
		           // Ext_SurfaceFunction29(l, d);
            }

#if !_DECALSHADER

            void ChainModifyVertex(inout VertexData v, inout VertexToPixel v2p, float4 time)
            {
                 ExtraV2F d;
                 
                 ZERO_INITIALIZE(ExtraV2F, d);
                 ZERO_INITIALIZE(Blackboard, d.blackboard);
                 // due to motion vectors in HDRP, we need to use the last
                 // time in certain spots. So if you are going to use _Time to adjust vertices,
                 // you need to use this time or motion vectors will break. 
                 d.time = time;

                   Ext_ModifyVertex0(v, d);
                 // Ext_ModifyVertex1(v, d);
                 // Ext_ModifyVertex2(v, d);
                  Ext_ModifyVertex3(v, d);
                 // Ext_ModifyVertex4(v, d);
                 // Ext_ModifyVertex5(v, d);
                 // Ext_ModifyVertex6(v, d);
                 // Ext_ModifyVertex7(v, d);
                 // Ext_ModifyVertex8(v, d);
                 // Ext_ModifyVertex9(v, d);
                 // Ext_ModifyVertex10(v, d);
                 // Ext_ModifyVertex11(v, d);
                 // Ext_ModifyVertex12(v, d);
                 // Ext_ModifyVertex13(v, d);
                 // Ext_ModifyVertex14(v, d);
                 // Ext_ModifyVertex15(v, d);
                 // Ext_ModifyVertex16(v, d);
                 // Ext_ModifyVertex17(v, d);
                 // Ext_ModifyVertex18(v, d);
                 // Ext_ModifyVertex19(v, d);
                 // Ext_ModifyVertex20(v, d);
                 // Ext_ModifyVertex21(v, d);
                 // Ext_ModifyVertex22(v, d);
                 // Ext_ModifyVertex23(v, d);
                 // Ext_ModifyVertex24(v, d);
                 // Ext_ModifyVertex25(v, d);
                 // Ext_ModifyVertex26(v, d);
                 // Ext_ModifyVertex27(v, d);
                 // Ext_ModifyVertex28(v, d);
                 // Ext_ModifyVertex29(v, d);


                 // #if %EXTRAV2F0REQUIREKEY%
                  v2p.extraV2F0 = d.extraV2F0;
                 // #endif

                 // #if %EXTRAV2F1REQUIREKEY%
                  v2p.extraV2F1 = d.extraV2F1;
                 // #endif

                 // #if %EXTRAV2F2REQUIREKEY%
                 // v2p.extraV2F2 = d.extraV2F2;
                 // #endif

                 // #if %EXTRAV2F3REQUIREKEY%
                 // v2p.extraV2F3 = d.extraV2F3;
                 // #endif

                 // #if %EXTRAV2F4REQUIREKEY%
                 // v2p.extraV2F4 = d.extraV2F4;
                 // #endif

                 // #if %EXTRAV2F5REQUIREKEY%
                 // v2p.extraV2F5 = d.extraV2F5;
                 // #endif

                 // #if %EXTRAV2F6REQUIREKEY%
                 // v2p.extraV2F6 = d.extraV2F6;
                 // #endif

                 // #if %EXTRAV2F7REQUIREKEY%
                 // v2p.extraV2F7 = d.extraV2F7;
                 // #endif
            }

            void ChainModifyTessellatedVertex(inout VertexData v, inout VertexToPixel v2p)
            {
               ExtraV2F d;
               ZERO_INITIALIZE(ExtraV2F, d);
               ZERO_INITIALIZE(Blackboard, d.blackboard);

               // #if %EXTRAV2F0REQUIREKEY%
                d.extraV2F0 = v2p.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                d.extraV2F1 = v2p.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // d.extraV2F2 = v2p.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // d.extraV2F3 = v2p.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // d.extraV2F4 = v2p.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // d.extraV2F5 = v2p.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // d.extraV2F6 = v2p.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // d.extraV2F7 = v2p.extraV2F7;
               // #endif


               // Ext_ModifyTessellatedVertex0(v, d);
               // Ext_ModifyTessellatedVertex1(v, d);
               // Ext_ModifyTessellatedVertex2(v, d);
               // Ext_ModifyTessellatedVertex3(v, d);
               // Ext_ModifyTessellatedVertex4(v, d);
               // Ext_ModifyTessellatedVertex5(v, d);
               // Ext_ModifyTessellatedVertex6(v, d);
               // Ext_ModifyTessellatedVertex7(v, d);
               // Ext_ModifyTessellatedVertex8(v, d);
               // Ext_ModifyTessellatedVertex9(v, d);
               // Ext_ModifyTessellatedVertex10(v, d);
               // Ext_ModifyTessellatedVertex11(v, d);
               // Ext_ModifyTessellatedVertex12(v, d);
               // Ext_ModifyTessellatedVertex13(v, d);
               // Ext_ModifyTessellatedVertex14(v, d);
               // Ext_ModifyTessellatedVertex15(v, d);
               // Ext_ModifyTessellatedVertex16(v, d);
               // Ext_ModifyTessellatedVertex17(v, d);
               // Ext_ModifyTessellatedVertex18(v, d);
               // Ext_ModifyTessellatedVertex19(v, d);
               // Ext_ModifyTessellatedVertex20(v, d);
               // Ext_ModifyTessellatedVertex21(v, d);
               // Ext_ModifyTessellatedVertex22(v, d);
               // Ext_ModifyTessellatedVertex23(v, d);
               // Ext_ModifyTessellatedVertex24(v, d);
               // Ext_ModifyTessellatedVertex25(v, d);
               // Ext_ModifyTessellatedVertex26(v, d);
               // Ext_ModifyTessellatedVertex27(v, d);
               // Ext_ModifyTessellatedVertex28(v, d);
               // Ext_ModifyTessellatedVertex29(v, d);

               // #if %EXTRAV2F0REQUIREKEY%
                v2p.extraV2F0 = d.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                v2p.extraV2F1 = d.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // v2p.extraV2F2 = d.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // v2p.extraV2F3 = d.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // v2p.extraV2F4 = d.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // v2p.extraV2F5 = d.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // v2p.extraV2F6 = d.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // v2p.extraV2F7 = d.extraV2F7;
               // #endif
            }

            void ChainFinalColorForward(inout Surface l, inout ShaderData d, inout half4 color)
            {
               //   Ext_FinalColorForward0(l, d, color);
               //   Ext_FinalColorForward1(l, d, color);
               //   Ext_FinalColorForward2(l, d, color);
               //   Ext_FinalColorForward3(l, d, color);
               //   Ext_FinalColorForward4(l, d, color);
               //   Ext_FinalColorForward5(l, d, color);
               //   Ext_FinalColorForward6(l, d, color);
               //   Ext_FinalColorForward7(l, d, color);
               //   Ext_FinalColorForward8(l, d, color);
               //   Ext_FinalColorForward9(l, d, color);
               //  Ext_FinalColorForward10(l, d, color);
               //  Ext_FinalColorForward11(l, d, color);
               //  Ext_FinalColorForward12(l, d, color);
               //  Ext_FinalColorForward13(l, d, color);
               //  Ext_FinalColorForward14(l, d, color);
               //  Ext_FinalColorForward15(l, d, color);
               //  Ext_FinalColorForward16(l, d, color);
               //  Ext_FinalColorForward17(l, d, color);
               //  Ext_FinalColorForward18(l, d, color);
               //  Ext_FinalColorForward19(l, d, color);
               //  Ext_FinalColorForward20(l, d, color);
               //  Ext_FinalColorForward21(l, d, color);
               //  Ext_FinalColorForward22(l, d, color);
               //  Ext_FinalColorForward23(l, d, color);
               //  Ext_FinalColorForward24(l, d, color);
               //  Ext_FinalColorForward25(l, d, color);
               //  Ext_FinalColorForward26(l, d, color);
               //  Ext_FinalColorForward27(l, d, color);
               //  Ext_FinalColorForward28(l, d, color);
               //  Ext_FinalColorForward29(l, d, color);
            }

            void ChainFinalGBufferStandard(inout Surface s, inout ShaderData d, inout half4 GBuffer0, inout half4 GBuffer1, inout half4 GBuffer2, inout half4 outEmission, inout half4 outShadowMask)
            {
               //   Ext_FinalGBufferStandard0(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard1(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard2(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard3(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard4(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard5(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard6(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard7(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard8(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard9(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard10(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard11(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard12(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard13(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard14(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard15(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard16(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard17(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard18(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard19(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard20(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard21(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard22(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard23(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard24(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard25(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard26(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard27(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard28(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard29(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
            }
#endif


         


#if _DECALSHADER

        ShaderData CreateShaderData(SurfaceDescriptionInputs IN)
        {
            ShaderData d = (ShaderData)0;
            d.TBNMatrix = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
            d.worldSpaceNormal = IN.WorldSpaceNormal;
            d.worldSpaceTangent = IN.WorldSpaceTangent;

            d.worldSpacePosition = IN.WorldSpacePosition;
            d.texcoord0 = IN.uv0.xyxy;
            d.screenPos = IN.ScreenPosition;

            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - d.worldSpacePosition);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(d.worldSpacePosition), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(d.worldSpacePosition, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenUV = (IN.ScreenPosition.xy / max(0.01, IN.ScreenPosition.w));
            // #endif

            return d;
        }
#else

         ShaderData CreateShaderData(VertexToPixel i
                  #if NEED_FACING
                     , bool facing
                  #endif
         )
         {
            ShaderData d = (ShaderData)0;
            d.clipPos = i.pos;
            d.worldSpacePosition = i.worldPos;

            d.worldSpaceNormal = normalize(i.worldNormal);
            d.worldSpaceTangent.xyz = normalize(i.worldTangent.xyz);

            d.tangentSign = i.worldTangent.w * unity_WorldTransformParams.w;
            float3 bitangent = cross(d.worldSpaceTangent.xyz, d.worldSpaceNormal) * d.tangentSign;
           
            d.TBNMatrix = float3x3(d.worldSpaceTangent, -bitangent, d.worldSpaceNormal);
            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);
             d.texcoord0 = i.texcoord0;
            // d.texcoord1 = i.texcoord1;
            // d.texcoord2 = i.texcoord2;

            // #if %TEXCOORD3REQUIREKEY%
            // d.texcoord3 = i.texcoord3;
            // #endif

            // d.isFrontFace = facing;
            // #if %VERTEXCOLORREQUIREKEY%
             d.vertexColor = i.vertexColor;
            // #endif

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(i.worldPos), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(i.worldPos, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenPos = i.screenPos;
             d.screenUV = (i.screenPos.xy / i.screenPos.w);
            // #endif


            // #if %EXTRAV2F0REQUIREKEY%
             d.extraV2F0 = i.extraV2F0;
            // #endif

            // #if %EXTRAV2F1REQUIREKEY%
             d.extraV2F1 = i.extraV2F1;
            // #endif

            // #if %EXTRAV2F2REQUIREKEY%
            // d.extraV2F2 = i.extraV2F2;
            // #endif

            // #if %EXTRAV2F3REQUIREKEY%
            // d.extraV2F3 = i.extraV2F3;
            // #endif

            // #if %EXTRAV2F4REQUIREKEY%
            // d.extraV2F4 = i.extraV2F4;
            // #endif

            // #if %EXTRAV2F5REQUIREKEY%
            // d.extraV2F5 = i.extraV2F5;
            // #endif

            // #if %EXTRAV2F6REQUIREKEY%
            // d.extraV2F6 = i.extraV2F6;
            // #endif

            // #if %EXTRAV2F7REQUIREKEY%
            // d.extraV2F7 = i.extraV2F7;
            // #endif

            return d;
         }

#endif

         
         #if defined(_PASSSHADOW)
            float3 _LightDirection;
            float3 _LightPosition;
         #endif

         #if (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))

            #define GetWorldToViewMatrix()     _ViewMatrix
            #define UNITY_MATRIX_I_V   _InvViewMatrix
            #define GetViewToHClipMatrix()     OptimizeProjectionMatrix(_ProjMatrix)
            #define UNITY_MATRIX_I_P   _InvProjMatrix
            #define GetWorldToHClipMatrix()    _ViewProjMatrix
            #define UNITY_MATRIX_I_VP  _InvViewProjMatrix
            #define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
            #define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix
            #define UNITY_MATRIX_PREV_I_VP _PrevInvViewProjMatrix

            void MotionVectorPositionZBias(VertexToPixel input)
            {
                #if UNITY_REVERSED_Z
                input.pos.z -= unity_MotionVectorsParams.z * input.pos.w;
                #else
                input.pos.z += unity_MotionVectorsParams.z * input.pos.w;
                #endif
            }

        #endif

         // vertex shader
         VertexToPixel Vert (VertexData v)
         {
           VertexToPixel o = (VertexToPixel)0;

           UNITY_SETUP_INSTANCE_ID(v);
           UNITY_TRANSFER_INSTANCE_ID(v, o);
           UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            
           #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
             VertexData previousMesh = v;
           #endif
           #if !_TESSELLATION_ON
             ChainModifyVertex(v, o, _Time);
           #endif

            o.texcoord0 = v.texcoord0;
           // o.texcoord1 = v.texcoord1;
           // o.texcoord2 = v.texcoord2;

           // #if %TEXCOORD3REQUIREKEY%
           // o.texcoord3 = v.texcoord3;
           // #endif

           // #if %VERTEXCOLORREQUIREKEY%
            o.vertexColor = v.vertexColor;
           // #endif

           // This return the camera relative position (if enable)
           float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
           float3 normalWS = TransformObjectToWorldNormal(v.normal);
           float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
           
           VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
           o.worldPos = positionWS;
           o.worldNormal = normalWS;
           o.worldTangent = tangentWS;


          // For some very odd reason, in 2021.2, we can't use Unity's defines, but have to use our own..
          #if _PASSSHADOW
              #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                 float3 lightDirectionWS = normalize(_LightPosition - o.worldPos);
              #else
                 float3 lightDirectionWS = _LightDirection;
              #endif
              // Define shadow pass specific clip position for Universal
              o.pos = TransformWorldToHClip(ApplyShadowBias(o.worldPos, o.worldNormal, lightDirectionWS));
              #if UNITY_REVERSED_Z
                  o.pos.z = min(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #else
                  o.pos.z = max(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #endif
          #elif _PASSMETA
              o.pos = MetaVertexPosition(float4(v.vertex.xyz, 0), v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #else
              o.pos = TransformWorldToHClip(o.worldPos);
          #endif

          // #if %SCREENPOSREQUIREKEY%
           o.screenPos = ComputeScreenPos(o.pos, _ProjectionParams.x);
          // #endif

          
          #if _PASSFORWARD || _PASSGBUFFER
              float2 uv1 = v.texcoord1.xy;
              OUTPUT_LIGHTMAP_UV(uv1, unity_LightmapST, o.lightmapUV);
              // o.texcoord1.xy = uv1;
              OUTPUT_SH(o.worldNormal, o.sh);
              
              #if defined(DYNAMICLIGHTMAP_ON)
                   o.dynamicLightmapUV.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                   #if UNITY_VERSION >= 60000009
                     OUTPUT_SH(o.worldNormal, o.sh);
                   #endif
              #elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && UNITY_VERSION >= 60000009
                   OUTPUT_SH4(vertexInput.positionWS, o.worldNormal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), o.sh, o.probeOcclusion);
              #endif
          #endif

          #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
              half fogFactor = 0;
              #if defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(o.pos.z);
              #endif
              #if _BAKEDLIT
                 o.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);
              #else
                 half3 vertexLight = VertexLighting(o.worldPos, o.worldNormal);
                 o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
              #endif
          #endif

          #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             o.shadowCoord = GetShadowCoord(vertexInput);
          #endif

          #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
            #if !defined(TESSELLATION_ON)
              MotionVectorPositionZBias(o);
            #endif

            o.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
            // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
            bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

            if (!forceNoMotion)
            {
              #if defined(HAVE_VFX_MODIFICATION)
                float3 previousPositionOS = currentFrameMvData.vfxParticlePositionOS;
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  const bool applyDeformation = false;
                #else
                  const bool applyDeformation = true;
                #endif
              #else
                const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
                float3 previousPositionOS = hasDeformation ? previousMesh.previousPositionOS : previousMesh.vertex.xyz;

                #if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
                  const bool applyDeformation = true;
                #else
                  const bool applyDeformation = hasDeformation;
                #endif
              #endif
              // TODO
              #if defined(FEATURES_GRAPH_VERTEX)
                if (applyDeformation)
                  previousPositionOS = GetLastFrameDeformedPosition(previousMesh, currentFrameMvData, previousPositionOS);
                else
                  previousPositionOS = previousMesh.positionOS;

                #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT)
                  previousPositionOS -= previousMesh.precomputedVelocity;
                #endif
              #endif

              #if defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(DOTS_DEFORMED)
                // Deformed vertices in DOTS are not cumulative with built-in Unity skinning/blend shapes
                // Needs to be called after vertex modification has been applied otherwise it will be
                // overwritten by Compute Deform node
                ApplyPreviousFrameDeformedVertexPosition(previousMesh.vertexID, previousPositionOS);
              #endif
              #if defined (_ADD_PRECOMPUTED_VELOCITY)
                previousPositionOS -= previousMesh.precomputedVelocity;
              #endif
              o.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(positionWS, 1.0f));

              #if defined(HAVE_VFX_MODIFICATION)
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT) || defined(_ADD_PRECOMPUTED_VELOCITY)
                    #error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
                  #endif
                  o.previousPositionCS = VFXGetPreviousClipPosition(previousMesh, currentFrameMvData.vfxElementAttributes, o.positionCS);
                #else
                  #if VFX_WORLD_SPACE
                    //previousPositionOS is already in world space
                    const float3 previousPositionWS = previousPositionOS;
                  #else
                    const float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz;
                  #endif
                  o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionWS, 1.0f));
                #endif
              #else
                o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1)));
              #endif
            }
          #endif

          return o;
         }


         

         // fragment shader
         void Frag (VertexToPixel IN
            , out half4 outNormalWS : SV_Target0
         #ifdef _WRITE_RENDERING_LAYERS
            , out float4 outRenderingLayers : SV_Target1
         #endif
            #ifdef _DEPTHOFFSET_ON
              , out float outputDepth : SV_Depth
            #endif
            #if NEED_FACING
               , bool facing : SV_IsFrontFace
            #endif
         )
         {
           UNITY_SETUP_INSTANCE_ID(IN);
           UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

           #if defined(LOD_FADE_CROSSFADE)
              LODFadeCrossFade(IN.pos);
           #endif

           ShaderData d = CreateShaderData(IN
                  #if NEED_FACING
                     , facing
                  #endif
               );
           Surface l = (Surface)0;

           #ifdef _DEPTHOFFSET_ON
              l.outputDepth = outputDepth;
           #endif

           l.Albedo = half3(0.5, 0.5, 0.5);
           l.Normal = float3(0,0,1);
           l.Occlusion = 1;
           l.Alpha = 1;

           ChainSurfaceFunction(l, d);

           #ifdef _DEPTHOFFSET_ON
              outputDepth = l.outputDepth;
           #endif

          #if defined(_GBUFFER_NORMALS_OCT)
              float3 normalWS = d.worldSpaceNormal;
              float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
              float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
              half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
              outNormalWS = half4(packedNormalWS, 0.0);
          #else
              float3 wsn = l.Normal;
              #if !_WORLDSPACENORMAL
                wsn = TangentToWorldSpace(d, l.Normal);
              #endif
              outNormalWS = half4(NormalizeNormalPerPixel(wsn), 0.0);
          #endif

          #ifdef _WRITE_RENDERING_LAYERS
            uint renderingLayers = GetMeshRenderingLayer();
            outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
          #endif

         
         }

         ENDHLSL

      }


      
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask RG

        	ZWrite On


        HLSLPROGRAM

           #pragma vertex Vert
   #pragma fragment Frag

        #define _PASSMOTIONVECTOR 1

        #if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES30)) 
            #pragma target 3.0
#else
            #pragma target 4.5
#endif
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON

        #define SHADERPASS SHADERPASS_MOTION_VECTORS
        #define RAYTRACING_SHADER_GRAPH_DEFAULT
        #define VARYINGS_NEED_PASS
        #define _PASSMOTIONVECTOR 1
        
        
	#include "Flow.cginc"


	#pragma shader_feature_local _ _STOCHASTIC_ON
	#pragma shader_feature_local _FOAM_OFF _FOAM_ALPHA _FOAM_CUTOUT


	#pragma shader_feature_local _ _FACETED_ON


	#pragma shader_feature_local _ALPHA_OFF _ALPHA_VERTICAL _ALPHA_DEPTH _ALPHA_MARCH_FLUID _ALPHA_MARCH_FLUID_AND_DEPTH


    #pragma shader_feature_local DISABLEFOG    


   #define _URP 1

   #define _ALPHABLEND_ON 1
#define _ALPHABLEND_ON 1
#define _SURFACE_TYPE_TRANSPARENT 1
#define _GRABPASSUSED 1
#define REQUIRE_OPAQUE_TEXTURE
#define REQUIRE_DEPTH_TEXTURE


        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
         
              #undef WorldNormalVector
      #define WorldNormalVector(data, normal) mul(normal, data.TBNMatrix)
      
      #define UnityObjectToWorldNormal(normal) mul(GetObjectToWorldMatrix(), normal)

      #define _WorldSpaceLightPos0 _MainLightPosition
      
      #define UNITY_DECLARE_TEX2D(name) TEXTURE2D(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2D_NOSAMPLER(name) TEXTURE2D(name);
      #define UNITY_DECLARE_TEX2DARRAY(name) TEXTURE2D_ARRAY(name); SAMPLER(sampler##name);
      #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(name) TEXTURE2D_ARRAY(name);

      #define UNITY_SAMPLE_TEX2DARRAY(tex,coord)            SAMPLE_TEXTURE2D_ARRAY(tex, sampler##tex, coord.xy, coord.z)
      #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##tex, coord.xy, coord.z, lod)
      #define UNITY_SAMPLE_TEX2D(tex, coord)                SAMPLE_TEXTURE2D(tex, sampler##tex, coord)
      #define UNITY_SAMPLE_TEX2D_SAMPLER(tex, samp, coord)  SAMPLE_TEXTURE2D(tex, sampler##samp, coord)

      #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod)   SAMPLE_TEXTURE2D_LOD(tex, sampler_##tex, coord, lod)
      #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) SAMPLE_TEXTURE2D_LOD (tex, sampler##samplertex,coord, lod)
     
      #if defined(UNITY_COMPILER_HLSL)
         #define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
      #else
         #define UNITY_INITIALIZE_OUTPUT(type,name)
      #endif

      #define sampler2D_float sampler2D
      #define sampler2D_half sampler2D

      

      // data across stages, stripped like the above.
      struct VertexToPixel
      {
         float4 pos : SV_POSITION;
         float3 worldPos : TEXCOORD0;
         float3 worldNormal : TEXCOORD1;
         float4 worldTangent : TEXCOORD2;
          float4 texcoord0 : TEXCOORD3;
         // float4 texcoord1 : TEXCOORD4;
         // float4 texcoord2 : TEXCOORD5;

         // #if %TEXCOORD3REQUIREKEY%
         // float4 texcoord3 : TEXCOORD6;
         // #endif

         // #if %SCREENPOSREQUIREKEY%
          float4 screenPos : TEXCOORD7;
         // #endif

         // #if %VERTEXCOLORREQUIREKEY%
          half4 vertexColor : COLOR;
         // #endif

         #if defined(LIGHTMAP_ON)
            float2 lightmapUV : TEXCOORD8;
         #endif
         #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV : TEXCOORD9;
         #endif
         #if !defined(LIGHTMAP_ON)
            float4 probeOcclusion : TEXCOORD8;
            float3 sh : TEXCOORD10;
         #endif

         #if defined(VARYINGS_NEED_FOG_AND_VERTEX_LIGHT)
            float4 fogFactorAndVertexLight : TEXCOORD11;
         #endif

         #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
           float4 shadowCoord : TEXCOORD12;
         #endif

         // #if %EXTRAV2F0REQUIREKEY%
          float4 extraV2F0 : TEXCOORD13;
         // #endif

         // #if %EXTRAV2F1REQUIREKEY%
          float4 extraV2F1 : TEXCOORD14;
         // #endif

         // #if %EXTRAV2F2REQUIREKEY%
         // float4 extraV2F2 : TEXCOORD15;
         // #endif

         // #if %EXTRAV2F3REQUIREKEY%
         // float4 extraV2F3 : TEXCOORD16;
         // #endif

         // #if %EXTRAV2F4REQUIREKEY%
         // float4 extraV2F4 : TEXCOORD17;
         // #endif

         // #if %EXTRAV2F5REQUIREKEY%
         // float4 extraV2F5 : TEXCOORD18;
         // #endif

         // #if %EXTRAV2F6REQUIREKEY%
         // float4 extraV2F6 : TEXCOORD19;
         // #endif

         // #if %EXTRAV2F7REQUIREKEY%
         // float4 extraV2F7 : TEXCOORD20;
         // #endif

         #if UNITY_ANY_INSTANCING_ENABLED
         uint instanceID : CUSTOM_INSTANCE_ID;
         #endif
         #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
         uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
         #endif
         #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
         uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
         #endif
         #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
         FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
         #endif

         #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
            float4 previousPositionCS : TEXCOORD21; // Contain previous transform position (in case of skinning for example)
            float4 positionCS : TEXCOORD22;
         #endif
      };

        
        
            
            // data describing the user output of a pixel
            struct Surface
            {
               half3 Albedo;
               half Height;
               half3 Normal;
               half Smoothness;
               half3 Emission;
               half Metallic;
               half3 Specular;
               half Occlusion;
               half SpecularPower; // for simple lighting
               half Alpha;
               float outputDepth; // if written, SV_Depth semantic is used. ShaderData.clipPos.z is unused value
               // HDRP Only
               half SpecularOcclusion;
               half SubsurfaceMask;
               half Thickness;
               half CoatMask;
               half CoatSmoothness;
               half Anisotropy;
               half IridescenceMask;
               half IridescenceThickness;
               int DiffusionProfileHash;
               float SpecularAAThreshold;
               float SpecularAAScreenSpaceVariance;
               // requires _OVERRIDE_BAKEDGI to be defined, but is mapped in all pipelines
               float3 DiffuseGI;
               float3 BackDiffuseGI;
               float3 SpecularGI;
               float ior;
               float3 transmittanceColor;
               float atDistance;
               float transmittanceMask;
               // requires _OVERRIDE_SHADOWMASK to be defines
               float4 ShadowMask;

               // for decals
               float NormalAlpha;
               float MAOSAlpha;


            };

            // Data the user declares in blackboard blocks
            struct Blackboard
            {
                
	float  groundHeight;
	float  surfaceHeight;
	float3 surfaceNormal;

	Fluid fluid;


	float foam;


                float blackboardDummyData;
            };

            // data the user might need, this will grow to be big. But easy to strip
            struct ShaderData
            {
               float4 clipPos; // SV_POSITION
               float3 localSpacePosition;
               float3 localSpaceNormal;
               float3 localSpaceTangent;
        
               float3 worldSpacePosition;
               float3 worldSpaceNormal;
               float3 worldSpaceTangent;
               float tangentSign;

               float3 worldSpaceViewDir;
               float3 tangentSpaceViewDir;

               float4 texcoord0;
               float4 texcoord1;
               float4 texcoord2;
               float4 texcoord3;

               float2 screenUV;
               float4 screenPos;

               float4 vertexColor;
               bool isFrontFace;

               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;

               float3x3 TBNMatrix;
               Blackboard blackboard;
            };

            struct VertexData
            {
               #if SHADER_TARGET > 30
               // uint vertexID : SV_VertexID;
               #endif
               float4 vertex : POSITION;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;

               // optimize out mesh coords when not in use by user or lighting system
               #if _URP && (_USINGTEXCOORD1 || _PASSMETA || _PASSFORWARD || _PASSGBUFFER)
                  float4 texcoord1 : TEXCOORD1;
               #endif

               #if _URP && (_USINGTEXCOORD2 || _PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && defined(DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif

               #if _STANDARD && (_USINGTEXCOORD1 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER || _PASSFORWARDADD) && LIGHTMAP_ON)))
                  float4 texcoord1 : TEXCOORD1;
               #endif
               #if _STANDARD && (_USINGTEXCOORD2 || (_PASSMETA || ((_PASSFORWARD || _PASSGBUFFER) && DYNAMICLIGHTMAP_ON)))
                  float4 texcoord2 : TEXCOORD2;
               #endif


               #if _HDRP
                  float4 texcoord1 : TEXCOORD1;
                  float4 texcoord2 : TEXCOORD2;
               #endif

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessVertex 
            {
               float4 vertex : INTERNALTESSPOS;
               float3 normal : NORMAL;
               float4 tangent : TANGENT;
               float4 texcoord0 : TEXCOORD0;
               float4 texcoord1 : TEXCOORD1;
               float4 texcoord2 : TEXCOORD2;

               // #if %TEXCOORD3REQUIREKEY%
               // float4 texcoord3 : TEXCOORD3;
               // #endif

               // #if %VERTEXCOLORREQUIREKEY%
                float4 vertexColor : COLOR;
               // #endif

               // #if %EXTRAV2F0REQUIREKEY%
                float4 extraV2F0 : TEXCOORD5;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                float4 extraV2F1 : TEXCOORD6;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // float4 extraV2F2 : TEXCOORD7;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // float4 extraV2F3 : TEXCOORD8;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // float4 extraV2F4 : TEXCOORD9;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // float4 extraV2F5 : TEXCOORD10;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // float4 extraV2F6 : TEXCOORD11;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // float4 extraV2F7 : TEXCOORD12;
               // #endif

               #if _PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR))
                  float3 previousPositionOS : TEXCOORD13; // Contain previous transform position (in case of skinning for example)
                  #if defined (_ADD_PRECOMPUTED_VELOCITY)
                     float3 precomputedVelocity : TEXCOORD14;
                  #endif
               #endif

               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ExtraV2F
            {
               float4 extraV2F0;
               float4 extraV2F1;
               float4 extraV2F2;
               float4 extraV2F3;
               float4 extraV2F4;
               float4 extraV2F5;
               float4 extraV2F6;
               float4 extraV2F7;
               Blackboard blackboard;
               float4 time;
            };


            float3 WorldToTangentSpace(ShaderData d, float3 normal)
            {
               return mul(d.TBNMatrix, normal);
            }

            float3 TangentToWorldSpace(ShaderData d, float3 normal)
            {
               return mul(normal, d.TBNMatrix);
            }

            // in this case, make standard more like SRPs, because we can't fix
            // unity_WorldToObject in HDRP, since it already does macro-fu there

            #if _STANDARD
               float3 TransformWorldToObject(float3 p) { return mul(unity_WorldToObject, float4(p, 1)); };
               float3 TransformObjectToWorld(float3 p) { return mul(unity_ObjectToWorld, float4(p, 1)); };
               float4 TransformWorldToObject(float4 p) { return mul(unity_WorldToObject, p); };
               float4 TransformObjectToWorld(float4 p) { return mul(unity_ObjectToWorld, p); };
               float4x4 GetWorldToObjectMatrix() { return unity_WorldToObject; }
               float4x4 GetObjectToWorldMatrix() { return unity_ObjectToWorld; }
               #if (defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (SHADER_TARGET_SURFACE_ANALYSIS && !SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord, lod) tex.SampleLevel (sampler##tex,coord, lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord, lod) tex.SampleLevel (sampler##samplertex,coord, lod)
              #else
                 #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex2D (tex,coord,0,lod)
                 #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex,samplertex,coord,lod) tex2D (tex,coord,0,lod)
              #endif

               #undef GetWorldToObjectMatrix()

               #define GetWorldToObjectMatrix()   unity_WorldToObject


            #endif

            float3 GetCameraWorldPosition()
            {
               #if _HDRP
                  return GetCameraRelativePositionWS(_WorldSpaceCameraPos);
               #else
                  return _WorldSpaceCameraPos;
               #endif
            }

            #if _GRABPASSUSED
               #if _STANDARD
                  TEXTURE2D(_Grab);
                  SAMPLER(sampler__Grab);
               #endif

               half3 GetSceneColor(float2 uv)
               {
                  #if _STANDARD
                     return SAMPLE_TEXTURE2D(_Grab, sampler__Grab, uv).rgb;
                  #else
                     return SHADERGRAPH_SAMPLE_SCENE_COLOR(uv);
                  #endif
               }
            #endif


      
            #if _STANDARD
               UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
               float GetSceneDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv)); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv)); } 
            #else
               float GetSceneDepth(float2 uv) { return SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv); }
               float GetLinear01Depth(float2 uv) { return Linear01Depth(GetSceneDepth(uv), _ZBufferParams); }
               float GetLinearEyeDepth(float2 uv) { return LinearEyeDepth(GetSceneDepth(uv), _ZBufferParams); } 
            #endif

            float3 GetWorldPositionFromDepthBuffer(float2 uv, float3 worldSpaceViewDir)
            {
               float eye = GetLinearEyeDepth(uv);
               float3 camView = mul((float3x3)GetObjectToWorldMatrix(), transpose(mul(GetWorldToObjectMatrix(), UNITY_MATRIX_I_V)) [2].xyz);

               float dt = dot(worldSpaceViewDir, camView);
               float3 div = worldSpaceViewDir/dt;
               float3 wpos = (eye * div) + GetCameraWorldPosition();
               return wpos;
            }
			
			float3 GetSceneWorldPosition(float2 screenUV, float sceneDepth)
			{
				#if _STANDARD
					float4 clipPos  = float4(screenUV * 2.0f - 1.0f, 0.0f, 1.0f);
					float4 viewPos  = mul(unity_CameraInvProjection, clipPos);
					float3 worldDir = mul((float3x3)UNITY_MATRIX_I_V, viewPos);
					
					return _WorldSpaceCameraPos + worldDir * LinearEyeDepth(sceneDepth);
				#else
					float4 clipPos = float4(screenUV * 2.0 - 1.0, sceneDepth, 1.0);
					
					#if UNITY_UV_STARTS_AT_TOP
						clipPos.y = -clipPos.y;
					#endif
					
					float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
					
					worldPos.xyz /= worldPos.w;
					
					#if _HDRP
						worldPos.xyz = GetAbsolutePositionWS(worldPos.xyz);
					#endif
					
					return worldPos.xyz;
				#endif
			}
			
			float GetSceneWorldDistance(float2 screenUV, float sceneDepth)
			{
				return distance(_WorldSpaceCameraPos, GetSceneWorldPosition(screenUV, sceneDepth));
			}

            #if _HDRP
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return GetAbsolutePositionWS(TransformObjectToWorld(pos));
            }
            #else
            float3 ObjectToWorldSpacePosition(float3 pos)
            {
               return TransformObjectToWorld(pos);
            }
            #endif

            #if _STANDARD
               UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  float4 depthNorms = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture, uv);
                  float3 norms = DecodeViewNormalStereo(depthNorms);
                  norms = mul((float3x3)GetWorldToViewMatrix(), norms) * 0.5 + 0.5;
                  return norms;
               }
            #elif _HDRP && !_DECALSHADER
               
               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  NormalData nd;
                  DecodeFromNormalBuffer(_ScreenSize.xy * uv, nd);
                  return nd.normalWS;
               }
            #elif _URP
               #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
               #endif

               float3 GetSceneNormal(float2 uv, float3 worldSpaceViewDir)
               {
                  #if (SHADER_LIBRARY_VERSION_MAJOR >= 10)
                     return SampleSceneNormals(uv);
                  #else
                     float3 wpos = GetWorldPositionFromDepthBuffer(uv, worldSpaceViewDir);
                     return normalize(-cross(ddx(wpos), ddy(wpos))) * 0.5 + 0.5;
                  #endif

                }
             #endif

             #if _HDRP

               half3 UnpackNormalmapRGorAG(half4 packednormal)
               {
                     // This do the trick
                  packednormal.x *= packednormal.w;

                  half3 normal;
                  normal.xy = packednormal.xy * 2 - 1;
                  normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                  return normal;
               }
               half3 UnpackNormal(half4 packednormal)
               {
                  #if defined(UNITY_NO_DXT5nm)
                     return packednormal.xyz * 2 - 1;
                  #else
                     return UnpackNormalmapRGorAG(packednormal);
                  #endif
               }
            #endif
            #if _HDRP || _URP

               half3 UnpackScaleNormal(half4 packednormal, half scale)
               {
                 #ifndef UNITY_NO_DXT5nm
                   // Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
                   // Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
                   packednormal.x *= packednormal.w;
                 #endif
                   half3 normal;
                   normal.xy = (packednormal.xy * 2 - 1) * scale;
                   normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                   return normal;
               }	

             #endif


            void GetSun(out float3 lightDir, out float3 color)
            {
               lightDir = float3(0.5, 0.5, 0);
               color = 1;
               #if _HDRP
                  if (_DirectionalLightCount > 0)
                  {
                     DirectionalLightData light = _DirectionalLightDatas[0];
                     lightDir = -light.forward.xyz;
                     color = light.color;
                  }
               #elif _STANDARD
			         lightDir = normalize(_WorldSpaceLightPos0.xyz);
                  color = _LightColor0.rgb;
               #elif _URP
	               Light light = GetMainLight();
	               lightDir = light.direction;
	               color = light.color;
               #endif
            }



        CBUFFER_START(UnityPerMaterial)

               
	float    _FlowDensity;
	float2   _FlowSeparationXZ;
	float    _FlowSimulationHeight;
	float    _FlowCameraHeight;
	float2   _FlowCountXZ;
	float4   _FlowCoordU000;
	float4   _FlowCoord0V00;
	float    _FlowSpeed;
	float4x4 _FlowMatrix;

	float _Sink;


	float _Emission;
	
	float _WaveStrengthMin;
	float _WaveStrengthMax;
	float _WaveStrengthScale;

	float _Tiling;
	float _AnimationSpeed;
	float _FoamBrightness;


	half _FlatShadingBlend;


	float _RangeMax;
	float _AlphaStep;
	int _AlphaMaxSteps;
	float _AlphaDepthScale;





        CBUFFER_END

        

        

        
	TEXTURE2D(_FlowDataA);
	SAMPLER(sampler_FlowDataA);
	TEXTURE2D(_FlowDataB);
	SAMPLER(sampler_FlowDataB);
	TEXTURE2D(_FlowDataC);
	SAMPLER(sampler_FlowDataC);
	TEXTURE2D(_FlowDataD);
	SAMPLER(sampler_FlowDataD);
	TEXTURE2D(_FlowDataE);
	SAMPLER(sampler_FlowDataE);
	TEXTURE2D(_FlowDataF);
	SAMPLER(sampler_FlowDataF);

	float4 SGT_O2W(float4 v)
	{
		v = mul(GetObjectToWorldMatrix(), v);
		#if _HDRP
			v.xyz = GetAbsolutePositionWS(v.xyz);
		#endif
		return v;
	}

	float4 SGT_W2O(float4 v)
	{
		#if _HDRP
			v.xyz = GetCameraRelativePositionWS(v.xyz);
		#endif
		return mul(GetWorldToObjectMatrix(), v);
	}

	float4 SGT_O2V(float4 v)
	{
		#if _STANDARD
			return float4(UnityObjectToViewPos(v.xyz), 1.0f);
		#else
			return float4(TransformWorldToView(TransformObjectToWorld(v.xyz)), 1.0f);
		#endif
	}

	float GetFluidHeight(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return groundHeight + fluidDepth;
	}

	float2 GetHeightAndDepth(float2 uv)
	{
		float groundHeight = SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0).r;
		float fluidDepth   = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0).r;

		return float2(groundHeight, fluidDepth);
	}

	Column GetColumn(float2 uv)
	{
		return DecodeColumn(SAMPLE_TEXTURE2D_LOD(_FlowDataA, sampler_FlowDataA, uv, 0), SAMPLE_TEXTURE2D_LOD(_FlowDataB, sampler_FlowDataB, uv, 0));
	}

	Fluid GetColumnFluid(float2 uv)
	{
		float4 c = SAMPLE_TEXTURE2D_LOD(_FlowDataC, sampler_FlowDataC, uv, 0);
		float4 d = SAMPLE_TEXTURE2D_LOD(_FlowDataD, sampler_FlowDataD, uv, 0);
		float4 e = SAMPLE_TEXTURE2D_LOD(_FlowDataE, sampler_FlowDataE, uv, 0);
		float4 f = SAMPLE_TEXTURE2D_LOD(_FlowDataF, sampler_FlowDataF, uv, 0);
		return DecodeFluid(c, d, e, f);
	}

	bool InsideFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y > columnHAD.x && wpos.y < (columnHAD.x + columnHAD.y);
	}

	bool UnderFluid(float3 wpos)
	{
		float2 columnPixel = mul(_FlowMatrix, float4(wpos, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		float2 columnHAD   = GetHeightAndDepth(columnCoord);

		return min(columnCoord.x, columnCoord.y) >= 0.0 && max(columnCoord.x, columnCoord.y) <= 1.0f && wpos.y < (columnHAD.x + columnHAD.y);
	}

	float RayMarchInside(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (InsideFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	float RayMarchUnder(float3 wpos, float3 wdir, float step, int maxs, float maxd)
	{
		float epsi = step * 0.01f;
		float dist = 0.0f;

		for (int i = 0; i < maxs && step > epsi; i++)
		{
			dist += step;

			float3 pos = wpos + wdir * dist;

			if (UnderFluid(pos) == false)
			{
				dist -= step;
				step *= 0.5f;
			}

			if (dist > maxd)
			{
				return maxd;
			}
		}

		return dist;
	}

	void Ext_ModifyVertex0 (inout VertexData v, inout ExtraV2F e)
	{
		float4 wpos        = SGT_O2W(v.vertex);
		float2 columnPixel = mul(_FlowMatrix, float4(wpos.xyz, 1.0f)).xy;
		float2 columnCoord = SnapCoordFromPixel(round(columnPixel), _FlowCountXZ);

		Column column0 = GetColumn(columnCoord);
		Column columnL = GetColumn(columnCoord - _FlowCoordU000.xy);
		Column columnR = GetColumn(columnCoord + _FlowCoordU000.xy);
		Column columnB = GetColumn(columnCoord - _FlowCoord0V00.xy);
		Column columnT = GetColumn(columnCoord + _FlowCoord0V00.xy);

		Fluid fluid0 = GetColumnFluid(columnCoord);
		Fluid fluidL = GetColumnFluid(columnCoord - _FlowCoordU000.xy);
		Fluid fluidR = GetColumnFluid(columnCoord + _FlowCoordU000.xy);
		Fluid fluidB = GetColumnFluid(columnCoord - _FlowCoord0V00.xy);
		Fluid fluidT = GetColumnFluid(columnCoord + _FlowCoord0V00.xy);

		float ww = fluidL.Depth + fluidR.Depth + fluidB.Depth + fluidT.Depth + 0.001f;
		float wL = fluidL.Depth / ww;
		float wR = fluidR.Depth / ww;
		float wB = fluidB.Depth / ww;
		float wT = fluidT.Depth / ww;
		float w0 = saturate(fluid0.Depth * 10);

		float hL = columnL.GroundHeight + fluidL.Depth;
		float hR = columnR.GroundHeight + fluidR.Depth;
		float hB = columnB.GroundHeight + fluidB.Depth;
		float hT = columnT.GroundHeight + fluidT.Depth;
		float hh = hL * wL + hR * wR + hB * wB + hT * wT;
		float h0 = column0.GroundHeight + fluid0.Depth;

		hh = lerp(hh, h0 - _Sink, saturate(0.01f / ww)); // Prevent skirts going down too far

		Fluid fluid = fluid0;

		fluid.Depth = lerp(fluidL.Depth * wL + fluidR.Depth * wR + fluidB.Depth * wB + fluidT.Depth * wT, fluid.Depth, w0);
		fluid.RGBA  = lerp(fluidL.RGBA  * wL + fluidR.RGBA  * wR + fluidB.RGBA  * wB + fluidT.RGBA  * wT, fluid.RGBA , w0);
		fluid.ESMV  = lerp(fluidL.ESMV  * wL + fluidR.ESMV  * wR + fluidB.ESMV  * wB + fluidT.ESMV  * wT, fluid.ESMV , w0);
		fluid.F123  = lerp(fluidL.F123  * wL + fluidR.F123  * wR + fluidB.F123  * wB + fluidT.F123  * wT, fluid.F123 , w0);

		e.blackboard.groundHeight  = column0.GroundHeight;
		e.blackboard.surfaceHeight = lerp(hh, h0, w0);
		e.blackboard.surfaceNormal = normalize(float3((hL - hR) / _FlowSeparationXZ.x, 2.0f, (hB - hT) / _FlowSeparationXZ.y));
		e.blackboard.fluid         = fluid;
	}


	TEXTURE2D(_NoiseMap);
	SAMPLER(sampler_NoiseMap);

	TEXTURE2D(_NormalFoamMap);
	SAMPLER(sampler_NormalFoamMap);

	void Ext_ModifyVertex3 (inout VertexData v, inout ExtraV2F e)
	{
		v.vertex.y = lerp(e.blackboard.groundHeight, e.blackboard.surfaceHeight, v.texcoord0.w) - _FlowSimulationHeight;

		v.normal = lerp(e.blackboard.surfaceNormal, v.normal, v.texcoord0.z);

		v.vertexColor = e.blackboard.fluid.RGBA;

		e.extraV2F0.xyz = SGT_O2V(v.vertex).xyz;
		e.extraV2F0.w   = e.blackboard.fluid.Depth;

		e.extraV2F1.xyz = e.blackboard.fluid.ESMV.xyz;
		e.extraV2F1.w = e.blackboard.fluid.F123.x;
	}

	float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float tiling, float time, float phaseOffset)
	{
		float progress = frac(time + phaseOffset);
		float3 uvw;
		uvw.xy = uv - flowVector * (progress - 0.5f);
		uvw.xy *= tiling;
		uvw.xy += phaseOffset;
		uvw.xy += (time - progress) * jump;
		uvw.z = 1 - abs(1 - 2 * progress);
		return uvw;
	}

	float3 CombineNormals(float3 n1, float3 n2)
	{
		return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
	}

	float3 UnpackNormalAndScale(float2 xy, float scale)
	{
		xy = xy * 2.0f - 1.0f; xy *= scale; return float3(xy, sqrt(1.0 - saturate(dot(xy, xy))));
	}

	float4 SampleStochastic(float2 uv, float noise)
	{
		float cur_height = uv.y + noise;
		float this_index = floor(cur_height);
		float next_index = this_index + 1.0f;

		float2 uvA     = uv + sin(float2(1.0f, 2.0f) * this_index);
		float2 uvB     = uv + sin(float2(1.0f, 2.0f) * next_index);
		float2 gradX   = ddx(uv);
		float2 gradY   = ddy(uv);

		float4 sampleA = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvA, gradX, gradY);
		float4 sampleB = SAMPLE_TEXTURE2D_GRAD(_NormalFoamMap, sampler_NormalFoamMap, uvB, gradX, gradY);

		return lerp(sampleA, sampleB, cur_height - this_index);
	}

	void Ext_SurfaceFunction3 (inout Surface o, inout ShaderData d)
	{
		d.blackboard.fluid.RGBA     = d.vertexColor;
		d.blackboard.fluid.Depth    = d.extraV2F0.w;
		d.blackboard.fluid.ESMV.xyz = d.extraV2F1.xyz;
		d.blackboard.fluid.F123.x   = d.extraV2F1.w;

		if (d.blackboard.fluid.Depth < 0.01f)
		{
			discard;
		}

		float2 uv          = d.worldSpacePosition.xz;
		float2 columnPixel = mul(_FlowMatrix, float4(d.worldSpacePosition, 1.0f)).xy;
		float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);
		Column column      = GetColumn(columnCoord);

		float  time = _Time.y * _AnimationSpeed;
		float2 jump = float2(0.13f, 0.17f);
		float2 fvec = (column.Outflow.yw - column.Outflow.xz) * _FlowSeparationXZ * _FlowSpeed;
		float  fmag = log10(1.0f + length(fvec)) * 0.1f;

		float3 flowA = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.0f);
		float3 flowB = FlowUVW(uv, fvec / _AnimationSpeed, jump, _Tiling, time, 0.5f);

		#if _STOCHASTIC_ON
			float  noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 0.0025f).a * 4.0f;
			float4 nfmA  = SampleStochastic(flowA, noise);
			float4 nfmB  = SampleStochastic(flowB, noise);
		#else
			float4 nfmA = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowA.xy);
			float4 nfmB = SAMPLE_TEXTURE2D(_NormalFoamMap, sampler_NormalFoamMap, flowB.xy);
		#endif

		// Water normals
		float  normalStr    = lerp(_WaveStrengthMin, _WaveStrengthMax, saturate(_WaveStrengthScale * fmag));
		float3 waterNormalA = UnpackNormalAndScale(nfmA.xy, flowA.z * normalStr);
		float3 waterNormalB = UnpackNormalAndScale(nfmB.xy, flowB.z * normalStr);
		float3 waterNormals = CombineNormals(waterNormalA, waterNormalB);

		o.Normal   = lerp(waterNormals, o.Normal, d.texcoord0.z);
		o.Albedo   = d.blackboard.fluid.RGBA.xyz;
		o.Emission = d.blackboard.fluid.ESMV.x * _Emission * o.Albedo;

		#if _FOAM_OFF
			d.blackboard.foam = 0.0f;
		#else
			d.blackboard.foam = d.blackboard.fluid.F123.x * (1.0f - d.texcoord0.z);

			#if _FOAM_ALPHA
				float3 foamAlbedo = (nfmA.w * flowA.z + nfmB.w * flowB.z) * _FoamBrightness;
			#elif _FOAM_CUTOUT
				float3 foamAlbedo = _FoamBrightness;
				float  foamRamp   = nfmA.w * flowA.z + nfmB.w * flowB.z;
				d.blackboard.foam = saturate((d.blackboard.foam - foamRamp) * 10.0f);
			#endif

			float3 foamNormal = float3(0.0f, 0.0f, 1.0f);

			o.Albedo = lerp(o.Albedo, foamAlbedo, d.blackboard.foam);
			o.Normal = lerp(o.Normal, foamNormal, d.blackboard.foam);
		#endif

		o.Smoothness = d.blackboard.fluid.ESMV.y;
		o.Metallic   = d.blackboard.fluid.ESMV.z;
	}


	void Ext_SurfaceFunction4 (inout Surface o, inout ShaderData d)
	{
	#if _FACETED_ON
		// lets just affect the TBN data, so we flat shade the original polygons, not the normal map
		float3 dx = ddx(d.worldSpacePosition);
		float3 dy = ddy(d.worldSpacePosition);
		float3 worldNormal = normalize(cross(dy, dx));
		worldNormal = lerp(d.worldSpaceNormal, worldNormal, _FlatShadingBlend);
		d.worldSpaceNormal = worldNormal;
		d.TBNMatrix[2] = worldNormal;
	#endif
	}


	void Ext_SurfaceFunction5 (inout Surface o, ShaderData d)
	{
		#if _ALPHA_OFF
		#else
			float3 position  = d.worldSpacePosition;
			float3 step      = d.worldSpacePosition - _WorldSpaceCameraPos;
			float2 bentScrUV = d.screenUV + o.Normal.xy * 0.05f * log(1.0f + d.blackboard.fluid.Depth * 1.0f);
			float  distMax   = max(_RangeMax * 0.001f, _RangeMax * (1.0f - d.blackboard.fluid.RGBA.w));
			float  distRange = distMax;
			float  vertDist  = length(d.extraV2F0.xyz);
			float  camtDist  = GetLinearEyeDepth(bentScrUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);

			if (camtDist < vertDist)
			{
				camtDist = GetLinearEyeDepth(d.screenUV) * length(d.extraV2F0.xyz / d.extraV2F0.z);
			}

			float diffDist = max(0.0f, camtDist - vertDist);

			// The depth buffer becomes unusable at certain ranges, so fade it out
			diffDist += max(0.0f, camtDist * _AlphaDepthScale - _RangeMax);
			distRange = min(distRange, diffDist);

			#if _ALPHA_VERTICAL
				float dist = d.blackboard.fluid.Depth;
			#elif _ALPHA_DEPTH
				float dist = diffDist;
			#elif _ALPHA_MARCH_FLUID
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distMax);
			#elif _ALPHA_MARCH_FLUID_AND_DEPTH
				float dist = RayMarchInside(position, normalize(step), _AlphaStep, _AlphaMaxSteps, distRange);
			#endif

			// Set opacity based on distance through fluid relative to the maximum distance, and make sure high opacity fluids become solid
			float opacity = saturate(dist / distMax + d.blackboard.foam + pow(d.blackboard.fluid.RGBA.w, 10.0f));

			o.Albedo    = lerp(0.0f, o.Albedo, opacity);
			o.Emission  = lerp(GetSceneColor(bentScrUV), o.Emission, opacity);
			//o.Emission += o.Albedo * d.blackboard.fluid.ESMV.x * opacity;
		#endif
	}





        
            void ChainSurfaceFunction(inout Surface l, inout ShaderData d)
            {
                 // Ext_SurfaceFunction0(l, d);
                 // Ext_SurfaceFunction1(l, d);
                 // Ext_SurfaceFunction2(l, d);
                  Ext_SurfaceFunction3(l, d);
                  Ext_SurfaceFunction4(l, d);
                  Ext_SurfaceFunction5(l, d);
                 // Ext_SurfaceFunction6(l, d);
                 // Ext_SurfaceFunction7(l, d);
                 // Ext_SurfaceFunction8(l, d);
                 // Ext_SurfaceFunction9(l, d);
		           // Ext_SurfaceFunction10(l, d);
                 // Ext_SurfaceFunction11(l, d);
                 // Ext_SurfaceFunction12(l, d);
                 // Ext_SurfaceFunction13(l, d);
                 // Ext_SurfaceFunction14(l, d);
                 // Ext_SurfaceFunction15(l, d);
                 // Ext_SurfaceFunction16(l, d);
                 // Ext_SurfaceFunction17(l, d);
                 // Ext_SurfaceFunction18(l, d);
		           // Ext_SurfaceFunction19(l, d);
                 // Ext_SurfaceFunction20(l, d);
                 // Ext_SurfaceFunction21(l, d);
                 // Ext_SurfaceFunction22(l, d);
                 // Ext_SurfaceFunction23(l, d);
                 // Ext_SurfaceFunction24(l, d);
                 // Ext_SurfaceFunction25(l, d);
                 // Ext_SurfaceFunction26(l, d);
                 // Ext_SurfaceFunction27(l, d);
                 // Ext_SurfaceFunction28(l, d);
		           // Ext_SurfaceFunction29(l, d);
            }

#if !_DECALSHADER

            void ChainModifyVertex(inout VertexData v, inout VertexToPixel v2p, float4 time)
            {
                 ExtraV2F d;
                 
                 ZERO_INITIALIZE(ExtraV2F, d);
                 ZERO_INITIALIZE(Blackboard, d.blackboard);
                 // due to motion vectors in HDRP, we need to use the last
                 // time in certain spots. So if you are going to use _Time to adjust vertices,
                 // you need to use this time or motion vectors will break. 
                 d.time = time;

                   Ext_ModifyVertex0(v, d);
                 // Ext_ModifyVertex1(v, d);
                 // Ext_ModifyVertex2(v, d);
                  Ext_ModifyVertex3(v, d);
                 // Ext_ModifyVertex4(v, d);
                 // Ext_ModifyVertex5(v, d);
                 // Ext_ModifyVertex6(v, d);
                 // Ext_ModifyVertex7(v, d);
                 // Ext_ModifyVertex8(v, d);
                 // Ext_ModifyVertex9(v, d);
                 // Ext_ModifyVertex10(v, d);
                 // Ext_ModifyVertex11(v, d);
                 // Ext_ModifyVertex12(v, d);
                 // Ext_ModifyVertex13(v, d);
                 // Ext_ModifyVertex14(v, d);
                 // Ext_ModifyVertex15(v, d);
                 // Ext_ModifyVertex16(v, d);
                 // Ext_ModifyVertex17(v, d);
                 // Ext_ModifyVertex18(v, d);
                 // Ext_ModifyVertex19(v, d);
                 // Ext_ModifyVertex20(v, d);
                 // Ext_ModifyVertex21(v, d);
                 // Ext_ModifyVertex22(v, d);
                 // Ext_ModifyVertex23(v, d);
                 // Ext_ModifyVertex24(v, d);
                 // Ext_ModifyVertex25(v, d);
                 // Ext_ModifyVertex26(v, d);
                 // Ext_ModifyVertex27(v, d);
                 // Ext_ModifyVertex28(v, d);
                 // Ext_ModifyVertex29(v, d);


                 // #if %EXTRAV2F0REQUIREKEY%
                  v2p.extraV2F0 = d.extraV2F0;
                 // #endif

                 // #if %EXTRAV2F1REQUIREKEY%
                  v2p.extraV2F1 = d.extraV2F1;
                 // #endif

                 // #if %EXTRAV2F2REQUIREKEY%
                 // v2p.extraV2F2 = d.extraV2F2;
                 // #endif

                 // #if %EXTRAV2F3REQUIREKEY%
                 // v2p.extraV2F3 = d.extraV2F3;
                 // #endif

                 // #if %EXTRAV2F4REQUIREKEY%
                 // v2p.extraV2F4 = d.extraV2F4;
                 // #endif

                 // #if %EXTRAV2F5REQUIREKEY%
                 // v2p.extraV2F5 = d.extraV2F5;
                 // #endif

                 // #if %EXTRAV2F6REQUIREKEY%
                 // v2p.extraV2F6 = d.extraV2F6;
                 // #endif

                 // #if %EXTRAV2F7REQUIREKEY%
                 // v2p.extraV2F7 = d.extraV2F7;
                 // #endif
            }

            void ChainModifyTessellatedVertex(inout VertexData v, inout VertexToPixel v2p)
            {
               ExtraV2F d;
               ZERO_INITIALIZE(ExtraV2F, d);
               ZERO_INITIALIZE(Blackboard, d.blackboard);

               // #if %EXTRAV2F0REQUIREKEY%
                d.extraV2F0 = v2p.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                d.extraV2F1 = v2p.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // d.extraV2F2 = v2p.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // d.extraV2F3 = v2p.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // d.extraV2F4 = v2p.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // d.extraV2F5 = v2p.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // d.extraV2F6 = v2p.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // d.extraV2F7 = v2p.extraV2F7;
               // #endif


               // Ext_ModifyTessellatedVertex0(v, d);
               // Ext_ModifyTessellatedVertex1(v, d);
               // Ext_ModifyTessellatedVertex2(v, d);
               // Ext_ModifyTessellatedVertex3(v, d);
               // Ext_ModifyTessellatedVertex4(v, d);
               // Ext_ModifyTessellatedVertex5(v, d);
               // Ext_ModifyTessellatedVertex6(v, d);
               // Ext_ModifyTessellatedVertex7(v, d);
               // Ext_ModifyTessellatedVertex8(v, d);
               // Ext_ModifyTessellatedVertex9(v, d);
               // Ext_ModifyTessellatedVertex10(v, d);
               // Ext_ModifyTessellatedVertex11(v, d);
               // Ext_ModifyTessellatedVertex12(v, d);
               // Ext_ModifyTessellatedVertex13(v, d);
               // Ext_ModifyTessellatedVertex14(v, d);
               // Ext_ModifyTessellatedVertex15(v, d);
               // Ext_ModifyTessellatedVertex16(v, d);
               // Ext_ModifyTessellatedVertex17(v, d);
               // Ext_ModifyTessellatedVertex18(v, d);
               // Ext_ModifyTessellatedVertex19(v, d);
               // Ext_ModifyTessellatedVertex20(v, d);
               // Ext_ModifyTessellatedVertex21(v, d);
               // Ext_ModifyTessellatedVertex22(v, d);
               // Ext_ModifyTessellatedVertex23(v, d);
               // Ext_ModifyTessellatedVertex24(v, d);
               // Ext_ModifyTessellatedVertex25(v, d);
               // Ext_ModifyTessellatedVertex26(v, d);
               // Ext_ModifyTessellatedVertex27(v, d);
               // Ext_ModifyTessellatedVertex28(v, d);
               // Ext_ModifyTessellatedVertex29(v, d);

               // #if %EXTRAV2F0REQUIREKEY%
                v2p.extraV2F0 = d.extraV2F0;
               // #endif

               // #if %EXTRAV2F1REQUIREKEY%
                v2p.extraV2F1 = d.extraV2F1;
               // #endif

               // #if %EXTRAV2F2REQUIREKEY%
               // v2p.extraV2F2 = d.extraV2F2;
               // #endif

               // #if %EXTRAV2F3REQUIREKEY%
               // v2p.extraV2F3 = d.extraV2F3;
               // #endif

               // #if %EXTRAV2F4REQUIREKEY%
               // v2p.extraV2F4 = d.extraV2F4;
               // #endif

               // #if %EXTRAV2F5REQUIREKEY%
               // v2p.extraV2F5 = d.extraV2F5;
               // #endif

               // #if %EXTRAV2F6REQUIREKEY%
               // v2p.extraV2F6 = d.extraV2F6;
               // #endif

               // #if %EXTRAV2F7REQUIREKEY%
               // v2p.extraV2F7 = d.extraV2F7;
               // #endif
            }

            void ChainFinalColorForward(inout Surface l, inout ShaderData d, inout half4 color)
            {
               //   Ext_FinalColorForward0(l, d, color);
               //   Ext_FinalColorForward1(l, d, color);
               //   Ext_FinalColorForward2(l, d, color);
               //   Ext_FinalColorForward3(l, d, color);
               //   Ext_FinalColorForward4(l, d, color);
               //   Ext_FinalColorForward5(l, d, color);
               //   Ext_FinalColorForward6(l, d, color);
               //   Ext_FinalColorForward7(l, d, color);
               //   Ext_FinalColorForward8(l, d, color);
               //   Ext_FinalColorForward9(l, d, color);
               //  Ext_FinalColorForward10(l, d, color);
               //  Ext_FinalColorForward11(l, d, color);
               //  Ext_FinalColorForward12(l, d, color);
               //  Ext_FinalColorForward13(l, d, color);
               //  Ext_FinalColorForward14(l, d, color);
               //  Ext_FinalColorForward15(l, d, color);
               //  Ext_FinalColorForward16(l, d, color);
               //  Ext_FinalColorForward17(l, d, color);
               //  Ext_FinalColorForward18(l, d, color);
               //  Ext_FinalColorForward19(l, d, color);
               //  Ext_FinalColorForward20(l, d, color);
               //  Ext_FinalColorForward21(l, d, color);
               //  Ext_FinalColorForward22(l, d, color);
               //  Ext_FinalColorForward23(l, d, color);
               //  Ext_FinalColorForward24(l, d, color);
               //  Ext_FinalColorForward25(l, d, color);
               //  Ext_FinalColorForward26(l, d, color);
               //  Ext_FinalColorForward27(l, d, color);
               //  Ext_FinalColorForward28(l, d, color);
               //  Ext_FinalColorForward29(l, d, color);
            }

            void ChainFinalGBufferStandard(inout Surface s, inout ShaderData d, inout half4 GBuffer0, inout half4 GBuffer1, inout half4 GBuffer2, inout half4 outEmission, inout half4 outShadowMask)
            {
               //   Ext_FinalGBufferStandard0(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard1(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard2(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard3(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard4(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard5(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard6(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard7(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard8(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //   Ext_FinalGBufferStandard9(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard10(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard11(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard12(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard13(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard14(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard15(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard16(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard17(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard18(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard19(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard20(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard21(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard22(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard23(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard24(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard25(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard26(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard27(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard28(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
               //  Ext_FinalGBufferStandard29(s, d, GBuffer0, GBuffer1, GBuffer2, outEmission, outShadowMask);
            }
#endif


        


#if _DECALSHADER

        ShaderData CreateShaderData(SurfaceDescriptionInputs IN)
        {
            ShaderData d = (ShaderData)0;
            d.TBNMatrix = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
            d.worldSpaceNormal = IN.WorldSpaceNormal;
            d.worldSpaceTangent = IN.WorldSpaceTangent;

            d.worldSpacePosition = IN.WorldSpacePosition;
            d.texcoord0 = IN.uv0.xyxy;
            d.screenPos = IN.ScreenPosition;

            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - d.worldSpacePosition);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(d.worldSpacePosition), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(d.worldSpacePosition, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), d.worldSpaceTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenUV = (IN.ScreenPosition.xy / max(0.01, IN.ScreenPosition.w));
            // #endif

            return d;
        }
#else

         ShaderData CreateShaderData(VertexToPixel i
                  #if NEED_FACING
                     , bool facing
                  #endif
         )
         {
            ShaderData d = (ShaderData)0;
            d.clipPos = i.pos;
            d.worldSpacePosition = i.worldPos;

            d.worldSpaceNormal = normalize(i.worldNormal);
            d.worldSpaceTangent.xyz = normalize(i.worldTangent.xyz);

            d.tangentSign = i.worldTangent.w * unity_WorldTransformParams.w;
            float3 bitangent = cross(d.worldSpaceTangent.xyz, d.worldSpaceNormal) * d.tangentSign;
           
            d.TBNMatrix = float3x3(d.worldSpaceTangent, -bitangent, d.worldSpaceNormal);
            d.worldSpaceViewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

            d.tangentSpaceViewDir = mul(d.TBNMatrix, d.worldSpaceViewDir);
             d.texcoord0 = i.texcoord0;
            // d.texcoord1 = i.texcoord1;
            // d.texcoord2 = i.texcoord2;

            // #if %TEXCOORD3REQUIREKEY%
            // d.texcoord3 = i.texcoord3;
            // #endif

            // d.isFrontFace = facing;
            // #if %VERTEXCOLORREQUIREKEY%
             d.vertexColor = i.vertexColor;
            // #endif

            // these rarely get used, so we back transform them. Usually will be stripped.
            #if _HDRP
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(GetCameraRelativePositionWS(i.worldPos), 1)).xyz;
            #else
                // d.localSpacePosition = mul(GetWorldToObjectMatrix(), float4(i.worldPos, 1)).xyz;
            #endif
            // d.localSpaceNormal = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldNormal));
            // d.localSpaceTangent = normalize(mul((float3x3)GetWorldToObjectMatrix(), i.worldTangent.xyz));

            // #if %SCREENPOSREQUIREKEY%
             d.screenPos = i.screenPos;
             d.screenUV = (i.screenPos.xy / i.screenPos.w);
            // #endif


            // #if %EXTRAV2F0REQUIREKEY%
             d.extraV2F0 = i.extraV2F0;
            // #endif

            // #if %EXTRAV2F1REQUIREKEY%
             d.extraV2F1 = i.extraV2F1;
            // #endif

            // #if %EXTRAV2F2REQUIREKEY%
            // d.extraV2F2 = i.extraV2F2;
            // #endif

            // #if %EXTRAV2F3REQUIREKEY%
            // d.extraV2F3 = i.extraV2F3;
            // #endif

            // #if %EXTRAV2F4REQUIREKEY%
            // d.extraV2F4 = i.extraV2F4;
            // #endif

            // #if %EXTRAV2F5REQUIREKEY%
            // d.extraV2F5 = i.extraV2F5;
            // #endif

            // #if %EXTRAV2F6REQUIREKEY%
            // d.extraV2F6 = i.extraV2F6;
            // #endif

            // #if %EXTRAV2F7REQUIREKEY%
            // d.extraV2F7 = i.extraV2F7;
            // #endif

            return d;
         }

#endif

        
         #if defined(_PASSSHADOW)
            float3 _LightDirection;
            float3 _LightPosition;
         #endif

         #if (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))

            #define GetWorldToViewMatrix()     _ViewMatrix
            #define UNITY_MATRIX_I_V   _InvViewMatrix
            #define GetViewToHClipMatrix()     OptimizeProjectionMatrix(_ProjMatrix)
            #define UNITY_MATRIX_I_P   _InvProjMatrix
            #define GetWorldToHClipMatrix()    _ViewProjMatrix
            #define UNITY_MATRIX_I_VP  _InvViewProjMatrix
            #define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
            #define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix
            #define UNITY_MATRIX_PREV_I_VP _PrevInvViewProjMatrix

            void MotionVectorPositionZBias(VertexToPixel input)
            {
                #if UNITY_REVERSED_Z
                input.pos.z -= unity_MotionVectorsParams.z * input.pos.w;
                #else
                input.pos.z += unity_MotionVectorsParams.z * input.pos.w;
                #endif
            }

        #endif

         // vertex shader
         VertexToPixel Vert (VertexData v)
         {
           VertexToPixel o = (VertexToPixel)0;

           UNITY_SETUP_INSTANCE_ID(v);
           UNITY_TRANSFER_INSTANCE_ID(v, o);
           UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            
           #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
             VertexData previousMesh = v;
           #endif
           #if !_TESSELLATION_ON
             ChainModifyVertex(v, o, _Time);
           #endif

            o.texcoord0 = v.texcoord0;
           // o.texcoord1 = v.texcoord1;
           // o.texcoord2 = v.texcoord2;

           // #if %TEXCOORD3REQUIREKEY%
           // o.texcoord3 = v.texcoord3;
           // #endif

           // #if %VERTEXCOLORREQUIREKEY%
            o.vertexColor = v.vertexColor;
           // #endif

           // This return the camera relative position (if enable)
           float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
           float3 normalWS = TransformObjectToWorldNormal(v.normal);
           float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
           
           VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
           o.worldPos = positionWS;
           o.worldNormal = normalWS;
           o.worldTangent = tangentWS;


          // For some very odd reason, in 2021.2, we can't use Unity's defines, but have to use our own..
          #if _PASSSHADOW
              #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                 float3 lightDirectionWS = normalize(_LightPosition - o.worldPos);
              #else
                 float3 lightDirectionWS = _LightDirection;
              #endif
              // Define shadow pass specific clip position for Universal
              o.pos = TransformWorldToHClip(ApplyShadowBias(o.worldPos, o.worldNormal, lightDirectionWS));
              #if UNITY_REVERSED_Z
                  o.pos.z = min(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #else
                  o.pos.z = max(o.pos.z, UNITY_NEAR_CLIP_VALUE);
              #endif
          #elif _PASSMETA
              o.pos = MetaVertexPosition(float4(v.vertex.xyz, 0), v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #else
              o.pos = TransformWorldToHClip(o.worldPos);
          #endif

          // #if %SCREENPOSREQUIREKEY%
           o.screenPos = ComputeScreenPos(o.pos, _ProjectionParams.x);
          // #endif

          
          #if _PASSFORWARD || _PASSGBUFFER
              float2 uv1 = v.texcoord1.xy;
              OUTPUT_LIGHTMAP_UV(uv1, unity_LightmapST, o.lightmapUV);
              // o.texcoord1.xy = uv1;
              OUTPUT_SH(o.worldNormal, o.sh);
              
              #if defined(DYNAMICLIGHTMAP_ON)
                   o.dynamicLightmapUV.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                   #if UNITY_VERSION >= 60000009
                     OUTPUT_SH(o.worldNormal, o.sh);
                   #endif
              #elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && UNITY_VERSION >= 60000009
                   OUTPUT_SH4(vertexInput.positionWS, o.worldNormal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), o.sh, o.probeOcclusion);
              #endif
          #endif

          #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
              half fogFactor = 0;
              #if defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(o.pos.z);
              #endif
              #if _BAKEDLIT
                 o.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);
              #else
                 half3 vertexLight = VertexLighting(o.worldPos, o.worldNormal);
                 o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
              #endif
          #endif

          #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             o.shadowCoord = GetShadowCoord(vertexInput);
          #endif

          #if _URP && (_PASSMOTIONVECTOR || ((_PASSFORWARD || _PASSUNLIT) && defined(_WRITE_TRANSPARENT_MOTION_VECTOR)))
            #if !defined(TESSELLATION_ON)
              MotionVectorPositionZBias(o);
            #endif

            o.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
            // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
            bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

            if (!forceNoMotion)
            {
              #if defined(HAVE_VFX_MODIFICATION)
                float3 previousPositionOS = currentFrameMvData.vfxParticlePositionOS;
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  const bool applyDeformation = false;
                #else
                  const bool applyDeformation = true;
                #endif
              #else
                const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
                float3 previousPositionOS = hasDeformation ? previousMesh.previousPositionOS : previousMesh.vertex.xyz;

                #if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
                  const bool applyDeformation = true;
                #else
                  const bool applyDeformation = hasDeformation;
                #endif
              #endif
              // TODO
              #if defined(FEATURES_GRAPH_VERTEX)
                if (applyDeformation)
                  previousPositionOS = GetLastFrameDeformedPosition(previousMesh, currentFrameMvData, previousPositionOS);
                else
                  previousPositionOS = previousMesh.positionOS;

                #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT)
                  previousPositionOS -= previousMesh.precomputedVelocity;
                #endif
              #endif

              #if defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(DOTS_DEFORMED)
                // Deformed vertices in DOTS are not cumulative with built-in Unity skinning/blend shapes
                // Needs to be called after vertex modification has been applied otherwise it will be
                // overwritten by Compute Deform node
                ApplyPreviousFrameDeformedVertexPosition(previousMesh.vertexID, previousPositionOS);
              #endif
              #if defined (_ADD_PRECOMPUTED_VELOCITY)
                previousPositionOS -= previousMesh.precomputedVelocity;
              #endif
              o.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(positionWS, 1.0f));

              #if defined(HAVE_VFX_MODIFICATION)
                #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
                  #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT) || defined(_ADD_PRECOMPUTED_VELOCITY)
                    #error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
                  #endif
                  o.previousPositionCS = VFXGetPreviousClipPosition(previousMesh, currentFrameMvData.vfxElementAttributes, o.positionCS);
                #else
                  #if VFX_WORLD_SPACE
                    //previousPositionOS is already in world space
                    const float3 previousPositionWS = previousPositionOS;
                  #else
                    const float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz;
                  #endif
                  o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionWS, 1.0f));
                #endif
              #else
                o.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1)));
              #endif
            }
          #endif

          return o;
         }


        
        
        // Using parts of com.unity.render-pipelines.universal\Editor\ShaderGraph\Includes\MotionVectorPass.hlsl
        //                com.unity.render-pipelines.universal\ShaderLibrary\MotionVectorsCommon.hlsl
        //                com.unity.render-pipelines.universal\Editor\ShaderGraph\Includes\Varyings.hlsl

        float2 CalcNdcMotionVectorFromCsPositions(float4 posCS, float4 prevPosCS)
        {
          // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
          bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
          if (forceNoMotion)
            return float2(0.0, 0.0);

          // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
          // since uv remap functions use floats
          float2 posNDC = posCS.xy * rcp(posCS.w);
          float2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

          float2 velocity;
          #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            {
              // Convert velocity from NDC space (-1..1) to screen UV 0..1 space since FoveatedRendering remap needs that range.
              float2 posUV = RemapFoveatedRenderingResolve(posNDC * 0.5 + 0.5);
              float2 prevPosUV = RemapFoveatedRenderingPrevFrameLinearToNonUniform(prevPosNDC * 0.5 + 0.5);

              // Calculate forward velocity
              velocity = (posUV - prevPosUV);
              #if UNITY_UV_STARTS_AT_TOP
                velocity.y = -velocity.y;
              #endif
            }
            else
          #endif
            {
              // Calculate forward velocity
              velocity = (posNDC.xy - prevPosNDC.xy);
              #if UNITY_UV_STARTS_AT_TOP
                velocity.y = -velocity.y;
              #endif

              // Convert velocity from NDC space (-1..1) to UV 0..1 space
              // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
              // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
              velocity.xy *= 0.5;
            }

          return velocity;
        }

        float4 Frag(
          VertexToPixel input) : SV_Target
          {
            UNITY_SETUP_INSTANCE_ID(input);

            float4 motionVector = float4(CalcNdcMotionVectorFromCsPositions(input.positionCS, input.previousPositionCS), 0, 0);
    
            return motionVector;
          }

        ENDHLSL
        }
      










      

   }
   
   
   
}
