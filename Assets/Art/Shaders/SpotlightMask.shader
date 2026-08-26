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

        [Header(Luz del foco)]
        // La luz SUMA color dentro del mismo circulo que el velo deja limpio, en vez de
        // pintar encima: por eso son HDR y por eso pueden pasar de 1.
        [HDR] _Light_Inner_Color("Color en el centro", Color) = (1, 0.9, 0.65, 1)
        [HDR] _Light_Outer_Color("Color en el borde", Color) = (0.35, 0.25, 0.7, 1)

        // Arranca en cero para no encender de golpe los materiales que ya usan este
        // shader y no piden luz ninguna (M_Shadows). Subelo en M_Spotlight.
        _Light_Intensity("Intensidad (0 = sin luz)", Float) = 0
        _Light_Falloff("Concentracion (alto = luz apretada en el centro)", Float) = 2
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

            // Alfa PREMULTIPLICADO. Es lo que permite oscurecer y sumar luz en el MISMO
            // pase: con el color ya multiplicado por su alfa, un fragmento opaco tapa el
            // fondo igual que con la mezcla clasica, y uno de alfa cero se suma sin mas.
            // Para el velo el resultado es identico al de antes; la luz es lo que gana.
            // Sin escribir en el z-buffer: es un velo, no geometria.
            Blend One OneMinusSrcAlpha
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
                half4  _Light_Inner_Color;
                half4  _Light_Outer_Color;
                float  _Light_Intensity;
                float  _Light_Falloff;
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

                // El color pasa a venir premultiplicado por su alfa, como pide la mezcla
                // de arriba. Hasta aqui el velo se ve exactamente igual que antes.
                veil.rgb *= veil.a;

                // --- Luz del foco ---------------------------------------------------
                // 0 en el centro y 1 en el borde exterior del difuminado: la luz ocupa
                // justo el circulo que el velo deja limpio, ni mas ni menos.
                float lightBlend = saturate(distanceToPlayer / fadeEnd);

                // El degradado, de dentro hacia fuera
                half3 lightColor = lerp(_Light_Inner_Color.rgb, _Light_Outer_Color.rgb, lightBlend);

                // pow en vez de una rampa recta para poder apretar la luz en el centro
                // (exponente alto) o repartirla hasta el borde (cercano a 1)
                float lightFalloff = pow(1.0 - lightBlend, max(_Light_Falloff, 1e-4));

                // input.color.a la ata al tinte del SpriteRenderer: si el velo se
                // desvanece, la luz se desvanece con el en vez de quedarse flotando.
                half3 light = lightColor * (_Light_Intensity * lightFalloff * input.color.a);

                // Se suma SIN tocar el alfa. Con premultiplicado, aportar color con alfa
                // cero es exactamente una suma: la luz aclara lo que haya detras (el
                // personaje, el fondo) en lugar de pintarle un disco de color encima.
                veil.rgb += light;

                return veil;
            }
            ENDHLSL
        }
    }

    // Sin fallback: un velo transparente no debe heredar los pases de sombra ni de
    // profundidad de Lit, o acabaria proyectando sombra sobre la escena.
    FallBack Off
}
