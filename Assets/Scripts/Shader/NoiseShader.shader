// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostEffects/CRT"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		//_NoiseX("NoiseX", Range(0, 1)) = 0
		//_Offset("Offset", Vector) = (0, 0, 0, 0)
		_RGBNoise("RGBNoise", Range(0, 1)) = 0
		//_SinNoiseWidth("SineNoiseWidth", Float) = 1
		//_SinNoiseScale("SinNoiseScale", Float) = 1
		//_SinNoiseOffset("SinNoiseOffset", Float) = 1
		//_ScanLineTail("Tail", Float) = 0.5
		//_ScanLineSpeed("TailSpeed", Float) = 100
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
				#pragma target 3.0

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				float rand(float2 co) {
					return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
				}

				float2 mod(float2 a, float2 b)
				{
					return a - floor(a / b) * b;
				}
				sampler2D _MainTex;
				float _NoiseX;
				float2 _Offset;
				float _RGBNoise;
				float _SinNoiseWidth;
				float _SinNoiseScale;
				float _SinNoiseOffset;
				float _ScanLineTail;
				float _ScanLineSpeed;

				fixed4 frag(v2f i) : SV_Target
				{
					
					float2 inUV = i.uv;
					float2 uv = i.uv - 0.5;
					float2 texUV = uv + 0.5;

					// 画面外なら描画しない
					if (max(abs(uv.y) - 0.5, abs(uv.x) - 0.5) > 0)
					{
						return float4(0, 0, 0, 1);
					}

					// 色を計算
					float3 col;
					// 色を取得
					col.r = tex2D(_MainTex, texUV).r;
					col.g = tex2D(_MainTex, texUV).g;
					col.b = tex2D(_MainTex, texUV).b;

					// RGBノイズ
					//if (rand((rand(floor(texUV.x * 1000) + _Time.x) - 0.5) + _Time.x) < _RGBNoise && rand((rand(floor(texUV.y * 1000) + _Time.y) - 0.5) + _Time.y) < _RGBNoise)
					if (rand((rand(floor(texUV * 1000) + _Time) - 0.5) + _Time) < _RGBNoise)
					{
						col.r = rand(uv + float2(123 + _Time.x + _Time.y, 0));
						col.g = rand(uv + float2(123 + _Time.x + _Time.y, 1));
						col.b = rand(uv + float2(123 + _Time.x + _Time.y, 2));
					}

					return float4(col, 1);
					
				}
				ENDCG
			}
		}
}