// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

Shader "Custom/ChunkShader"
{
    Properties
    {
       _Glossiness("Smoothness", Range(0,1)) = 0.5
       _Metallic("Metallic", Range(0,1)) = 0.0
       _Emission("Emission", Range(0,1)) = 0.0
       _Alpha("Alpha", Range(0,1)) = 0.0
       _Occlusion("Occlusion", Range(0,1)) = 1.0
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
          Tags { "RenderType" = "Opaque" }

          CGINCLUDE
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "UnityPBSLighting.cginc"
            #pragma require 2darray

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

            half _Glossiness;
            half _Metallic;
            half _Emission;
            half _Alpha;
            half _Occlusion;

            float _SideScale, _TopScale, _BottomScale, _Ambience, _VoxelHalf, _voxelsPerMeter, _BlendFactor;

#ifndef LIGHTMAP_ON
            // half-precision fragment shader registers:
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
            struct v2f_base {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0; // _MainTex
               float3 worldNormal : TEXCOORD1;
               float4 worldPos : TEXCOORD2;
#if UNITY_SHOULD_SAMPLE_SH
               half3 sh : TEXCOORD3; // SH
#endif
               UNITY_LIGHTING_COORDS(4, 5)
#if SHADER_TARGET >= 30
               float4 lmap : TEXCOORD6;
#endif
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };
#endif
            // high-precision fragment shader registers:
#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            struct v2f_base {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0; // _MainTex
               float3 worldNormal : TEXCOORD1;
               float3 worldPos : TEXCOORD2;
#if UNITY_SHOULD_SAMPLE_SH
               half3 sh : TEXCOORD3; // SH
#endif
               UNITY_FOG_COORDS(4)
               UNITY_SHADOW_COORDS(5)
#if SHADER_TARGET >= 30
               float4 lmap : TEXCOORD6;
#endif
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };
#endif
#endif


#ifdef LIGHTMAP_ON
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
            struct v2f_base
            {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0;
               SHADOW_COORDS(1) // put shadows data into TEXCOORD1
               fixed3 diff : COLOR0;
               fixed3 ambient : COLOR1;
               //float4 pos : SV_POSITION;
               float4 worldPos : TEXCOORD2;
               float4 lmap : TEXCOORD3;

               half3 worldNormal : NORMAL;

               UNITY_LIGHTING_COORDS(4, 5)

               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };
#endif


#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            struct v2f_base
            {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0;
               //SHADOW_COORDS(1) // put shadows data into TEXCOORD1
               fixed3 diff : COLOR0;
               fixed3 ambient : COLOR1;
               //float4 pos : SV_POSITION;
               float4 worldPos : TEXCOORD2;
               float4 lmap : TEXCOORD3;

               half3 worldNormal : NORMAL;

               UNITY_FOG_COORDS(4)
               UNITY_SHADOW_COORDS(5)
#ifdef DIRLIGHTMAP_COMBINED
               float3 tSpace0 : TEXCOORD6;
               float3 tSpace1 : TEXCOORD7;
               float3 tSpace2 : TEXCOORD8;
#endif
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };
#endif
#endif
            // vertex-to-fragment interpolation data
            struct v2f_Add {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0; // _MainTex
               float3 worldNormal : TEXCOORD1;
               float3 worldPos : TEXCOORD2;
               UNITY_LIGHTING_COORDS(3, 4)
               UNITY_FOG_COORDS(5)
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            // vertex-to-fragment interpolation data
            struct v2f_Deferred {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0; // _MainTex
               float3 worldNormal : TEXCOORD1;
               float3 worldPos : TEXCOORD2;
#ifndef DIRLIGHTMAP_OFF
               float3 viewDir : TEXCOORD3;
#endif
               float4 lmap : TEXCOORD4;
#ifndef LIGHTMAP_ON
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
               half3 sh : TEXCOORD5; // SH
#endif
#else
#ifdef DIRLIGHTMAP_OFF
               float4 lmapFadePos : TEXCOORD5;
#endif
#endif
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
            };

            struct v2f_Meta {
               UNITY_POSITION(pos);
               float2 uv : TEXCOORD0; // _MainTex
               float3 worldPos : TEXCOORD1;
               float3 worldNormal : NORMAL;
#ifdef EDITOR_VISUALIZATION
               float2 vizUV : TEXCOORD2;
               float4 lightCoord : TEXCOORD3;
#endif
               UNITY_VERTEX_INPUT_INSTANCE_ID
               UNITY_VERTEX_OUTPUT_STEREO
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

            float3 GetBlendedAlbedo(float3 worldPos, float3 worldNormal)
            {
               float voxelHalf = ((1 / _voxelsPerMeter) / 2);

               float3 localPos = worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
               float3 projNormal = saturate(pow(worldNormal * 1.4, 4));

               int3 worldVoxel = WorldToVoxel(worldPos);
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

               return Albedo;
            }

            /*v2f LegacyLight_vert(v2f o)
            {
               // diffuse and ambient lighting.
               half nl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
               o.diff = nl * _LightColor0.rgb;
               o.ambient = ShadeSH9(half4(o.worldNormal, 1)) + _Ambience;

               // compute shadows data
               TRANSFER_SHADOW(o)

               return o;
            }

            fixed4 LegacyLight_frag(fixed4 col, v2f i)
            {
               // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
               fixed shadow = SHADOW_ATTENUATION(i);
               // darken light's illumination with shadow, keep ambient intact
               fixed3 lighting = i.diff * shadow + i.ambient;
               col.rgb *= lighting;

               return col;
            }*/



          ENDCG


        Pass 
        {
            Name "FORWARD"
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase

            #pragma vertex vert
            #pragma fragment frag
            
            #define UNITY_INSTANCED_LOD_FADE
            #define UNITY_INSTANCED_SH
            #define UNITY_INSTANCED_LIGHTMAPSTS

            #define INTERNAL_DATA
            #define WorldReflectionVector(data,normal) data.worldRefl
            #define WorldNormalVector(data,normal) normal

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight multi_compile_fog
            // shadow helper functions and macros

            v2f_base ForwardBase_vert(appdata_full v, v2f_base o)
            {

#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
               fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
               fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
               fixed3 worldBinormal = cross(o.worldNormal, worldTangent) * tangentSign;
#endif
#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED) && !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
               o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
               o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
               o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#endif

#ifdef DYNAMICLIGHTMAP_ON
               o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
#ifdef LIGHTMAP_ON
               o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

               // SH/ambient and vertex lights
#ifndef LIGHTMAP_ON
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
               o.sh = 0;
               // Approximated illumination from non-important point lights
#ifdef VERTEXLIGHT_ON
               o.sh += Shade4PointLights(
                  unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                  unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                  unity_4LightAtten0, o.worldPos, o.worldNormal);
#endif
               o.sh = ShadeSHPerVertex(o.worldNormal, o.sh);
#endif
#endif // !LIGHTMAP_ON

               UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
#ifdef FOG_COMBINED_WITH_TSPACE
               UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o, o.pos); // pass fog coordinates to pixel shader
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
               UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos); // pass fog coordinates to pixel shader
#else
               UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
#endif
               return o;
            }

            fixed4 ForwardBase_frag(fixed4 col, v2f_base i)
            {
               float3 worldPos = i.worldPos.xyz;
#ifndef USING_DIRECTIONAL_LIGHT
               fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
               fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
               float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

#ifdef UNITY_COMPILER_HLSL
               SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
               SurfaceOutputStandard o;
#endif
               o.Metallic = _Metallic;
               o.Smoothness = _Glossiness;
               o.Albedo = col;
               o.Emission = _Emission;
               o.Alpha = _Alpha;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;

               // compute lighting & shadowing factor
               UNITY_LIGHT_ATTENUATION(atten, i, worldPos)
                  fixed4 c = 0;

               // Setup lighting environment
               UnityGI gi;
               UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
               gi.indirect.diffuse = 0;
               gi.indirect.specular = 0;
               gi.light.color = _LightColor0.rgb;
               gi.light.dir = lightDir;
               // Call GI (lightmaps/SH/reflections) lighting function
               UnityGIInput giInput;
               UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
               giInput.light = gi.light;
               giInput.worldPos = worldPos;
               giInput.worldViewDir = worldViewDir;
               giInput.atten = atten;

#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
               giInput.lightmapUV = IN.lmap;
#else
               giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
               giInput.ambient = i.sh;
#else
               giInput.ambient.rgb = 0.0;
#endif
               giInput.probeHDR[0] = unity_SpecCube0_HDR;
               giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
               giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
               giInput.boxMax[0] = unity_SpecCube0_BoxMax;
               giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
               giInput.boxMax[1] = unity_SpecCube1_BoxMax;
               giInput.boxMin[1] = unity_SpecCube1_BoxMin;
               giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
               LightingStandard_GI(o, giInput, gi);

               // realtime lighting: call lighting function
               c += LightingStandard(o, worldViewDir, gi);
               UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
               UNITY_OPAQUE_ALPHA(c.a);
               return c;
            }

             v2f_base vert(appdata_full v)
             {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f_base o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                return ForwardBase_vert(v, o);
             }

             fixed4 frag(v2f_base i) : SV_Target
             {
                  
                  float3 Albedo = GetBlendedAlbedo(i.worldPos, i.worldNormal);
                  fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);

                  return ForwardBase_frag(col, i);
                  //return LegacyLight_frag(col, i);
             }
            ENDCG
        }

        Pass
        {
           Name "FORWARD"
           Tags {"LightMode" = "ForwardAdd"}
           ZWrite Off Blend One One

           CGPROGRAM

            //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight multi_compile_fog multi_compile_fwdadd_fullshadows
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma skip_variants INSTANCING_ON
            #pragma multi_compile_fwdadd_fullshadows

            #define UNITY_INSTANCED_LOD_FADE
            #define UNITY_INSTANCED_SH
            #define UNITY_INSTANCED_LIGHTMAPSTS

            #define INTERNAL_DATA
            #define WorldReflectionVector(data,normal) data.worldRefl
            #define WorldNormalVector(data,normal) normal

            #pragma vertex vert
            #pragma fragment frag

            v2f_Add vert(appdata_full v)
            {
               UNITY_SETUP_INSTANCE_ID(v);
               v2f_Add o;
               o.pos = UnityObjectToClipPos(v.vertex);
               o.uv = v.texcoord;
               o.worldPos = mul(unity_ObjectToWorld, v.vertex);
               o.worldNormal = UnityObjectToWorldNormal(v.normal);
               UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
               UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
               return o;
            }
              
            fixed4 frag(v2f_Add i) : SV_Target
            {

                 float3 Albedo = GetBlendedAlbedo(i.worldPos, i.worldNormal);
                 fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);

#ifdef FOG_COMBINED_WITH_TSPACE
                 UNITY_EXTRACT_FOG_FROM_TSPACE(i);
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
                 UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
#else
                 UNITY_EXTRACT_FOG(IN);
#endif
                 float3 worldPos = i.worldPos.xyz;
#ifndef USING_DIRECTIONAL_LIGHT
                 fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
                 fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
                 float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
#ifdef UNITY_COMPILER_HLSL
                 SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
                 SurfaceOutputStandard o;
#endif
                 o.Metallic = _Metallic;
                 o.Smoothness = _Glossiness;
                 o.Albedo = col;
                 o.Emission = _Emission;
                 o.Alpha = _Alpha;
                 o.Occlusion = _Occlusion;
                 fixed3 normalWorldVertex = fixed3(0, 0, 1);
                 o.Normal = i.worldNormal;
                 normalWorldVertex = i.worldNormal;

                 UNITY_LIGHT_ATTENUATION(atten, i, worldPos)
                 fixed4 c = 0;

                 // Setup lighting environment
                 UnityGI gi;
                 UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
                 gi.indirect.diffuse = 0;
                 gi.indirect.specular = 0;
                 gi.light.color = _LightColor0.rgb;
                 gi.light.dir = lightDir;
                 gi.light.color *= atten;
                 c += LightingStandard(o, worldViewDir, gi);
                 c.a = 0.0;
                 UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
                 UNITY_OPAQUE_ALPHA(c.a);

                 return c;//ForwardBase_frag(col, i);
                 //return LegacyLight_frag(col, i);
            }
           ENDCG


        }

        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }

            CGPROGRAM

            #pragma multi_compile_instancing
            #pragma exclude_renderers nomrt
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma multi_compile_prepassfinal

            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_INSTANCED_LOD_FADE
            #define UNITY_INSTANCED_SH
            #define UNITY_INSTANCED_LIGHTMAPSTS

            #define INTERNAL_DATA
            #define WorldReflectionVector(data,normal) data.worldRefl
            #define WorldNormalVector(data,normal) normal

            v2f_Deferred vert(appdata_full v)
            {
               UNITY_SETUP_INSTANCE_ID(v);
               v2f_Deferred o;
               o.pos = UnityObjectToClipPos(v.vertex);
               o.uv = v.texcoord;
               o.worldPos = mul(unity_ObjectToWorld, v.vertex);
               o.worldNormal = UnityObjectToWorldNormal(v.normal);

               float3 worldPos = o.worldPos;
               float3 worldNormal = o.worldNormal;

               float3 viewDirForLight = UnityWorldSpaceViewDir(worldPos);
#ifndef DIRLIGHTMAP_OFF
               o.viewDir = viewDirForLight;
#endif

#ifdef DYNAMICLIGHTMAP_ON
               o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#else
               o.lmap.zw = 0;
#endif
#ifdef LIGHTMAP_ON
               o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#ifdef DIRLIGHTMAP_OFF
               o.lmapFadePos.xyz = (mul(unity_ObjectToWorld, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
               o.lmapFadePos.w = (-UnityObjectToViewPos(v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
#endif
#else
               o.lmap.xy = 0;
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
               o.sh = 0;
               o.sh = ShadeSHPerVertex(worldNormal, o.sh);
#endif
#endif
               return o;
            }

            #ifdef LIGHTMAP_ON
            float4 unity_LightmapFade;
            #endif
            fixed4 unity_Ambient;

            void frag(v2f_Deferred i, out half4 outGBuffer0 : SV_Target0, out half4 outGBuffer1 : SV_Target1, out half4 outGBuffer2 : SV_Target2, out half4 outEmission : SV_Target3
/*#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
               , out half4 outShadowMask : SV_Target4
#endif*/)
            {
               float3 Albedo = GetBlendedAlbedo(i.worldPos, i.worldNormal);
               fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);


               UNITY_SETUP_INSTANCE_ID(i);
#ifdef FOG_COMBINED_WITH_TSPACE
               UNITY_EXTRACT_FOG_FROM_TSPACE(i);
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
               UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
#else
               UNITY_EXTRACT_FOG(i);
#endif
               float3 worldPos = i.worldPos.xyz;

#ifndef USING_DIRECTIONAL_LIGHT
               fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
               fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
               float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
#ifdef UNITY_COMPILER_HLSL
               SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
               SurfaceOutputStandard o;
#endif
               o.Metallic = _Metallic;
               o.Smoothness = _Glossiness;
               o.Albedo = col;
               o.Emission = _Emission;
               o.Alpha = _Alpha;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;

               fixed3 originalNormal = o.Normal;
               half atten = 1;

               // Setup lighting environment
               UnityGI gi;
               UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
               gi.indirect.diffuse = 0;
               gi.indirect.specular = 0;
               gi.light.color = 0;
               gi.light.dir = half3(0, 1, 0);
               // Call GI (lightmaps/SH/reflections) lighting function
               UnityGIInput giInput;
               UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
               giInput.light = gi.light;
               giInput.worldPos = worldPos;
               giInput.worldViewDir = worldViewDir;
               giInput.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
               giInput.lightmapUV = IN.lmap;
#else
               giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
               giInput.ambient = i.sh;
#else
               giInput.ambient.rgb = 0.0;
#endif
               giInput.probeHDR[0] = unity_SpecCube0_HDR;
               giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
               giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
               giInput.boxMax[0] = unity_SpecCube0_BoxMax;
               giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
               giInput.boxMax[1] = unity_SpecCube1_BoxMax;
               giInput.boxMin[1] = unity_SpecCube1_BoxMin;
               giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
               LightingStandard_GI(o, giInput, gi);

               // call lighting function to output g-buffer
               outEmission = LightingStandard_Deferred(o, worldViewDir, gi, outGBuffer0, outGBuffer1, outGBuffer2);
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
               //outShadowMask = UnityGetRawBakedOcclusions(IN.lmap.xy, worldPos);
#endif
#ifndef UNITY_HDR_ON
               outEmission.rgb = exp2(-outEmission.rgb);
#endif

               //float3 Albedo = GetBlendedAlbedo(i.worldPos, i.worldNormal);
               //fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);

               
            }

            ENDCG
        }
        
        Pass
        {
           Name "Meta"
           Tags { "LightMode" = "Meta" }
           Cull Off

           CGPROGRAM
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma shader_feature EDITOR_VISUALIZATION

            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_INSTANCED_LOD_FADE
            #define UNITY_INSTANCED_SH
            #define UNITY_INSTANCED_LIGHTMAPSTS

            #define INTERNAL_DATA
            #define WorldReflectionVector(data,normal) data.worldRefl
            #define WorldNormalVector(data,normal) normal

            #include "UnityMetaPass.cginc"

            v2f_Meta vert(appdata_full v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f_Meta o;
                UNITY_INITIALIZE_OUTPUT(v2f_Meta, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = v.texcoord;
                o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
#ifdef EDITOR_VISUALIZATION
                o.vizUV = 0;
                o.lightCoord = 0;
                if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
                   o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
                else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
                {
                   o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                   o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
                }
#endif
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos.xyz = worldPos;
                o.worldNormal = worldNormal;
                return o;
            }

            fixed4 frag(v2f_Meta i) : SV_Target
            {
               float3 Albedo = GetBlendedAlbedo(i.worldPos, i.worldNormal);
               fixed4 col = fixed4(Albedo.x, Albedo.y, Albedo.z, 0);

               UNITY_SETUP_INSTANCE_ID(i);

#ifdef FOG_COMBINED_WITH_TSPACE
               UNITY_EXTRACT_FOG_FROM_TSPACE(i);
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
               UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
#else
               UNITY_EXTRACT_FOG(i);
#endif

               float3 worldPos = i.worldPos.xyz;
#ifndef USING_DIRECTIONAL_LIGHT
               fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
               fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
               SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
               SurfaceOutputStandard o;
#endif
               o.Metallic = _Metallic;
               o.Smoothness = _Glossiness;
               o.Albedo = col;
               o.Emission = _Emission;
               o.Alpha = _Alpha;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;


               UnityMetaInput metaIN;
               UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
               metaIN.Albedo = o.Albedo;
               metaIN.Emission = o.Emission;
#ifdef EDITOR_VISUALIZATION
               metaIN.VizUV = i.vizUV;
               metaIN.LightCoord = i.lightCoord;
#endif
               return UnityMetaFragment(metaIN);
            }

           ENDCG
        }

        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
}
