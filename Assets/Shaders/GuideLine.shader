//CustomEditorExtension Shader Form
Shader "Project/GuideLine"
{
    Properties
    {
        // Main Texture To Show Appearances
        [PerRendererData] 
        _MainTex ("Sprite Texture", 2D) = "white" {}
        // Main Color To Draw On View
        _Color ("Tint", Color) = (1,1,1,1)
        _NearPanel("Near Panel",float) = -200

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            //Create Default(None) Or DEBUG_DISPLAY(Variant)
            #pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                float3  positionWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float _NearPanel;

            Varyings UnlitVertex(Attributes v)
            {
                //Initalize
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                //convert the vertex positions from object space to clip space so they can be rendered
                o.positionCS = TransformObjectToHClip(v.positionOS);
                //convert the vertex positions from object space to world space
                o.positionWS = TransformObjectToWorld(v.positionOS);
                //TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                clip(i.positionWS.z - _NearPanel);

                //uv postion : left-buttom (0,0) right-top (1,1)
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif
                
                float dis = i.positionWS.z - _NearPanel;
                clip(dis);
                float a = 1 - clamp(0,1, 0.05 * dis);
                mainTex.a = a;

                return mainTex;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                float3  positionWS      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _NearPanel;

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.positionWS = TransformObjectToWorld(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif
                
                float a = i.positionWS.z - _NearPanel;
                clip(a);
                float far = clamp(0,1,0.2 * ( _NearPanel + 200 - i.positionWS.z));
                clip(far);
                mainTex.a = clamp(0,1,a) * mainTex.a * far;

                return mainTex;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
