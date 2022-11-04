Shader "Unlit/BoidVortex"
{
	Properties
	{
		_Forward ("Forward", Vector) = (0, 0, 0)
		_Align ("Align", Vector) = (1, 0, 0)

		_AlignedCol ("AlignedCol", Color) = (1, 1, 1, 1)
		_MisalignedCol ("MisAlignedCol", Color) = (1, 1, 1, 1)
		_BkgCol ("Background", Color) = (1, 1, 1, 1)
		_FogDist ("Fog Distance", float) = 50
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Cull Off
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			
			#include "UnityCG.cginc"

			float3 _Forward;
			float3 _Align;

			float4 _AlignedCol;
			float4 _MisalignedCol;
			float4 _BkgCol;
			float _FogDist;

			float _Wavelength;
			float _Amplitude;
			float _Frequency;

			struct BoidData
			{
				float3 Position;
				float3 Forward;
			};
		
			StructuredBuffer<BoidData> _Boids;
			StructuredBuffer<uint> _BoidPartitions;

			struct appdata
			{
				uint instanceID : SV_InstanceID;
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float distToCam : TEXCOORD2;
				uint instanceID : TEXCOORD3;
			};
			
			v2f vert (appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);

				v2f o;

				float3 toCam = _WorldSpaceCameraPos - _Boids[v.instanceID].Position;

				float3 y = normalize(_Boids[v.instanceID].Forward);
				float3 x = normalize(cross(y, toCam));
				float3 z = normalize(cross(x, y));

				float4x4 modelMatrix = float4x4(
					x.x, y.x, z.x, 0,
					x.y, y.y, z.y, 0,
					x.z, y.z, z.z, 0,
					  0,   0,   0, 1);

				modelMatrix._m03_m13_m23_m33 += float4(_Boids[v.instanceID].Position, 0);

				float4x4 pv = mul(UNITY_MATRIX_P, UNITY_MATRIX_V);
				float4x4 mvp = mul(pv, modelMatrix);
				o.vertex = mul(mvp, v.vertex);

				//o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(modelMatrix, v.vertex);
				o.distToCam = length(o.worldPos - _WorldSpaceCameraPos);
                o.instanceID = v.instanceID;
				return o;
			}

			float InverseLerp(float v, float a, float b)
			{
				return (v - a) / (b - a);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				_Forward = _Boids[i.instanceID].Forward;
				float dotty = dot(_Forward, _Align);
				float4 shimmerCol = lerp(_MisalignedCol, _AlignedCol, InverseLerp(dotty * sign(dotty), 0.5, 1));
				return lerp (shimmerCol, _BkgCol, saturate(i.distToCam / _FogDist));
			}
			ENDCG
		}
	}
}
