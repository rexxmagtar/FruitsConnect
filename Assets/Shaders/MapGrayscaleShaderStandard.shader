Shader "Custom/MapGrayscaleShaderStandard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ColorRadius ("Color Radius", Float) = 3.0
        _SmoothFalloff ("Smooth Falloff", Range(0, 1)) = 0.3
        _GlobalColorBlend ("Global Color Blend", Range(0, 1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
        }
        LOD 200
        
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _ColorRadius;
        float _SmoothFalloff;
        float _GlobalColorBlend;
        half _Glossiness;
        half _Metallic;
        
        // Arrays for node positions (max 32 nodes)
        float4 _ConnectedNodePositions[32];
        int _ConnectedNodeCount;
        float4 _ConsumerPositions[32];
        int _ConsumerCount;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample original texture color
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Calculate grayscale using luminance formula
            float gray = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
            fixed3 grayscaleColor = fixed3(gray, gray, gray);
            
            // Check distance to connected nodes
            float minDistanceToColoredZone = 999999.0;
            
            // Check connected nodes
            for (int j = 0; j < _ConnectedNodeCount && j < 32; j++)
            {
                float3 nodePos = _ConnectedNodePositions[j].xyz;
                float2 nodePos2D = nodePos.xz; // Use XZ plane (assuming Y is up)
                float2 worldPos2D = IN.worldPos.xz;
                
                float dist = distance(worldPos2D, nodePos2D);
                if (dist < minDistanceToColoredZone)
                {
                    minDistanceToColoredZone = dist;
                }
            }
            
            // Check consumers
            for (int k = 0; k < _ConsumerCount && k < 32; k++)
            {
                float3 consumerPos = _ConsumerPositions[k].xyz;
                float2 consumerPos2D = consumerPos.xz; // Use XZ plane
                float2 worldPos2D = IN.worldPos.xz;
                
                float dist = distance(worldPos2D, consumerPos2D);
                if (dist < minDistanceToColoredZone)
                {
                    minDistanceToColoredZone = dist;
                }
            }
            
            // Determine if we're in a colored zone
            float colorFactor = 0.0;
            
            if (minDistanceToColoredZone < _ColorRadius)
            {
                // Calculate smooth falloff from edge to center
                float normalizedDist = minDistanceToColoredZone / _ColorRadius;
                float falloffStart = 1.0 - _SmoothFalloff;
                
                if (normalizedDist < falloffStart)
                {
                    // Full color in center
                    colorFactor = 1.0;
                }
                else
                {
                    // Smooth transition at edges
                    float t = (normalizedDist - falloffStart) / _SmoothFalloff;
                    colorFactor = 1.0 - smoothstep(0.0, 1.0, t);
                }
            }
            
            // Lerp between grayscale and original color based on distance
            fixed3 distanceBasedColor = lerp(grayscaleColor, texColor.rgb, colorFactor);
            
            // Apply global color blend (0 = full color everywhere, 1 = distance-based coloring)
            // This allows smooth transition from distance-based to full color globally
            fixed3 finalColor = lerp(texColor.rgb, distanceBasedColor, _GlobalColorBlend);
            
            // Set surface properties for Standard shader
            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = texColor.a;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
