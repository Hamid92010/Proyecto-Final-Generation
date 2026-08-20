// SceneToon.shader
// Toon shader para Unity 6 / URP.
// Portado del tutorial de Roystan (roystan.net/articles/toon-shader), originalmente escrito
// para el Built-in Render Pipeline. La lógica es la misma: Blinn-Phong con smoothstep
// para bandear iluminación, especular y rim.

Shader "Toon/SceneToon"
{
    Properties
    {
        [MainTexture] _BaseMap("Textura base", 2D) = "white" {}
        [MainColor]   _BaseColor("Color base", Color) = (0.5, 0.65, 1, 1)

        [Header(Ambiental)]
        [HDR] _AmbientColor("Color ambiental", Color) = (0.4, 0.4, 0.4, 1)

        [Header(Especular)]
        [HDR] _SpecularColor("Color especular", Color) = (0.9, 0.9, 0.9, 1)
        _Glossiness("Glossiness", Float) = 32

        [Header(Rim)]
        [HDR] _RimColor("Color de rim", Color) = (1, 1, 1, 1)
        _RimAmount("Cantidad de rim", Range(0, 1)) = 0.716
        _RimThreshold("Umbral de rim", Range(0, 1)) = 0.1

        [Header(Banda de luz)]
        _ShadowEdge("Suavizado del borde", Range(0.001, 0.5)) = 0.01

        [Header(Opciones)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha clipping", Float) = 0
        _Cutoff("Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        // El CBUFFER es obligatorio para que el SRP Batcher acepte el material.
        // Las texturas van fuera (ya las declara SurfaceInput.hlsl).
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _AmbientColor;
            half4  _SpecularColor;
            half4  _RimColor;
            float  _Glossiness;
            float  _RimAmount;
            float  _RimThreshold;
            float  _ShadowEdge;
            float  _Cutoff;
            float  _Cull;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------------
        // Pass principal
        // ------------------------------------------------------------------
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // Sombras de la luz principal
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            // Luces adicionales (puntuales / spot)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // En URP 17 (Unity 6) la keyword de Forward+ se llama _CLUSTER_LIGHT_LOOP.
            // Si compilas contra URP 14-16, cámbiala por _FORWARD_PLUS.
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalInputs   = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS   = normalInputs.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord   = ComputeFogFactor(positionInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Coordenada de sombra (equivalente a TRANSFER_SHADOW / SHADOW_ATTENUATION)
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = float4(GetNormalizedScreenSpaceUV(IN.positionCS), 0.0, 1.0);
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #endif

                Light mainLight = GetMainLight(shadowCoord);
                float attenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // --- Iluminación direccional bandeada ---
                float NdotL = dot(mainLight.direction, normalWS);
                float lightIntensity = smoothstep(0.0, _ShadowEdge, NdotL * attenuation);
                float3 light = lightIntensity * mainLight.color;

                // --- Especular (Blinn-Phong recortado) ---
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float NdotH = dot(normalWS, halfVector);
                float specularIntensity = pow(saturate(NdotH) * lightIntensity, _Glossiness * _Glossiness);
                float specularSmooth = smoothstep(0.005, 0.01, specularIntensity);
                float3 specular = specularSmooth * _SpecularColor.rgb;

                // --- Rim light (solo en la cara iluminada) ---
                float rimDot = 1.0 - dot(viewDirWS, normalWS);
                float rimIntensity = rimDot * pow(saturate(NdotL), _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                float3 rim = rimIntensity * _RimColor.rgb;

                // --- Luces adicionales, con el mismo bandeado ---
                float3 additionalLight = 0.0;
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalWS = normalWS;
                    inputData.viewDirectionWS = viewDirWS;
                    inputData.shadowCoord = shadowCoord;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                    uint additionalLightsCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(additionalLightsCount)
                        Light extraLight = GetAdditionalLight(lightIndex, IN.positionWS, half4(1, 1, 1, 1));
                        float extraAtten = extraLight.shadowAttenuation * extraLight.distanceAttenuation;
                        float extraNdotL = dot(extraLight.direction, normalWS);
                        additionalLight += smoothstep(0.0, _ShadowEdge, extraNdotL * extraAtten) * extraLight.color;
                    LIGHT_LOOP_END
                #endif

                half4 color = albedo;
                color.rgb *= (_AmbientColor.rgb + light + additionalLight + specular + rim);
                color.rgb = MixFog(color.rgb, IN.fogCoord);
                color.a = albedo.a;
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Sombras proyectadas (equivale al UsePass "...SHADOWCASTER" del tutorial)
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth y DepthNormals: necesarios para depth prepass, SSAO y para
        // Renderer Features de detección de bordes (outlines full screen).
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
