Shader "FX/ProceduralWater"
{
    Properties
    {
        _BaseColor("Shallow Color (RGBA)", Color) = (0.08, 0.55, 0.70, 0.45)
        _DeepColor("Deep Color (RGBA)", Color) = (0.02, 0.18, 0.28, 0.65)

        _ReflectionColor("Reflection Tint (RGB) Strength (A)", Color) = (0.60, 0.85, 1.00, 0.45)
        _SpecularColor("Specular Color (RGB) Strength (A)", Color) = (1, 1, 1, 0.35)
        _WorldLightDir("Specular Light Dir", Vector) = (0.3, 0.9, -0.2, 0.0)
        _Shininess("Shininess", Range(8.0, 500.0)) = 180.0

        _FresnelScale("Fresnel Scale", Range(0.0, 4.0)) = 1.0
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 4.0
        _FresnelBias("Fresnel Bias", Range(0.0, 1.5)) = 0.10

        _RefractionStrength("Refraction Strength", Range(0.0, 1.0)) = 0.65

        _DepthMax("Depth Color Range", Range(0.1, 50.0)) = 6.0
        _EdgeFade("Intersection Fade", Range(0.01, 5.0)) = 0.35
        _Absorption("Absorption", Range(0.0, 4.0)) = 0.75

        _Opacity("Opacity", Range(0.0, 1.0)) = 0.85
        _MinAlpha("Min Alpha (Intersections)", Range(0.0, 1.0)) = 0.18
        _DepthAlphaBoost("Depth Alpha Boost", Range(0.0, 2.0)) = 0.35

        _RippleScale("Ripple Scale", Range(0.05, 20.0)) = 2.5
        _RippleSpeed("Ripple Speed", Range(0.0, 5.0)) = 1.0
        _RippleStrength("Ripple Normal Strength", Range(0.0, 1.0)) = 0.55

        _FoamColor("Foam Color", Color) = (0.92, 0.97, 1.0, 1.0)
        _FoamIntensity("Foam Intensity", Range(0.0, 3.0)) = 1.0
        _ShoreDepth("Shore Depth", Range(0.01, 5.0)) = 0.9
        _FoamNoiseScale("Foam Noise Scale", Range(0.05, 20.0)) = 2.0
        _FoamNoiseSpeed("Foam Noise Speed", Range(0.0, 5.0)) = 1.1
        _FoamCrestThreshold("Crest Threshold", Range(0.0, 1.0)) = 0.30
        _FoamCrestIntensity("Crest Intensity", Range(0.0, 3.0)) = 1.0

        _EdgeFoamIntensity("Edge Foam Intensity", Range(0.0, 3.0)) = 1.0
        _EdgeFoamWidth("Edge Foam Width (Pixels)", Range(0.5, 6.0)) = 1.5
        _EdgeFoamThreshold("Edge Foam Threshold (Meters)", Range(0.001, 1.0)) = 0.08

        _GerstnerIntensity("Gerstner Intensity", Range(0.0, 3.0)) = 1.0
        _GAmplitude("Wave Amplitude", Vector) = (0.22 ,0.18, 0.14, 0.10)
        _GFrequency("Wave Frequency", Vector) = (1.25, 1.65, 1.10, 0.85)
        _GSteepness("Wave Steepness", Vector) = (0.85, 0.75, 0.65, 0.60)
        _GSpeed("Wave Speed", Vector) = (1.20, 1.45, 1.05, 0.80)
        _GDirectionAB("Wave Direction AB", Vector) = (0.25 ,0.85, 0.90, 0.20)
        _GDirectionCD("Wave Direction CD", Vector) = (0.10 ,0.95, 0.55, 0.55)
    }

        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 350
        ColorMask RGB

        GrabPass { "_RefractionTex" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _RefractionTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float4 _BaseColor;
            float4 _DeepColor;

            float4 _ReflectionColor;
            float4 _SpecularColor;
            float4 _WorldLightDir;
            float _Shininess;

            float _FresnelScale;
            float _FresnelPower;
            float _FresnelBias;

            float _RefractionStrength;

            float _DepthMax;
            float _EdgeFade;
            float _Absorption;

            float _Opacity;
            float _MinAlpha;
            float _DepthAlphaBoost;

            float _RippleScale;
            float _RippleSpeed;
            float _RippleStrength;

            float4 _FoamColor;
            float _FoamIntensity;
            float _ShoreDepth;
            float _FoamNoiseScale;
            float _FoamNoiseSpeed;
            float _FoamCrestThreshold;
            float _FoamCrestIntensity;

            float _EdgeFoamIntensity;
            float _EdgeFoamWidth;
            float _EdgeFoamThreshold;

            float _GerstnerIntensity;
            float4 _GAmplitude;
            float4 _GFrequency;
            float4 _GSteepness;
            float4 _GSpeed;
            float4 _GDirectionAB;
            float4 _GDirectionCD;

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(19.1, 7.7);

                v += a * Noise2(p); p = p * 2.02 + shift; a *= 0.5;
                v += a * Noise2(p); p = p * 2.03 + shift; a *= 0.5;
                v += a * Noise2(p); p = p * 2.01 + shift; a *= 0.5;
                v += a * Noise2(p);

                return v;
            }

            void GerstnerWave(float2 dir, float amplitude, float frequency, float steepness, float speed, float2 p, float t, inout float3 offset, inout float3 tangent, inout float3 binormal)
            {
                float2 d = normalize(dir);
                float k = max(0.0001, frequency);
                float w = speed * k;
                float phase = k * dot(d, p) + w * t;

                float s = sin(phase);
                float c = cos(phase);

                float qa = steepness * amplitude;

                offset.x += d.x * qa * c;
                offset.z += d.y * qa * c;
                offset.y += amplitude * s;

                tangent += float3(-d.x * d.x * qa * k * s, d.x * amplitude * k * c, -d.x * d.y * qa * k * s);
                binormal += float3(-d.x * d.y * qa * k * s, d.y * amplitude * k * c, -d.y * d.y * qa * k * s);
            }

            float3 ComputeGerstnerNormal(float3 tangent, float3 binormal)
            {
                float3 n = normalize(cross(binormal, tangent));
                return n;
            }

            float RippleHeight(float2 p, float t)
            {
                float2 q = p * _RippleScale;
                q += float2(t * _RippleSpeed, t * (_RippleSpeed * 0.73));
                float n = Fbm(q);
                float s1 = sin((q.x + q.y) * 2.1 + t * 1.7);
                float s2 = sin((q.x * 1.3 - q.y * 1.7) * 2.6 - t * 1.1);
                return (n * 0.7 + 0.3 * (0.5 + 0.25 * s1 + 0.25 * s2));
            }

            float3 RippleNormal(float2 p, float t)
            {
                float eps = 0.06 / max(0.05, _RippleScale);
                float h0 = RippleHeight(p, t);
                float hx = RippleHeight(p + float2(eps, 0.0), t);
                float hz = RippleHeight(p + float2(0.0, eps), t);

                float dhdx = (hx - h0) / eps;
                float dhdz = (hz - h0) / eps;

                return normalize(float3(-dhdx, 1.0, -dhdz));
            }

            float SampleSceneDepthLinear(float2 uv)
            {
                float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                return LinearEyeDepth(raw);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
                float eyeDepth : TEXCOORD5;
                UNITY_FOG_COORDS(6)
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos0 = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 p = worldPos0.xz;
                float t = _Time.y;

                float3 offset = float3(0, 0, 0);
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);

                GerstnerWave(_GDirectionAB.xy, _GAmplitude.x, _GFrequency.x, _GSteepness.x, _GSpeed.x, p, t, offset, tangent, binormal);
                GerstnerWave(_GDirectionAB.zw, _GAmplitude.y, _GFrequency.y, _GSteepness.y, _GSpeed.y, p, t, offset, tangent, binormal);
                GerstnerWave(_GDirectionCD.xy, _GAmplitude.z, _GFrequency.z, _GSteepness.z, _GSpeed.z, p, t, offset, tangent, binormal);
                GerstnerWave(_GDirectionCD.zw, _GAmplitude.w, _GFrequency.w, _GSteepness.w, _GSpeed.w, p, t, offset, tangent, binormal);

                offset *= _GerstnerIntensity;

                float4 displaced = v.vertex;
                displaced.xyz += mul(unity_WorldToObject, float4(offset, 0.0)).xyz;

                float3 worldPos = mul(unity_ObjectToWorld, displaced).xyz;
                float3 worldNormal = ComputeGerstnerNormal(tangent, binormal);

                o.pos = UnityObjectToClipPos(displaced);
                o.screenPos = ComputeScreenPos(o.pos);
                o.grabPos = ComputeGrabScreenPos(o.pos);

                o.worldPos = worldPos;
                o.worldNormal = worldNormal;

                float3 viewDir = _WorldSpaceCameraPos - worldPos;
                o.viewDir = viewDir;

                float3 viewPos = UnityObjectToViewPos(displaced);
                o.eyeDepth = -viewPos.z;

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 n0 = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);

                float t = _Time.y;
                float3 nRipple = RippleNormal(i.worldPos.xz, t);
                float3 n = normalize(lerp(n0, nRipple, saturate(_RippleStrength)));

                float2 uv = i.screenPos.xy / i.screenPos.w;

                float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                float sceneDepth = LinearEyeDepth(rawDepth);

                float depthDiffSigned = sceneDepth - i.eyeDepth;
                float depthDiffPos = max(0.0, depthDiffSigned);
                float submergedMask = step(0.0, depthDiffSigned);

                float soft = saturate(depthDiffPos / max(0.0001, _EdgeFade));
                soft = smoothstep(0.0, 1.0, soft);

                float depthLerp = saturate(depthDiffPos / max(0.0001, _DepthMax));

                float4 waterColor = lerp(_BaseColor, _DeepColor, depthLerp);

                float absorb = exp2(-depthDiffPos * _Absorption);
                float3 waterRgb = lerp(waterColor.rgb, waterColor.rgb * absorb, 0.85);

                float ndv = saturate(dot(n, v));
                float fresnel = pow(1.0 - ndv, _FresnelPower) * _FresnelScale + _FresnelBias;
                fresnel = saturate(fresnel);

                half4 refr = tex2Dproj(_RefractionTex, UNITY_PROJ_COORD(i.grabPos));
                float3 refrRgb = refr.rgb * lerp(1.0, absorb, 0.75);

                float refrToWater = saturate(_RefractionStrength + depthLerp * 0.55);
                float3 refrMix = lerp(refrRgb, waterRgb, refrToWater);

                float3 lightDir = normalize(_WorldLightDir.xyz);
                float3 h = normalize(lightDir + v);
                float spec = pow(saturate(dot(n, h)), _Shininess) * _SpecularColor.a;

                float shoreRaw = submergedMask * saturate(1.0 - depthDiffPos / max(0.0001, _ShoreDepth));
                float shore = pow(shoreRaw, 1.35);

                float crest = saturate((1.0 - n.y - _FoamCrestThreshold) / max(0.0001, 1.0 - _FoamCrestThreshold));
                crest = pow(crest, 1.2);

                float2 px = (_EdgeFoamWidth / _ScreenParams.xy);
                float dC = sceneDepth;
                float dR = SampleSceneDepthLinear(uv + float2(px.x, 0.0));
                float dL = SampleSceneDepthLinear(uv + float2(-px.x, 0.0));
                float dU = SampleSceneDepthLinear(uv + float2(0.0, px.y));
                float dD = SampleSceneDepthLinear(uv + float2(0.0, -px.y));

                float grad = max(max(abs(dC - dR), abs(dC - dL)), max(abs(dC - dU), abs(dC - dD)));
                float edgeFoam = smoothstep(_EdgeFoamThreshold, _EdgeFoamThreshold * 3.0, grad);
                edgeFoam = pow(edgeFoam, 1.4) * _EdgeFoamIntensity;

                edgeFoam *= shoreRaw;

                float foamNoise = Fbm(i.worldPos.xz * _FoamNoiseScale + float2(t * _FoamNoiseSpeed, -t * (_FoamNoiseSpeed * 0.83)));
                float foamBase = saturate(shore + crest * _FoamCrestIntensity + edgeFoam);
                float foamMask = saturate(foamBase * lerp(0.65, 1.35, foamNoise));
                foamMask = saturate(foamMask * foamMask);

                float3 foam = _FoamColor.rgb * (foamMask * _FoamIntensity);

                float3 reflectionTint = _ReflectionColor.rgb * _ReflectionColor.a;
                float3 col = lerp(refrMix, reflectionTint, fresnel);

                col += _SpecularColor.rgb * spec;
                col += foam;

                float baseAlpha = lerp(_BaseColor.a, _DeepColor.a, depthLerp);
                float alphaDeep = saturate(baseAlpha + depthLerp * _DepthAlphaBoost);
                float alphaFull = saturate(alphaDeep * _Opacity);
                float outAlpha = lerp(_MinAlpha, alphaFull, soft);

                half4 outCol = half4(col, outAlpha);
                UNITY_APPLY_FOG(i.fogCoord, outCol);
                return outCol;
            }
            ENDCG
        }
    }

        Fallback "Transparent/Diffuse"
}
