// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/BasicRayMarcher"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}	
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"			

			// Provided by script
			uniform float4x4  _FrustrumCornersES;
			uniform sampler2D _MainTex;
			uniform float4	  _MainTex_TexelSize;
			uniform float4x4  _CameraInvViewMatrix;
			uniform float3    _CameraWS;
			uniform float3    _LightDir; 
			uniform sampler2D _CameraDepthTexture;
			uniform float4x4  _MatTorus_InvModel;
			uniform sampler2D _ColorRamp;
			uniform int       _DebugMode;
			uniform sampler2D _PerfRamp;

			#include "Assets/Shaders/Common/Raymarch.cginc"

			// Input to vertex shader
			struct appdata 
			{
				// z value here contains the index of
				// _FrustrumCornerES to use
				float4 vertex: POSITION;
				float2 uv:     TEXCOORD0;
			};

			// Output of vertex shader / input to fragment shader
			struct v2f
			{
				float4 pos: SV_POSITION;
				float2 uv:  TEXCOORD0;
				float4 ray: TEXCOORD1;  
			};

			v2f vert (appdata v) {
				v2f o;

				// Index passed via custom blit function in RaymarchFilter.cs
				half index = v.vertex.z;
				v.vertex.z = 0.1;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0) {
					o.uv.y = 1 - o.uv.y;
				}
				#endif

				// Get the eyespace view ray (normalized)
				// 0 is dummy value. Non-square matrices are not supported on gles
				o.ray = float4(_FrustrumCornersES[(int)index].xyz, 0);

				// Dividing by z "normalizes" it in the z axis
				// Therefore multiplying the ray by some number i
				// gives the viewspace position of the point on
				// the ray with [viewspace z] = i
				o.ray /= abs(o.ray.z);

				// Transform the ray from eyespace to worldspace
				// Note: _CameraInvViewMatrix was provided by the script
				o.ray = mul(_CameraInvViewMatrix, o.ray);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {				 
				// ray direction 
				float3 rd = normalize(i.ray.xyz);
				// ray origin (camera position)
				float3 ro = _CameraWS;

				float2 duv = i.uv;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0) {
					duv.y = 1 - duv.y;
				}
				#endif

				// Convert from depth buffer (eye space) to true distance
				// from camera. This is done by multiplying the eyespace
				// depth by the length of the "z-normalized" ray (see vert()).
				// Think of similar triangles: the view-space z-distance
				// between a point and the camera is proportional to the
				// absolute distance.
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, duv).r);
				depth *= length(i.ray.xyz);

				// Color of the scene before this shader was run
				fixed3 col = _DebugMode == 1 ? tex2D(_PerfRamp, i.uv) : tex2D(_MainTex, i.uv);   
				//fixed4 add = raymarch(ro, rd, depth);
				fixed4 add = _DebugMode == 1 ? raymarchPerfTest(ro, rd, depth) : raymarch(ro, rd, depth);   
				 
				// Returns final color using alpha blending
				return fixed4(col*(1.0 - add.w) + add.xyz * add.w,1.0);				
			}
			ENDCG
		}
	}
}
