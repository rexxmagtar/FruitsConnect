Shader "UI/RotatingShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Ray Settings)]
        _RayCount ("Ray Count", Float) = 12
        _RaySpeed ("Ray Rotation Speed", Float) = 1.0
        _RaySharpness ("Ray Sharpness", Range(0.1, 50)) = 2.0
        
        [Header(Glow Settings)]
        _CenterGlowSize ("Center Glow Size", Range(0, 1)) = 0.2
        _OuterFade ("Outer Fade", Range(0, 1)) = 0.5
        
        [Header(Colors)]
        _InnerColor ("Inner Color", Color) = (1, 0.9, 0.5, 1)
        _OuterColor ("Outer Color", Color) = (1, 0.7, 0, 1)

        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _RayCount;
            float _RaySpeed;
            float _RaySharpness;
            float _CenterGlowSize;
            float _OuterFade;
            fixed4 _InnerColor;
            fixed4 _OuterColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Center UVs (-0.5 to 0.5)
                float2 uv = IN.texcoord - 0.5;
                
                // Polar Coordinates
                float dist = length(uv) * 2.0; // 0 to 1 distance
                float angle = atan2(uv.y, uv.x);
                
                // 1. Ray Pattern
                // Add rotation to the angle
                float rotation = _Time.y * _RaySpeed;
                float rays = sin(angle * _RayCount + rotation);
                
                // Sharpen rays using power or smoothstep
                rays = pow(abs(rays), _RaySharpness);
                
                // 2. Glow and Fading
                // Distance fade (outer circle)
                float mask = smoothstep(_OuterFade, _CenterGlowSize, dist);
                
                // 3. Color blending
                // Mix inner and outer colors based on distance
                fixed4 finalColor = lerp(_OuterColor, _InnerColor, saturate(1.0 - dist));
                
                // Apply ray intensity
                float finalAlpha = rays * mask;
                
                // Add a solid center glow
                float centerGlow = smoothstep(_CenterGlowSize, 0.0, dist);
                finalAlpha = saturate(finalAlpha + centerGlow);
                
                finalColor.a *= finalAlpha * IN.color.a;

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return finalColor;
            }
            ENDCG
        }
    }
}
