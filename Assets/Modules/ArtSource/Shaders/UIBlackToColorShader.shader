Shader "Custom/UIBlackToColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _ReplacementColor ("Black Replacement Color", Color) = (1, 0, 0, 1)
        _WhiteReplacementColor ("White Replacement Color", Color) = (0, 0, 1, 1)
        _BlackThreshold ("Black Threshold", Range(0, 1)) = 0.1
        _WhiteThreshold ("White Threshold", Range(0, 1)) = 0.9
        _Smoothness ("Color Transition Smoothness", Range(0.01, 1)) = 0.1
        _PreserveAlpha ("Preserve Original Alpha", Range(0, 1)) = 1
        
        // UI Masking properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ReplacementColor;
            fixed4 _WhiteReplacementColor;
            float _BlackThreshold;
            float _WhiteThreshold;
            float _Smoothness;
            float _PreserveAlpha;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Calculate the brightness of the pixel
                float brightness = (texColor.r + texColor.g + texColor.b) / 3.0;
                
                // Determine if this pixel is considered "black" or "white"
                float blackFactor = smoothstep(_BlackThreshold, 0, brightness);
                float whiteFactor = smoothstep(1, _WhiteThreshold, brightness);
                
                // Create smooth transitions
                float blackTransition = smoothstep(0, _Smoothness, blackFactor);
                float whiteTransition = smoothstep(0, _Smoothness, whiteFactor);
                
                // Mix the original color with replacement colors
                fixed4 finalColor;
                finalColor.rgb = lerp(texColor.rgb, _ReplacementColor.rgb, blackTransition);
                finalColor.rgb = lerp(finalColor.rgb, _WhiteReplacementColor.rgb, whiteTransition);
                
                // Handle alpha blending
                if (_PreserveAlpha > 0.5)
                {
                    // Preserve original alpha
                    finalColor.a = texColor.a * i.color.a;
                }
                else
                {
                    // Use replacement color alpha for black/white areas
                    float combinedTransition = max(blackTransition, whiteTransition);
                    finalColor.a = lerp(texColor.a, 
                        lerp(_ReplacementColor.a, _WhiteReplacementColor.a, whiteTransition), 
                        combinedTransition) * i.color.a;
                }
                
                // Apply vertex color tint
                finalColor.rgb *= i.color.rgb;
                
                // Apply UI masking (clip rect)
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    // Fallback for older hardware
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ReplacementColor;
            fixed4 _WhiteReplacementColor;
            float _BlackThreshold;
            float _WhiteThreshold;
            float _Smoothness;
            float _PreserveAlpha;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Calculate the brightness of the pixel
                float brightness = (texColor.r + texColor.g + texColor.b) / 3.0;
                
                // Simple threshold-based replacement for older hardware
                float blackFactor = step(brightness, _BlackThreshold);
                float whiteFactor = step(_WhiteThreshold, brightness);
                
                // Mix the original color with replacement colors
                fixed4 finalColor;
                finalColor.rgb = lerp(texColor.rgb, _ReplacementColor.rgb, blackFactor);
                finalColor.rgb = lerp(finalColor.rgb, _WhiteReplacementColor.rgb, whiteFactor);
                
                // Handle alpha blending
                if (_PreserveAlpha > 0.5)
                {
                    finalColor.a = texColor.a * i.color.a;
                }
                else
                {
                    float combinedFactor = max(blackFactor, whiteFactor);
                    finalColor.a = lerp(texColor.a, 
                        lerp(_ReplacementColor.a, _WhiteReplacementColor.a, whiteFactor), 
                        combinedFactor) * i.color.a;
                }
                
                // Apply vertex color tint
                finalColor.rgb *= i.color.rgb;
                
                // Apply UI masking (clip rect)
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"
}
