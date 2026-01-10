Shader "Custom/PortalShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Portal Settings)]
        _PortalColor1 ("Portal Color 1", Color) = (0.2, 0.5, 1.0, 1.0) // Bright Blue
        _PortalColor2 ("Portal Color 2", Color) = (0.8, 0.3, 1.0, 1.0) // Bright Purple
        _PortalColor3 ("Portal Color 3", Color) = (0.1, 0.9, 1.0, 1.0) // Bright Cyan
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.2
        
        [Header(Animation)]
        _RotationSpeed ("Rotation Speed", Float) = 2.0
        _SwirlSpeed ("Swirl Speed", Float) = 3.0
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 0.3)) = 0.15
        
        [Header(Swirl Effect)]
        _SwirlIntensity ("Swirl Intensity", Range(1, 10)) = 5.0
        _SwirlTightness ("Swirl Tightness", Range(1, 20)) = 8.0
        
        [Header(Rings)]
        _RingCount ("Ring Count", Int) = 4
        _RingWidth ("Ring Width", Range(0.01, 0.3)) = 0.08
        _RingSpacing ("Ring Spacing", Range(0.1, 0.6)) = 0.2
        
        [Header(Center)]
        _CenterSize ("Center Void Size", Range(0.1, 0.6)) = 0.25
        _CenterGlow ("Center Glow Intensity", Range(0, 3)) = 1.5
        _OuterGlow ("Outer Glow", Range(0, 2)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

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
                float2 centerUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            
            float4 _PortalColor1;
            float4 _PortalColor2;
            float4 _PortalColor3;
            float _Brightness;
            
            float _RotationSpeed;
            float _SwirlSpeed;
            float _PulseSpeed;
            float _PulseAmount;
            
            float _SwirlIntensity;
            float _SwirlTightness;
            
            int _RingCount;
            float _RingWidth;
            float _RingSpacing;
            
            float _CenterSize;
            float _CenterGlow;
            float _OuterGlow;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                
                // Calculate center UV (0,0 is center, 1,1 is corner)
                o.centerUV = (v.uv - 0.5) * 2.0;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample sprite texture (for alpha mask if needed)
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Calculate distance from center
                float2 centerUV = i.centerUV;
                float dist = length(centerUV);
                
                // Clamp to circle with soft edge
                if (dist > 1.0)
                {
                    discard;
                }
                
                // Calculate angle for rotation
                float angle = atan2(centerUV.y, centerUV.x);
                float time = _Time.y;
                
                // Swirling effect - create spiral pattern (like reference images)
                float swirlAngle = angle + time * _SwirlSpeed;
                float swirlDist = dist * _SwirlTightness;
                float swirlPattern = sin(swirlAngle + swirlDist) * 0.5 + 0.5;
                
                // Rotating angle for color variation
                float rotatedAngle = angle + time * _RotationSpeed;
                
                // Pulse effect (subtle breathing)
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseAmount;
                float adjustedDist = dist / pulse;
                
                // Create concentric rings (visible like reference)
                float ringValue = 0.0;
                for (int ring = 0; ring < _RingCount; ring++)
                {
                    float ringRadius = (float(ring) + 1.0) * _RingSpacing;
                    float ringDist = abs(adjustedDist - ringRadius);
                    
                    // Smooth ring with falloff
                    float ringAlpha = 1.0 - smoothstep(0.0, _RingWidth, ringDist);
                    ringValue += ringAlpha * (1.0 - ring / float(_RingCount)) * 0.8; // Fade outer rings
                }
                
                // Dark center void (like reference images)
                float centerVoid = 0.0;
                if (adjustedDist < _CenterSize)
                {
                    // Dark void in center
                    centerVoid = smoothstep(0.0, _CenterSize * 0.5, adjustedDist);
                }
                else
                {
                    centerVoid = 1.0;
                }
                
                // Center glow ring (bright ring around void)
                float centerGlow = 0.0;
                float glowDist = abs(adjustedDist - _CenterSize);
                centerGlow = exp(-glowDist * 8.0) * _CenterGlow;
                
                // Outer glow (soft edge)
                float outerGlow = exp(-(1.0 - dist) * 3.0) * _OuterGlow;
                
                // Combine swirl pattern with rings
                float swirlIntensity = swirlPattern * _SwirlIntensity;
                float pattern = ringValue * 0.6 + swirlIntensity * 0.4 + centerGlow + outerGlow * 0.3;
                pattern = saturate(pattern);
                
                // Apply center void
                pattern *= centerVoid;
                
                // Color gradient based on swirl and distance
                float colorMix1 = sin(rotatedAngle * 2.0 + adjustedDist * 6.0) * 0.5 + 0.5;
                float colorMix2 = cos(rotatedAngle * 1.5 + adjustedDist * 4.0 + time) * 0.5 + 0.5;
                
                // Blend between colors
                fixed3 portalColor = lerp(_PortalColor1.rgb, _PortalColor2.rgb, colorMix1);
                portalColor = lerp(portalColor, _PortalColor3.rgb, colorMix2 * 0.6);
                
                // Apply brightness multiplier
                portalColor *= _Brightness;
                
                // Add brightness variation for pulsing
                float brightnessVariation = 0.9 + sin(time * _PulseSpeed * 2.0 + adjustedDist * 10.0) * 0.1;
                portalColor *= brightnessVariation;
                
                // Add bright highlights in swirl pattern
                float highlight = step(0.7, swirlPattern) * 0.5;
                portalColor += highlight;
                
                // Edge fade for smooth edges
                float edgeFade = 1.0 - smoothstep(0.85, 1.0, dist);
                
                // Final color - ensure good visibility
                fixed4 finalColor;
                finalColor.rgb = portalColor * pattern;
                finalColor.a = pattern * edgeFade * texColor.a * i.color.a;
                
                // Ensure minimum alpha for visibility
                finalColor.a = max(finalColor.a, pattern * 0.3);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
