Shader "AD/Billboard"
{
    Properties
    {
        [PerRendererData]  _MainTex ("Texture", 2D) = "white" {}

        _VecticalBillboarding ("Vertical Restraints",Range(0,1)) = 1
        
        [Header(Stencil)]
        //[Enum(Never,1,Less,2,Equal,3,LEqual,4,Greater,5,NotEqual,6,GEqual,7,AlwaysRender,8)] 
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        [IntRange] _Stencil ("Stencil ID", Range(0,255)) = 0
        //[Enum(Keep,1,Zero,2,Replace,3,IncrSat,4,DecrSat,5,Invert,6,IncrWrap,7,DecrWrap,8)]
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilOp ("Stencil Operation", Float) = 0
        [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0,255)) = 255
        [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0,255)) = 255

        [Header(Color)]
        _MainColor ("Main Color", Color) = (1,1,1,1)
        [IntRange] _ColorMask ("Color Mask", Range(0,16)) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _VecticalBillboarding;

            float4 _MainColor;

            v2f vert (appdata v)
            {   
                v2f o;
                float3 center = float3(0, 0, 0);
                float3 viewer = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos, 1));
                float3 normalDir = viewer - center;
                normalDir.y =normalDir.y * _VecticalBillboarding;
                normalDir = normalize(normalDir);
                float3 upDir = abs(normalDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 rightDir = normalize(cross(normalDir, upDir));
                upDir = normalize(cross(rightDir, normalDir));
                float3 centerOffs = v.vertex.xyz - center;
                float3 localPos = center + rightDir * centerOffs.x + upDir * centerOffs.y + normalDir * centerOffs.z;
                o.vertex = UnityObjectToClipPos(float4(localPos, 1));
                o.uv = TRANSFORM_TEX(v.vertex, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                col.xyz = col.xyz * _MainColor.xyz;
                col.w = col.w * _MainColor.w;

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}