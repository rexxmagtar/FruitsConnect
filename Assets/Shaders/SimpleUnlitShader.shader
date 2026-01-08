Shader "Custom/SimpleUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Tiling ("Tiling", Vector) = (1,1,0,0)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _Tiling;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Apply tiling and offset
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) * _Tiling.xy + _Tiling.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture and multiply by color
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
}
