Shader "Custom/UnlitLineTiled"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Tiling ("Tiling", Float) = 1.0
    }
    
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
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Tiling;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate world-space distance along the line
                // Get the change in world position per unit of UV.x
                float3 dWorld_dUV = ddx(i.worldPos) / max(abs(ddx(i.uv.x)), 0.0001);
                float worldDistPerUV = length(dWorld_dUV);
                
                // If derivative calculation fails, try alternative
                if (worldDistPerUV < 0.0001 || isnan(worldDistPerUV))
                {
                    float3 dWorld_dUV_alt = ddy(i.worldPos) / max(abs(ddy(i.uv.x)), 0.0001);
                    worldDistPerUV = length(dWorld_dUV_alt);
                }
                
                // Calculate distance along the line using UV coordinate
                // UV.x goes from 0 to 1 along the line
                // Multiply by world distance per UV to get actual world-space distance
                float distanceAlongLine = i.uv.x * worldDistPerUV;
                
                // Fallback: if calculation failed, use world position magnitude
                if (distanceAlongLine < 0.0001 || isnan(distanceAlongLine))
                {
                    distanceAlongLine = length(i.worldPos);
                }
                
                // Tile based on world-space distance along the line
                // _Tiling represents world units per texture tile
                float tiledU = distanceAlongLine / _Tiling;
                float2 tiledUV = float2(tiledU, i.uv.y);
                
                fixed4 texColor = tex2D(_MainTex, tiledUV);
                return texColor * i.color;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Texture"
}

