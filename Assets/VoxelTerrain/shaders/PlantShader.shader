Shader "Custom/PlantShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "gray" {}
        _Ambience("Ambience", Float) = 1
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.99
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Emission("Emission", Range(0,1)) = 0.0
        _Alpha("Alpha", Range(0,1)) = 0.0
        _Occlusion("Occlusion", Range(0,1)) = 1.0

        //__Time("Time", Float) = 0
        
        _WindDir("WindDir", Vector) = (1, 0, 0)
        _baseWindShere("BaseWindShere", Range(0,1)) = 0.1

        _permutation("permutation table" , 2D) = "white" {}

        _scale("scale", Vector) = (15, 0, 15)
        _shift("shift", Vector) = (0, 0, 0)

        _gain("gain", Float) = 1
        _setup("setup", Float) = 0
    }
    SubShader
    {
        //Tags { "Queue" = "AlphaTest" "RenderType" = "Transparent" }
        //Tags{ "Queue" = "Transparent" "RenderType" = "TransparentCutout" }
        Tags{"Queue" = "AlphaTest" "RenderType" = "Opaque" "RenderType" = "TransparentCutout"}
        //Blend SrcAlpha OneMinusSrcAlpha
        AlphaToMask On
        LOD 200
        ZWrite On

        CGINCLUDE
           #include "UnityCG.cginc"
           #include "Lighting.cginc"
           #include "AutoLight.cginc"
           #include "UnityPBSLighting.cginc"

           #pragma target 5.0
           #pragma profileoption MaxTexIndirections=16

           struct VertexData {
               float3 Vertex;
               float2 UV;
               float3 Normal;
               float3 Color;
           };
           
//#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
//#define FOG_COMBINED_WITH_WORLD_POS
           struct v2f
           {
               float2 uv : TEXCOORD0;
               UNITY_FOG_COORDS(1)
               SHADOW_COORDS(2) // put shadows data into TEXCOORD1
               float4 pos : SV_POSITION;
               fixed3 diff : COLOR0;
               fixed3 ambient : COLOR1;
               fixed3 diff_rev : COLOR2;
               fixed3 ambient_rev : COLOR3;
               float4 worldPos : TEXCOORD3;

               half3 worldNormal : NORMAL;
               half3 worldNormal_rev : NORMAL1;
           };
//#endif
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
              float3 color : COLOR2;

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
              float3 color : COLOR0;
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
              float3 color : COLOR2;

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
              float3 color : COLOR2;
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
              float3 color : COLOR0;
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
              float3 color : COLOR0;
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
              float3 color : COLOR0;
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
           };

           uniform sampler2D _permutation;

           sampler2D _MainTex;
           float4 _MainTex_ST;

           half _Glossiness;
           half _Metallic;
           half _Emission;
           half _Alpha;
           half _Occlusion;
           fixed _Cutoff;

           float __Time;
           float3 _WindDir;
           float _baseWindShere;
           float3 _scale;
           float3 _shift;
           float _gain;
           float _setup;

           uniform StructuredBuffer<VertexData> _Vertex;

           float _Ambience;

           float sqrMagnitude(float3 p)
           {
              return p.x * p.x + p.y * p.y + p.z * p.z;
           }

           float magnitude(float3 p)
           {
              return sqrt(sqrMagnitude(p));
           }

           float4 GetTangent(float3 normal)
           {
              float3 tangent;
              float3 t1 = cross(normal, float3(0, 0, 1));
              float3 t2 = cross(normal, float3(0, 1, 0));
              if (magnitude(t1) > magnitude(t2))
              {
                 tangent = t1;
              }
              else
              {
                 tangent = t2;
              }
              return float4(tangent, 1);
           }

           /*int perm(int d)
           {
              d = d % 256;
              float2 t = float2(d % 16, d / 16) / 15.0;
              return tex2Dlod(_permutation, float4(t, 0, 0)).r * 255;
           }

           float fade(float t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

           float lerp(float t, float a, float b) { return a + t * (b - a); }

           float grad(int hash, float x, float y, float z)
           {
              int h = hash % 16;										// & 15;
              float u = h < 8 ? x : y;
              float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
              return ((h % 2) == 0 ? u : -u) + (((h / 2) % 2) == 0 ? v : -v); 	// h&1, h&2 
           }

           float noise(float x, float y, float z)
           {
              int X = (int)floor(x) % 256;	// & 255;
              int Y = (int)floor(y) % 256;	// & 255;
              int Z = (int)floor(z) % 256;	// & 255;

              x -= floor(x);
              y -= floor(y);
              z -= floor(z);

              float u = fade(x);
              float v = fade(y);
              float w = fade(z);

              int A = perm(X) + Y;
              int AA = perm(A) + Z;
              int AB = perm(A + 1) + Z;
              int B = perm(X + 1) + Y;
              int BA = perm(B) + Z;
              int BB = perm(B + 1) + Z;

              return lerp(w, lerp(v, lerp(u, grad(perm(AA), x, y, z),
                 grad(perm(BA), x - 1, y, z)),
                 lerp(u, grad(perm(AB), x, y - 1, z),
                    grad(perm(BB), x - 1, y - 1, z))),
                 lerp(v, lerp(u, grad(perm(AA + 1), x, y, z - 1),
                    grad(perm(BA + 1), x - 1, y, z - 1)),
                    lerp(u, grad(perm(AB + 1), x, y - 1, z - 1),
                       grad(perm(BB + 1), x - 1, y - 1, z - 1))));
           }*/

           float hash(float n)
           {
              return frac(sin(n)*43758.5453);
           }

           float noise(float3 x)
           {
              // The noise function returns a value in the range -1.0f -> 1.0f

              float3 p = floor(x);
              float3 f = frac(x);

              f = f * f*(3.0 - 2.0*f);
              float n = p.x + p.y*57.0 + 113.0*p.z;

              return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                 lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                 lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                    lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
           }

           float4 ApplyWind(float3 worldPos)
           {
              float windSpeed = _baseWindShere * 10;

              float shifted_x = _shift.x + _WindDir.x * windSpeed * __Time;
              float shifted_z = _shift.z + _WindDir.z * windSpeed * __Time;

              float nx = worldPos.x * _scale.x + shifted_x;
              float nz = worldPos.z * _scale.z + shifted_z;

              float ns = abs(noise(float3(nx, 0, nz))*_baseWindShere*_gain);

              float3 shere = _WindDir * ns;

              return float4(shere, 0);
           }

        ENDCG

        /*Pass
        {
            Cull Off
            AlphaTest Greater [_Cutoff]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

           // make fog work
           #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap multi_compile_fog

            v2f vert(uint id : SV_VertexID)
            {
               v2f o;

               float4 in_v = float4(_Vertex[id].Vertex, 1);
               o.pos = UnityObjectToClipPos(in_v);
               o.uv = _Vertex[id].UV;
               //o.uv = TRANSFORM_TEX(_Vertex[id].UV, _MainTex);
               //UNITY_TRANSFER_FOG(o, o.pos);
               o.worldPos = mul(unity_ObjectToWorld, in_v);

               half3 worldNorm = UnityObjectToWorldNormal(_Vertex[id].Normal);;
               

               // diffuse and ambient lighting.
               half nl = max(0, dot(-worldNorm, _WorldSpaceLightPos0.xyz));
               o.diff = nl * _LightColor0.rgb;
               o.ambient = ShadeSH9(half4(-worldNorm, 1)) + _Ambience;
               o.worldNormal = -worldNorm;


               nl = max(0, dot(worldNorm, _WorldSpaceLightPos0.xyz));
               o.diff_rev = nl * _LightColor0.rgb;
               o.ambient_rev = ShadeSH9(half4(worldNorm, 1)) + _Ambience;
               o.worldNormal_rev = worldNorm;

               // compute shadows data
               TRANSFER_SHADOW(o)

               return o;
            }

            fixed4 frag(v2f i, half facing : VFACE) : SV_Target
            {
               // sample the texture
               //fixed4 col = fixed4(0.5, 0.5, 0.5, 0.5);//tex2D(_MainTex, i.uv);
               fixed4 col = tex2D(_MainTex, i.uv);

               fixed3 diff = i.diff;
               fixed3 ambient = i.ambient;
               half3 normal = i.worldNormal;

               if (facing < 0.5) {
                  normal = i.worldNormal_rev;
                  diff = i.diff_rev;
                  ambient = i.ambient_rev;
               }

               // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
               fixed shadow = SHADOW_ATTENUATION(i);
               // darken light's illumination with shadow, keep ambient intact
               fixed3 lighting = diff * shadow + ambient;
               col.rgb *= lighting;

               // apply fog
               //UNITY_APPLY_FOG(i.fogCoord, col);
               return col;
            }
            ENDCG
        }*/

        Pass
        {
           Name "FORWARD"
           Tags {"LightMode" = "ForwardBase"}

           AlphaTest Greater[_Cutoff]
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
               o.Alpha = col.a;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;

               clip(o.Alpha - _Cutoff);
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

            v2f_base vert(uint id : SV_VertexID)
            {
               v2f_base o;
               float4 in_v = float4(_Vertex[id].Vertex, 1);
               o.pos = UnityObjectToClipPos(in_v);
               o.uv = _Vertex[id].UV;
               o.worldPos = mul(unity_ObjectToWorld, in_v);
               o.worldNormal = UnityObjectToWorldNormal(_Vertex[id].Normal);
               o.color = _Vertex[id].Color;

               if (o.uv.y < 0.001)
                 o.pos += ApplyWind(o.worldPos.xyz);

               appdata_full v;
               UNITY_SETUP_INSTANCE_ID(v);
               v.vertex = in_v;
               v.normal = _Vertex[id].Normal;
               v.texcoord = float4(o.uv, 0, 0);
               v.tangent = GetTangent(v.normal);
               v.texcoord1 = 0;
               v.texcoord2 = 0;
               v.texcoord3 = 0;
               v.color = fixed4(1, 1, 1, 1);

               return ForwardBase_vert(v, o);
            }

            fixed4 frag(v2f_base i) : SV_Target
            {

                 fixed4 col = tex2D(_MainTex, i.uv) * fixed4(i.color, 1);
                 
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
           ColorMask RGB

           AlphaTest Greater[_Cutoff]
           CGPROGRAM

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

            v2f_Add vert(uint id : SV_VertexID)
            {
               UNITY_SETUP_INSTANCE_ID(v);
               v2f_Add o;
               float4 in_v = float4(_Vertex[id].Vertex, 1);
               o.pos = UnityObjectToClipPos(in_v);
               o.uv = _Vertex[id].UV;
               o.worldPos = mul(unity_ObjectToWorld, in_v);
               o.worldNormal = UnityObjectToWorldNormal(_Vertex[id].Normal);
               o.color = _Vertex[id].Color;

               if (o.uv.y < 0.001)
                  o.pos += ApplyWind(o.worldPos);

               appdata_full v;
               UNITY_SETUP_INSTANCE_ID(v);
               v.vertex = in_v;
               v.normal = _Vertex[id].Normal;
               v.texcoord = float4(o.uv, 0, 0);
               v.tangent = GetTangent(v.normal);
               v.texcoord1 = 0;
               v.texcoord2 = 0;
               v.texcoord3 = 0;
               v.color = fixed4(1, 1, 1, 1);


               UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
               UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
               return o;
            }

            fixed4 frag(v2f_Add i) : SV_Target
            {

                 fixed4 col = tex2D(_MainTex, i.uv) * fixed4(i.color, 1);

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
                 o.Alpha = col.a;
                 o.Occlusion = _Occlusion;
                 fixed3 normalWorldVertex = fixed3(0, 0, 1);
                 o.Normal = i.worldNormal;
                 normalWorldVertex = i.worldNormal;

                 clip(o.Alpha - _Cutoff);
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

           AlphaTest Greater[_Cutoff]
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

            v2f_Deferred vert(uint id : SV_VertexID)
            {
               v2f_Deferred o;
               float4 in_v = float4(_Vertex[id].Vertex, 1);
               o.pos = UnityObjectToClipPos(in_v);
               o.uv = _Vertex[id].UV;
               o.worldPos = mul(unity_ObjectToWorld, in_v);
               o.worldNormal = UnityObjectToWorldNormal(_Vertex[id].Normal);
               o.color = _Vertex[id].Color;

               if (o.uv.y < 0.001)
                  o.pos += ApplyWind(o.worldPos);

               appdata_full v;
               UNITY_SETUP_INSTANCE_ID(v);
               v.vertex = in_v;
               v.normal = _Vertex[id].Normal;
               v.texcoord = float4(o.uv, 0, 0);
               v.tangent = GetTangent(v.normal);
               v.texcoord1 = 0;
               v.texcoord2 = 0;
               v.texcoord3 = 0;
               v.color = fixed4(1, 1, 1, 1);

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
               fixed4 col = tex2D(_MainTex, i.uv) * fixed4(i.color, 1);


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
               o.Alpha = col.a;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;

               fixed3 originalNormal = o.Normal;
               half atten = 1;

               clip(o.Alpha - _Cutoff);

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



            }

           ENDCG
        }

        Pass
        {
           Name "Meta"
           Tags { "LightMode" = "Meta" }
           Cull Off

           AlphaTest Greater[_Cutoff]
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

            v2f_Meta vert(uint id : SV_VertexID)
            {
               v2f_Meta o;

               float4 in_v = float4(_Vertex[id].Vertex, 1);
               o.pos = UnityObjectToClipPos(in_v);
               o.uv = _Vertex[id].UV;
               o.worldPos = mul(unity_ObjectToWorld, in_v);
               o.worldNormal = UnityObjectToWorldNormal(_Vertex[id].Normal);
               o.color = _Vertex[id].Color;

               if (o.uv.y < 0.001)
                  o.pos += ApplyWind(o.worldPos);

                appdata_full v;
                UNITY_SETUP_INSTANCE_ID(v);
                v.vertex = in_v;
                v.normal = _Vertex[id].Normal;
                v.texcoord = float4(o.uv, 0, 0);
                v.tangent = GetTangent(v.normal);
                v.texcoord1 = 0;
                v.texcoord2 = 0;
                v.texcoord3 = 0;
                v.color = fixed4(1, 1, 1, 1);

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
               fixed4 col = tex2D(_MainTex, i.uv) * fixed4(i.color, 1);

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
               o.Alpha = col.a;
               o.Occlusion = _Occlusion;
               fixed3 normalWorldVertex = fixed3(0, 0, 1);
               o.Normal = i.worldNormal;
               normalWorldVertex = i.worldNormal;

               clip(o.Alpha - _Cutoff);
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
    }
}
