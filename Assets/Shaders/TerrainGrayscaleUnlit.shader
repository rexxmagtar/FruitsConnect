Shader "Custom/TerrainGrayscaleUnlit"
{
    Properties
    {
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        
        _Color ("Color", Color) = (1,1,1,1)
        _ColorRadius ("Color Radius", Float) = 3.0
        _SmoothFalloff ("Smooth Falloff", Range(0, 1)) = 0.3
        _GlobalColorBlend ("Global Color Blend", Range(0, 1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            "Queue"="Geometry-100"
        }
        
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_Control : TEXCOORD0;
                float2 uv_Splat : TEXCOORD1;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _Control;
            float4 _Control_ST;
            sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
            float4 _Splat0_ST;
            fixed4 _Color;
            
            float _ColorRadius;
            float _SmoothFalloff;
            float _GlobalColorBlend;
            
            // Arrays for node positions (max 32 nodes)
            float4 _ConnectedNodePositions[32];
            int _ConnectedNodeCount;
            float4 _ConsumerPositions[32];
            int _ConsumerCount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_Control = TRANSFORM_TEX(v.uv, _Control);
                o.uv_Splat = TRANSFORM_TEX(v.uv, _Splat0);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample splat control map
                fixed4 splatControl = tex2D(_Control, i.uv_Control);
                
                // Sample and blend terrain textures
                fixed4 col = splatControl.r * tex2D(_Splat0, i.uv_Splat);
                col += splatControl.g * tex2D(_Splat1, i.uv_Splat);
                col += splatControl.b * tex2D(_Splat2, i.uv_Splat);
                col += splatControl.a * tex2D(_Splat3, i.uv_Splat);
                col *= _Color;
                
                // Calculate grayscale using luminance formula
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                fixed3 grayscaleColor = fixed3(gray, gray, gray);
                
                // Determine if we're in a colored zone
                float colorFactor = 0.0;
                float2 worldPos2D = i.worldPos.xz;
                
                // Check connected nodes
                for (int j = 0; j < _ConnectedNodeCount && j < 32; j++)
                {
                    float3 nodePos = _ConnectedNodePositions[j].xyz;
                    float dist = distance(worldPos2D, nodePos.xz);
                    
                    if (dist < _ColorRadius)
                    {
                        float normalizedDist = dist / _ColorRadius;
                        float falloffStart = 1.0 - _SmoothFalloff;
                        float factor = (normalizedDist < falloffStart) ? 1.0 : (1.0 - smoothstep(0.0, 1.0, (normalizedDist - falloffStart) / _SmoothFalloff));
                        colorFactor = max(colorFactor, factor);
                    }
                }
                
                // Check consumers
                for (int k = 0; k < _ConsumerCount && k < 32; k++)
                {
                    float3 consumerPos = _ConsumerPositions[k].xyz;
                    float dist = distance(worldPos2D, consumerPos.xz);
                    
                    if (dist < _ColorRadius)
                    {
                        float normalizedDist = dist / _ColorRadius;
                        float falloffStart = 1.0 - _SmoothFalloff;
                        float factor = (normalizedDist < falloffStart) ? 1.0 : (1.0 - smoothstep(0.0, 1.0, (normalizedDist - falloffStart) / _SmoothFalloff));
                        colorFactor = max(colorFactor, factor);
                    }
                }
                
                // Lerp between grayscale and original color based on distance
                fixed3 distanceBasedColor = lerp(grayscaleColor, col.rgb, colorFactor);
                
                // Apply global color blend
                fixed3 finalColor = lerp(col.rgb, distanceBasedColor, _GlobalColorBlend);
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Texture"
}
