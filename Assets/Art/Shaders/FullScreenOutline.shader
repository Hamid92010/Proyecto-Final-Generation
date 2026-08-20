// FullScreenOutline.shader
// Outlines por deteccion de bordes (Sobel) para Unity 6 / URP 17.
// Pensado para usarse con un Full Screen Pass Renderer Feature.
// Basado en la tecnica del video de NedMakesGames (Sobel sobre profundidad y color),
// ampliada con deteccion sobre normales y correccion de angulo rasante.

Shader "FullScreen/ToonOutline"
{
    Properties
    {
        [HDR] _OutlineColor("Color de la línea", Color) = (0, 0, 0, 1)
        _OutlineThickness("Grosor en píxeles", Range(1, 6)) = 1

        [Header(Profundidad)]
        [Toggle(_USE_DEPTH_EDGES)] _UseDepthEdges("Usar profundidad", Float) = 1
        _DepthThreshold("Umbral de profundidad", Range(0.0001, 2)) = 0.02
        _DepthNormalThreshold("Correccion angulo rasante", Range(0, 1)) = 0.5
        _DepthNormalThresholdScale("Escala de la correccion", Range(0, 20)) = 7

        [Header(Normales)]
        [Toggle(_USE_NORMAL_EDGES)] _UseNormalEdges("Usar normales", Float) = 1
        _NormalThreshold("Umbral de normales", Range(0, 3)) = 0.4

        [Header(Color)]
        [Toggle(_USE_COLOR_EDGES)] _UseColorEdges("Usar color", Float) = 0
        _ColorThreshold("Umbral de color", Range(0, 3)) = 0.5

        [Header(Distancia)]
        _FadeStart("Distancia de inicio del fade", Float) = 40
        _FadeEnd("Distancia de fin del fade", Float) = 90
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ToonOutlinePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _USE_DEPTH_EDGES
            #pragma shader_feature_local_fragment _ _USE_NORMAL_EDGES
            #pragma shader_feature_local_fragment _ _USE_COLOR_EDGES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Vert(), Attributes, Varyings y _BlitTexture vienen de Blit.hlsl.
            // OJO: este archivo está en el paquete CORE, no en el universal.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float  _OutlineThickness;
            float  _DepthThreshold;
            float  _DepthNormalThreshold;
            float  _DepthNormalThresholdScale;
            float  _NormalThreshold;
            float  _ColorThreshold;
            float  _FadeStart;
            float  _FadeEnd;

            // Kernel 3x3. El indice 0 es la esquina superior izquierda.
            static const float2 kOffsets[9] =
            {
                float2(-1,  1), float2(0,  1), float2(1,  1),
                float2(-1,  0), float2(0,  0), float2(1,  0),
                float2(-1, -1), float2(0, -1), float2(1, -1)
            };

            static const float kSobelX[9] =
            {
                 1,  0, -1,
                 2,  0, -2,
                 1,  0, -1
            };

            static const float kSobelY[9] =
            {
                 1,  2,  1,
                 0,  0,  0,
                -1, -2, -1
            };

            float Luminance709(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.texcoord;
                float2 texel = (1.0 / _ScreenParams.xy) * _OutlineThickness;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepthCenter = SampleSceneDepth(uv);
                float depthCenter = LinearEyeDepth(rawDepthCenter, _ZBufferParams);

                float edge = 0.0;

                // ---------------------------------------------------------
                // Sobel sobre profundidad
                // ---------------------------------------------------------
                #if defined(_USE_DEPTH_EDGES)
                {
                    float gx = 0.0;
                    float gy = 0.0;

                    [unroll]
                    for (int i = 0; i < 9; i++)
                    {
                        float raw = SampleSceneDepth(uv + kOffsets[i] * texel);
                        float d = LinearEyeDepth(raw, _ZBufferParams);
                        gx += d * kSobelX[i];
                        gy += d * kSobelY[i];
                    }

                    float depthGradient = sqrt(gx * gx + gy * gy);

                    // Correccion de angulo rasante: en superficies muy inclinadas respecto
                    // a la camara (suelos, paredes en fuga) el gradiente de profundidad es
                    // enorme aunque no haya un borde real. Se sube el umbral en esos pixeles.
                    float3 normalCenter = SampleSceneNormals(uv);
                    float3 positionWS = ComputeWorldSpacePosition(uv, rawDepthCenter, UNITY_MATRIX_I_VP);
                    float3 viewDirWS = normalize(positionWS - GetCameraPositionWS());

                    float NdotV = 1.0 - dot(normalCenter, -viewDirWS);
                    float grazing = saturate((NdotV - _DepthNormalThreshold) / max(1e-4, 1.0 - _DepthNormalThreshold));
                    grazing = grazing * _DepthNormalThresholdScale + 1.0;

                    // El umbral escala con la distancia para que el grosor sea estable.
                    float depthThreshold = _DepthThreshold * depthCenter * grazing;

                    edge = max(edge, step(depthThreshold, depthGradient));
                }
                #endif

                // ---------------------------------------------------------
                // Sobel sobre normales
                // ---------------------------------------------------------
                #if defined(_USE_NORMAL_EDGES)
                {
                    float3 gx = 0.0;
                    float3 gy = 0.0;

                    [unroll]
                    for (int j = 0; j < 9; j++)
                    {
                        float3 n = SampleSceneNormals(uv + kOffsets[j] * texel);
                        gx += n * kSobelX[j];
                        gy += n * kSobelY[j];
                    }

                    float normalGradient = sqrt(dot(gx, gx) + dot(gy, gy));
                    edge = max(edge, step(_NormalThreshold, normalGradient));
                }
                #endif

                // ---------------------------------------------------------
                // Sobel sobre luminancia del color
                // ---------------------------------------------------------
                #if defined(_USE_COLOR_EDGES)
                {
                    float gx = 0.0;
                    float gy = 0.0;

                    [unroll]
                    for (int k = 0; k < 9; k++)
                    {
                        float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + kOffsets[k] * texel).rgb;
                        float l = Luminance709(c);
                        gx += l * kSobelX[k];
                        gy += l * kSobelY[k];
                    }

                    float colorGradient = sqrt(gx * gx + gy * gy);
                    edge = max(edge, step(_ColorThreshold, colorGradient));
                }
                #endif

                // Desvanecer las líneas a distancia para evitar ruido en el horizonte.
                float fade = 1.0 - saturate((depthCenter - _FadeStart) / max(1e-4, _FadeEnd - _FadeStart));
                edge *= fade;

                half3 result = lerp(sceneColor.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return half4(result, sceneColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
