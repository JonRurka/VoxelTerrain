Shader "Billboard/CutoffSimpleColor" {
	Properties {
		_Color ("Min Color", color) = (1,1,1,1) 
		_Cutoff ("Alpha Cutoff", Range(0,1)) = 1
		_Ramp ("Ramp", 2D) = "white" {}

	}
	SubShader {
		Tags {"Queue" = "Transparent"} 
		Pass {   
			CGPROGRAM
 
			#pragma vertex vert  
			#pragma fragment frag 
         // User-specified uniforms            
			uniform sampler2D _Ramp;
			uniform float _Cutoff;
			uniform float4 _Color;
			uniform float4 _Ramp_ST;
 
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
				float4 textureColor = tex2D(_Ramp, float2(_Ramp_ST.xy * input.tex.xy + _Ramp_ST.zw));
				if(textureColor.a < _Cutoff){
					discard;
				}
				return _Color;   
			}
			ENDCG
		}
	}
}