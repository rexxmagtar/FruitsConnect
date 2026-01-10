Shader "Custom/GrayscaleShader"
{
    Properties
    {
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Contrast ("Contrast", Range(0, 2)) = 1.0
        _EffectPower ("Effect Power", Range(0, 1)) = 1.0
        _MeshMinY ("Mesh Min Y", Float) = 0.0
        _MeshMaxY ("Mesh Max Y", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        // Grab the screen behind the object into _BackgroundTexture
        GrabPass
        {
            "_BackgroundTexture"
        }

        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float worldPosY : TEXCOORD1;
            };
            
            sampler2D _BackgroundTexture;
            float _Brightness;
            float _Contrast;
            float _EffectPower;
            float _MeshMinY;
            float _MeshMaxY;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                // Get world space Y position
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPosY = worldPos.y;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample what was already rendered (first material's output)
                fixed4 col = tex2Dproj(_BackgroundTexture, UNITY_PROJ_COORD(i.grabPos));
                
                // Convert to grayscale using gamma-correct luminance formula
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Apply brightness and contrast adjustments
                gray = (gray - 0.5) * _Contrast + 0.5;
                gray = gray * _Brightness;
                
                // Clamp the value to ensure it stays in valid range
                gray = saturate(gray);
                
                // Calculate normalized Y position (0 = bottom, 1 = top)
                float normalizedY = (i.worldPosY - _MeshMinY) / (_MeshMaxY - _MeshMinY);
                normalizedY = saturate(normalizedY); // Clamp to 0-1
                
                // Determine if this fragment should be grayscale based on Y position
                // _EffectPower = 1.0 means all grayscale, 0.0 means all color
                // We want color to appear from bottom to top as _EffectPower decreases
                // Progress threshold: 1.0 - _EffectPower (when _EffectPower = 0, threshold = 1.0 = all color)
                float progressThreshold = 1.0 - _EffectPower;
                
                // Smooth transition zone for color covering animation
                float transitionZone = 0.05; // Small transition zone for smoothness
                
                // Calculate grayscale factor with smooth transition
                // If normalizedY is below threshold, show color (grayscaleFactor = 0)
                // If normalizedY is above threshold, show grayscale (grayscaleFactor = 1)
                // Smooth transition at the boundary
                float grayscaleFactor = 0.0;
                if (normalizedY < progressThreshold - transitionZone)
                {
                    // Below threshold - fully colored
                    grayscaleFactor = 0.0;
                }
                else if (normalizedY > progressThreshold + transitionZone)
                {
                    // Above threshold - fully grayscale
                    grayscaleFactor = 1.0;
                }
                else
                {
                    // In transition zone - smooth interpolation
                    float t = (normalizedY - (progressThreshold - transitionZone)) / (transitionZone * 2.0);
                    grayscaleFactor = smoothstep(0.0, 1.0, t);
                }
                
                fixed4 grayscaleColor = fixed4(gray, gray, gray, col.a);
                fixed4 finalColor = lerp(col, grayscaleColor, grayscaleFactor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
} 