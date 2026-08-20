// SpotlightMask.shader
// Velo oscuro semitransparente con un agujero circular DIFUMINADO alrededor del personaje.
// Es el mismo truco que la mascara de las rejas (SG_BG_Texture): se mide la distancia de
// cada pixel al centro del foco en espacio de mundo. La diferencia esta en el corte:
//   - las rejas usan Step        -> borde duro, el pixel esta o no esta
//   - este velo usa smoothstep   -> el borde se desvanece en una corona de anchura ajustable
//
// El centro (_Player_Position) lo escribe el componente MathematicalMask, el mismo que ya
// alimenta a las rejas. Ojo: NO es la posicion cruda del jugador, sino su proyeccion sobre
// el plano de este sprite vista desde la camara. Sin esa proyeccion el foco se despegaria
// del personaje y, como el nodo mide en 3D, la diferencia de profundidad se comeria el radio.

Shader "Scene/SpotlightMask"
{
    Properties
    {
        [Header(Velo)]
        _Overlay_Color("Color del velo (el alfa manda la oscuridad)", Color) = (0, 0, 0, 0.8)
        _Image_Texture("Textura del velo (opcional)", 2D) = "white" {}

        [Header(Foco)]
        // Lo escribe MathematicalMask en cada frame. Se deja expuesto para poder ver el valor
        // en el Inspector mientras se depura.
        _Player_Position("Centro del foco (lo escribe MathematicalMask)", Vector) = (0, 0, 0, 0)
        _Mask_Radius("Radio de la zona limpia", Float) = 1
        _Mask_Softness("Anchura del difuminado", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SpotlightOverlay"
            Tags { "LightMode" = "UniversalForward" }

            // Mezcla alfa clasica. Sin escribir en el z-buffer: es un velo, no geometria.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Las texturas van fuera del CBUFFER o el SRP Batcher rechaza el material.
            TEXTURE2D(_Image_Texture);
            SAMPLER(sampler_Image_Texture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Image_Texture_ST;
                half4  _Overlay_Color;
                float4 _Player_Position;
                float  _Mask_Radius;
                float  _Mask_Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Hace falta la posicion de MUNDO del pixel: el foco vive en el mundo, no en
                // las UV del sprite, asi que el velo puede moverse sin arrastrar el agujero.
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;

                output.uv = TRANSFORM_TEX(input.uv, _Image_Texture);

                // Color de vertice: es por donde entra el tinte del SpriteRenderer.
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 veil = SAMPLE_TEXTURE2D(_Image_Texture, sampler_Image_Texture, input.uv)
                           * _Overlay_Color
                           * input.color;

                float distanceToPlayer = distance(input.positionWS, _Player_Position.xyz);

                // smoothstep(a, b, x) da 0 por debajo de a, 1 por encima de b y una rampa
                // suave entre medias. Aqui: 0 = agujero limpio en el centro, 1 = velo entero.
                // El max() evita que a == b, que dejaria el borde duro y con division por cero.
                float fadeStart = _Mask_Radius;
                float fadeEnd   = _Mask_Radius + max(_Mask_Softness, 1e-4);
                float veilAmount = smoothstep(fadeStart, fadeEnd, distanceToPlayer);

                veil.a *= veilAmount;

                return veil;
            }
            ENDHLSL
        }
    }

    // Sin fallback: un velo transparente no debe heredar los pases de sombra ni de
    // profundidad de Lit, o acabaria proyectando sombra sobre la escena.
    FallBack Off
}
