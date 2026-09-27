Shader "UI/GoldenTrophy"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Gold Colors)]
        _GoldColor1 ("Gold Dark", Color) = (0.75, 0.58, 0.1, 1)
        _GoldColor2 ("Gold Light", Color) = (1, 0.9, 0.5, 1)
        
        [Header(Shine Settings)]
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineWidth ("Shine Width", Range(0, 1)) = 0.1
        _ShineSpeed ("Shine Speed", Float) = 2.0
        _ShineFrequency ("Shine Interval (s)", Float) = 3.0
        _ShineAngle ("Shine Angle", Range(0, 360)) = 45
        _ShineIntensity ("Shine Intensity", Range(0, 5)) = 2.0
        
        [Header(Edge Blink)]
        _EdgeBlinkIntensity ("Edge Blink Power", Range(0, 5)) = 1.0
        _EdgeThickness ("Edge Thickness", Range(0, 0.5)) = 0.05

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
            
            float4 _GoldColor1;
            float4 _GoldColor2;
            float4 _ShineColor;
            float _ShineWidth;
            float _ShineSpeed;
            float _ShineFrequency;
            float _ShineAngle;
            float _ShineIntensity;
            float _EdgeBlinkIntensity;
            float _EdgeThickness;

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
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                // Base golden gradient based on Y coordinate
                float goldMask = IN.texcoord.y;
                float3 goldBase = lerp(_GoldColor1.rgb, _GoldColor2.rgb, goldMask);
                
                // Combine with original texture (preserving details)
                half4 color = texColor;
                color.rgb *= goldBase;
                color *= IN.color;

                // --- Periodic Shine Sweep ---
                float angleRad = _ShineAngle * 0.0174533; // Degrees to Radians
                float2 shineDir = float2(cos(angleRad), sin(angleRad));
                float proj = dot(IN.texcoord, shineDir);
                
                // Calculate time-based cycle for sweep
                float sweepTime = _Time.y * _ShineSpeed;
                float sweepCycle = fmod(sweepTime, _ShineFrequency);
                // Only show sweep during the first part of the frequency
                float sweepActive = step(sweepCycle, 1.5); // Sweep lasts 1.5s
                
                float sweepPos = lerp(-0.5, 1.5, sweepCycle / 1.5) * sweepActive;
                float shine = smoothstep(sweepPos - _ShineWidth, sweepPos, proj) * 
                              smoothstep(sweepPos + _ShineWidth, sweepPos, proj) * sweepActive;
                
                color.rgb += _ShineColor.rgb * shine * _ShineIntensity * texColor.a;

                // --- Edge Blink (Silhouette) ---
                // Sample alpha around current pixel to find edges of the sprite silhouette
                float2 uv = IN.texcoord;
                float4 alphaSamples;
                float offset = _EdgeThickness * 0.02;
                alphaSamples.x = tex2D(_MainTex, uv + float2(offset, 0)).a;
                alphaSamples.y = tex2D(_MainTex, uv + float2(-offset, 0)).a;
                alphaSamples.z = tex2D(_MainTex, uv + float2(0, offset)).a;
                alphaSamples.w = tex2D(_MainTex, uv + float2(0, -offset)).a;
                
                float edgeAlpha = max(max(alphaSamples.x, alphaSamples.y), max(alphaSamples.z, alphaSamples.w));
                float edgeMask = saturate(edgeAlpha - texColor.a);
                
                // Periodic blink for edges - happens every _ShineFrequency seconds
                float blinkTime = _Time.y;
                float blink = pow(0.5 + 0.5 * sin(blinkTime * 2.0 * 3.14159 / _ShineFrequency), 15.0);
                
                color.rgb += _ShineColor.rgb * edgeMask * blink * _EdgeBlinkIntensity * 2.0;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
            ENDCG
        }
    }
}
