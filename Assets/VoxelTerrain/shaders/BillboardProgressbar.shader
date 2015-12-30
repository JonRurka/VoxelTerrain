Shader "Billboard/Progressbar" {
	Properties {
		_FrameColor("Frame Color", color) = (1,1,1,1)
		_BarColor("Bar Color", color) = (1,1,1,1)
		_MainTex ("Frame texture", 2D) = "white" {}
		_BarTex ("Bar texture", 2D) = "white"{}
		_Progress ("Progress", Range(0,1)) = 1
		_Ramp("Ramp", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue" = "Transparent"} 
		Pass {   
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
 
			#pragma vertex vert  
			#pragma fragment frag 
         // User-specified uniforms            
			uniform sampler2D _MainTex;
			uniform float4 _FrameColor;
 
			struct vertexInput {
				float4 vertex : POSITION;
				float4 tex : TEXCOORD0;
			};
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
			};
 
			vertexOutput vert(vertexInput input) 
			{
				vertexOutput output;
				output.pos = mul(UNITY_MATRIX_P, 
				mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
					- float4(input.vertex.x, input.vertex.y, 0.0, 0.0));
 
				output.tex = input.tex;
 
				return output;
			}
 
			float4 frag(vertexOutput input) : COLOR
			{
				return tex2D(_MainTex, float2(input.tex.xy)) * _FrameColor;   
			}
			ENDCG
		}
		Pass {   
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
 
			#pragma vertex vert  
			#pragma fragment frag 
         // User-specified uniforms            
			uniform sampler2D _BarTex;
			uniform float4 _BarColor;
			uniform float _Progress;
			uniform sampler2D _Ramp;
 
			struct vertexInput {
				float4 vertex : POSITION;
				float4 tex : TEXCOORD0;
			};
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
			};
 
			vertexOutput vert(vertexInput input) 
			{
				vertexOutput output;
				output.pos = mul(UNITY_MATRIX_P, 
				mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
					- float4(input.vertex.x, input.vertex.y, 0.0, 0.0));
 
				output.tex = input.tex;
 
				return output;
			}
			float4 frag(vertexOutput input) : COLOR
			{
				float4 rampColor = tex2D(_Ramp, float2(input.tex.xy));
				if(rampColor.a <= 1 - _Progress){
					discard;
				}
				return tex2D(_BarTex, float2(input.tex.xy)) * _BarColor;   
			}
			ENDCG
		}
	}
}