Shader "Unlit/BoidMinimal"
{
    Properties
    {
		_Col ("Color", Color) = (1, 1, 1, 1)
		_Col2 ("Color2", Color) = (1, 1, 1, 1)
		_Bkg ("BkgColor", Color) = (1, 1, 1, 1)
		_FogDist ("Fog Dist", float) = 100 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		//ZWrite Off
		//ZTest Greater
		ZClip False

        Pass
        {
            CGPROGRAM
			#pragma multi_compile_instancing
			#pragma instancing_options
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			float4 _Col;
			float4 _Col2;
			float4 _Bkg;
			float _FogDist;

			float InverseLerp(float v, float a, float b)
			{
				return (v - a) / (b - a);
			}

            struct appdata
            {
				UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float distToCam : TEXCOORD1;
				float3 forward : TEXCOORD2;
            };


            v2f vert (appdata v)
            {
				UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.distToCam = length(worldPos - _WorldSpaceCameraPos);

				o.forward = normalize(mul(unity_ObjectToWorld, float4(0, 0, 10, 1)) - worldPos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				//return float4(i.forward, 1);
				float dotty = dot(i.forward, float3(1, 0, 0));
				//dotty *= 2;
				//dotty %= 1;
				float4 shimmer = lerp(_Col, _Col2, InverseLerp(dotty * sign(dotty), 0.5, 1));

                //float4 shimmer =  (_Col, _Col2, (dot(normalize(i.forward), float3(1, 0, 0)) + 1) / 2);
                return lerp (shimmer, _Bkg, saturate(i.distToCam / _FogDist));
            }
            ENDCG
        }
    }
}
