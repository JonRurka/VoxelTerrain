Shader "Custom/ChunkShader"
{
    Properties
    {
       _Textures("Textures", 2DArray) = "" {}
       //_Data("Volume", 3D) = "" {}
       _chunk("Chunk", Vector) = (0, 0, 0)
       _Dimensions("Dimensions", Vector) = (0, 0, 0)
       _NumTextures("Num Textures", Float) = 0
       _VoxelHalf("VoxelHalf", Float) = 0
       _voxelsPerMeter("voxelsPerMeter", Float) = 0
       _SideScale("Side Scale", Float) = 2
       _TopScale("Top Scale", Float) = 2
       _BottomScale("Bottom Scale", Float) = 2
       _Ambience("Ambience", Float) = 1
       _BlendFactor("Blend Factor", Float) = 1
    }
    SubShader
    {
        Pass 
        {
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma require 2darray

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"

            uniform StructuredBuffer<uint> _Data;
            uniform StructuredBuffer<uint> _textureMap;

            uniform StructuredBuffer<uint> _edgeData;

            UNITY_DECLARE_TEX2DARRAY(_Textures);
            //float _Data[10648];
            //sampler3D _Data;
            sampler2D _MainTex;
            float3 _chunk;
            float3 _Dimensions;
            int _NumTextures;

            float _SideScale, _TopScale, _BottomScale, _Ambience, _VoxelHalf, _voxelsPerMeter, _BlendFactor;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // put shadows data into TEXCOORD1
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD3;

                half3 worldNormal : NORMAL;
            };

             struct BlendCell
             {
                uint index;
                float3 dir;
                float3 col;
                float t;
             };

             int3 GlobalToLocalCoord(int3 location) {
                int x = location.x - (_chunk.x * _Dimensions.x);
                int y = location.y - (_chunk.y * _Dimensions.y);
                int z = location.z - (_chunk.z * _Dimensions.z);
                return int3(x, y, z);
             }

             int3 WorldToVoxel(float3 worldPos)
             {
                int x = floor((worldPos.x) * _voxelsPerMeter);
                int y = floor((worldPos.y) * _voxelsPerMeter);
                int z = floor((worldPos.z) * _voxelsPerMeter);
                return int3(x, y, z);
             }

             float3 VoxelToWorld(int3 loc) {
                float newX = (((loc.x / (float)_voxelsPerMeter) - 0));
                float newY = (((loc.y / (float)_voxelsPerMeter) - 0));
                float newZ = (((loc.z / (float)_voxelsPerMeter) - 0));
                return float3(newX, newY, newZ);
             }

             int Get_Flat_Index(int x, int y, int z)
             {
                return x + (_Dimensions.y + 2) * (y + (_Dimensions.z + 2) * z);
             }

             float Scale(float value, float oldMin, float oldMax, float newMin, float newMax)
             {
                return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
             }

             int3 snapToCardinal(float3 dir)
             {
                int3 v_dir = 0;

                int x_s = dir.x > 0 ? 1 : -1;
                int y_s = dir.y > 0 ? 1 : -1;
                int z_s = dir.z > 0 ? 1 : -1;

                if (abs(dir.x) > abs(dir.y) && abs(dir.x) > abs(dir.z))
                   v_dir = int3(1 * x_s, 0, 0);
                else if (abs(dir.y) > abs(dir.z))
                   v_dir = int3(0, 1 * y_s, 0);
                else
                   v_dir = int3(0, 0, 1 * z_s);

                return v_dir;
             }

             float3 tri_sample(half3 worldNormal, float3 projNormal, float3 postion, float index)
             {
                float2 uv = frac(postion.zy * _SideScale);
                float3 x = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index)) * abs(worldNormal.x);

                float3 y = 0;
                if (worldNormal.y > 0) {
                   uv = frac(postion.zx * _TopScale);
                   y = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index)) * abs(worldNormal.y);
                }
                else {
                   uv = frac(postion.zx * _BottomScale);
                   y = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index)) * abs(worldNormal.y);
                }

                uv = frac(postion.xy * _SideScale);
                float3 z = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv.x, uv.y, index)) * abs(worldNormal.z);

                float3 Albedo = z;
                Albedo = lerp(Albedo, x, projNormal.x);
                Albedo = lerp(Albedo, y, projNormal.y);

                return Albedo;
             }

             v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0, float3 normal : NORMAL)
             {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;

                o.worldPos = mul(unity_ObjectToWorld, vertex);

                o.worldNormal = UnityObjectToWorldNormal(normal);

                // diffuse and ambient lighting.
                half nl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(o.worldNormal, 1)) + _Ambience;

                // compute shadows data
                TRANSFER_SHADOW(o)

                return o;
             }

             fixed4 frag(v2f i) : SV_Target
             {
                  //fixed4 col = 0;//tex2D(_MainTex, i.uv);
                  float voxelHalf = ((1 / _voxelsPerMeter) / 2);

                  float3 localPos = i.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                  float3 projNormal = saturate(pow(i.worldNormal * 1.4, 4));

                  half3 worldNormal = i.worldNormal;

                  int3 worldVoxel = WorldToVoxel(i.worldPos);
                  int3 localVoxel = GlobalToLocalCoord(worldVoxel);
                  float3 localCenterPosition = VoxelToWorld(localVoxel) + float3(voxelHalf, voxelHalf, voxelHalf);

                  float3 difference = localPos - localCenterPosition;
                  float3 dir = normalize(difference);

                  uint type = _Data[Get_Flat_Index(localVoxel.x + 1, localVoxel.y + 1, localVoxel.z + 1)];
                  uint index1 = _textureMap[type * 6 + 0];
                  
                  float3 dirs[6] = {
                    float3(1, 0, 0),
                    float3(-1, 0, 0),
                    float3(0, 1, 0),
                    float3(0, -1, 0),
                    float3(0, 0, 1),
                    float3(0, 0, -1),
                  };

                  //uint numBlendCells = 0;
                  //BlendCell cells[6];

                  float3 Albedo = tri_sample(worldNormal, projNormal, localPos, (float)index1);
                  

                  int3 otherVoxel = 0;
                  uint o_type = 0;
                  uint other_index = 0;
                  float3 dirLength = 0;
                  float3 other_Albedo = 0;
                  float t = 0;
                  for (uint j = 0; j < 6; j++)
                  {
                     otherVoxel = localVoxel + dirs[j];
                     o_type = _Data[Get_Flat_Index(otherVoxel.x + 1, otherVoxel.y + 1, otherVoxel.z + 1)];
                     other_index = _textureMap[o_type * 6 + 0];

                     if (other_index != index1)
                     {
                        int frag_x_s = sign(dir.x);
                        int frag_y_s = sign(dir.y);
                        int frag_z_s = sign(dir.z);

                        int other_x_s = sign(dirs[j].x);
                        int other_y_s = sign(dirs[j].y);
                        int other_z_s = sign(dirs[j].z);

                        if ((other_x_s == 0 || other_x_s == frag_x_s) &&
                           (other_y_s == 0 || other_y_s == frag_y_s) &&
                           (other_z_s == 0 || other_z_s == frag_z_s))
                        {
                           //numBlendCells thisCell;
                           //thisCell.index = other_index;
                           //thisCell.dir = dirs[i];

                           dirLength = (dirs[j] * difference);

                           other_Albedo = tri_sample(worldNormal, projNormal, localPos, other_index);

                           t = abs(length(dirLength) / (1 / _voxelsPerMeter)) * _BlendFactor;

                           Albedo = lerp(Albedo, other_Albedo, t);

                           //cells[numBlendCells] = thisCell;
                           //numBlendCells++;
                        }
                        //int3 blendDirs = int(frag_x_s == other_x_s ? other_x_s : 0, frag_y_s == other_y_s ? other_y_s : 0, frag_z_s == other_z_s ? other_z_s : 0);

                     }
                  }
                  //return fixed4(abs(v_dir.x), abs(v_dir.y), abs(v_dir.z), 0);

                  

                  //float index2 = index1;

                  /*if ((otherLocalVoxel.x < 0 || otherLocalVoxel.y < 0 || otherLocalVoxel.z < 0) ||
                     (otherLocalVoxel.x >= _Dimensions.x || otherLocalVoxel.y >= _Dimensions.y || otherLocalVoxel.z >= _Dimensions.z))
                  {
                     index2 = index1;
                  }
                  else
                  {
                     uint o_type = _Data[Get_Flat_Index(otherLocalVoxel.x, otherLocalVoxel.y, otherLocalVoxel.z)];
                     index2 = _textureMap[o_type * 6 + 0];
                  }*/

                  //uint o_type = _Data[Get_Flat_Index(otherLocalVoxel.x, otherLocalVoxel.y, otherLocalVoxel.z)];
                  //index2 = _textureMap[o_type * 6 + 0];

                  //return fixed4(abs(v_dir_f.x), abs(v_dir_f.y), abs(v_dir_f.z), 0);
                  //return fixed4(abs(v_dir.x * normalized_v_dir_length), abs(v_dir.y * normalized_v_dir_length), abs(v_dir.z * normalized_v_dir_length), 0);


                  fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);

                  // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                  fixed shadow = SHADOW_ATTENUATION(i);
                  // darken light's illumination with shadow, keep ambient intact
                  fixed3 lighting = i.diff * shadow + i.ambient;
                  col.rgb *= lighting;

                  return col;
             }
            ENDCG
        }
        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
}
