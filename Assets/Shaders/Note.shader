Shader "Project/Note"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _FocusTex ("Judge Texture", 2D) = "white" {}
        _NearPanel ("Camera Control", float) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            
		    Blend SrcAlpha OneMinusSrcAlpha

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

            TEXTURE2D(_FocusTex);
            SAMPLER(sampler_FocusTex);
            half4 _FocusTex_ST;
            float _NearPanel;

            Varyings UnlitVertex(Attributes v)
            {
                //Initalize
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 temp = v.positionOS;
                //convert the vertex positions from object space to world space
                o.positionWS = TransformObjectToWorld(v.positionOS);
                temp.z = _NearPanel - o.positionWS.z;
                //convert the vertex positions from object space to clip space so they can be rendered
                o.positionCS = TransformObjectToHClip(temp);
                //TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)
                o.uv = TRANSFORM_TEX(v.uv, _FocusTex);
                o.color = v.color;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                //uv postion : left-buttom (0,0) right-top (1,1)
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_FocusTex, sampler_FocusTex, i.uv);
                clip(mainTex.a-0.001);
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
		    Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld ,v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _NearPanel;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float a = i.worldPos.z - _NearPanel + 5;
                clip(a);
                col.a = clamp(0,1,a);
                return col;
            }
            ENDCG
        }
        
    }
}
