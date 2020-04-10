Shader "Custom/ChunkShader_surf"
{
   Properties
   {
      _MainTex("Texture", 2D) = "white" {}
      _Textures("Textures", 2DArray) = "" {}
      //_Data("Volume", 3D) = "" {}
      _chunk("Chunk", Vector) = (0, 0, 0)
      _Dimensions ("Dimensions", Vector) = (0, 0, 0)
      _VoxelHalf ("VoxelHalf", Float) = 0
      _NumTextures ("Num Textures", Float) = 0
      _SideScale("Side Scale", Float) = 2
      _TopScale("Top Scale", Float) = 2
      _BottomScale("Bottom Scale", Float) = 2
   }

   SubShader
   {
      Tags {
         "Queue" = "Geometry"
         "IgnoreProjector" = "False"
         "RenderType" = "Opaque"
      }

      Cull Back
      ZWrite On

      CGPROGRAM
      // Physically based Standard lighting model, and enable shadows on all light types
      #pragma surface surf Lambert fullforwardshadows
      #pragma exclude_renderers flash

      // Use shader model 5.0 target, to get nicer looking lighting, texture arrays, and structured buffers
      #pragma target 5.0

      #pragma require 2darray

      #include "UnityCG.cginc"

      #if defined(SHADER_API_D3D11)
      uniform StructuredBuffer<float> _Data;
      uniform StructuredBuffer<float> _textureMap;
      //RWStructuredBuffer<float> _t_out : register(u1);
      #endif

      UNITY_DECLARE_TEX2DARRAY(_Textures);
      //float _Data[10648];
      //sampler3D _Data;
      sampler2D _MainTex;
      float3 _chunk;
      float3 _Dimensions;
      int _NumTextures;

      float _SideScale, _TopScale, _BottomScale;
      
      struct Input {
         float3 worldPos;
         float3 worldNormal;
         float2 uv_MainTex;
         float4 data : COLOR;
      };

      int3 GlobalToLocalCoord(float3 location) {
         int x = location.x - (_chunk.x * _Dimensions.x);
         int y = location.y - (_chunk.y * _Dimensions.y);
         int z = location.z - (_chunk.z * _Dimensions.z);
         return int3(x, y, z);
      }

      /*int3 WorldToVoxel(float3 worldPos)
      {
         int x = Mathf.FloorToInt(((worldPos.x) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
         int y = Mathf.FloorToInt(((worldPos.y) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
         int z = Mathf.FloorToInt(((worldPos.z) + VoxelSettings.half) * (float)VoxelSettings.voxelsPerMeter);
         return new Vector3Int(x, y, z);
      }*/

      int Get_Flat_Index(int x, int y, int z)
      {
         return x + _Dimensions.y * (y + _Dimensions.z * z);
      }

      float Scale(float value, float oldMin, float oldMax, float newMin, float newMax)
      {
         return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
      }

      void surf(Input IN, inout SurfaceOutput o)
      {
         float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
         float3 projNormal = saturate(pow(IN.worldNormal * 1.4, 4));

         float3 worldNormal = IN.worldNormal;

         int3 block = GlobalToLocalCoord(localPos);

#if defined(SHADER_API_D3D11) || defined(SHADER_TARGET_SURFACE_ANALYSIS)

         
         
         
         float type = 0;
         float index1 = 2;

         //int index1 = round(Scale(IN.data.x, 0, 1, 0, _NumTextures));
         //int index2 = round(Scale(IN.data.y, 0, 1, 0, _NumTextures));
         //int index3 = round(Scale(IN.data.z, 0, 1, 0, _NumTextures));

         //o.Albedo = float3(In.data.x, In.data.x, In.data.x);

         type = 3;//_Data[Get_Flat_Index(block.x, block.y, block.z)];
         index1 = 1;//_textureMap[0];// _textureMap[type * 6 + 0];

         //int index = round(_Data[Get_Flat_Index((block.x + 1), (block.y + 1), (block.z + 1))]);

         //float3 block_sample = float3((block.x + 1) / (_Dimensions.x + 2), (block.y + 1) / (_Dimensions.y + 2), (block.z + 1) / (_Dimensions.z + 2));
         //float3 block_sample = float3(0.5, 0.54, 0.54);
         //float3 vol = tex3D(_Data, block_sample);
         //float isOut = vol.y;
         //float index = vol.x;

        // return;
         
         // SIDE X
         float2 uv = frac(localPos.zy * _SideScale);
         float3 x = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index1)) * abs(IN.worldNormal.x);
         //float3 x_2 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index2)) * abs(IN.worldNormal.x);
         //float3 x_3 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index3)) * abs(IN.worldNormal.x);
         //float3 x = (x_1 * IN.uv_MainTex.x) + (x_2 * (1 - IN.uv_MainTex.x));
         //1x = (x * IN.uv_MainTex.y) + (x_3 * (1 - IN.uv_MainTex.y));
         //float3 x = lerp(x_1, x_2, IN.uv_MainTex.x);
         //x = lerp(x, x_3, IN.uv_MainTex.y);

         // TOP / BOTTOM
         float3 y = 0;
         if (worldNormal.y > 0) {
            uv = frac(localPos.zx * _TopScale);
            y = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index1)) * abs(worldNormal.y);
            //float3 y_2 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index2)) * abs(IN.worldNormal.y);
            //float3 y_3 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index3)) * abs(IN.worldNormal.y);
            //float3 f = tex2D(_MainTex, uv) * abs(IN.worldNormal.y);

            //y = lerp(y_2, y_1, f.x);
            //y = lerp(y_3, y, f.y);
            //y = lerp(y, y_3, IN.uv_MainTex.y);
         }
         else {
            uv = frac(localPos.zx * _BottomScale);
            y = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index1)) * abs(worldNormal.y);
            //float3 y_2 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index2)) * abs(IN.worldNormal.y);
            //float3 y_3 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index3)) * abs(IN.worldNormal.y);
            //y = lerp(y_1, y_2, IN.uv_MainTex.x);
            //y = lerp(y, y_3, IN.uv_MainTex.y);
         }

         // SIDE Z
         uv = frac(localPos.xy * _SideScale);
         float3 z = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index1)) * abs(worldNormal.z);
         //float3 z_2 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index2)) * abs(IN.worldNormal.z);
         //float3 z_3 = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index3)) * abs(IN.worldNormal.z);
         //float z = lerp(z_1, z_2, IN.uv_MainTex.x);
         //z = lerp(z, z_3, IN.uv_MainTex.y);

         o.Albedo = z;
         o.Albedo = lerp(o.Albedo, x, projNormal.x);
         o.Albedo = lerp(o.Albedo, y, projNormal.y);
#else
         o.Albedo = float4(0,0,1,0);
         //o.Albedo = tex2D(_MainTex, IN.uv_MainTex);

#endif
      }
      ENDCG
   }
   FallBack "Diffuse"
}
    

