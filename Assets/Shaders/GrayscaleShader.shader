Shader "Custom/GrayscaleShader"
{
    Properties
    {
        _GrayscaleIntensity ("Grayscale Intensity", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+1"
            "RenderType"="Transparent"
        }
        
        // Grab the screen content (what was rendered before this pass)
        GrabPass { }
        
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
            };

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _GrabTexture;
            float _GrayscaleIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample what was rendered before (the first material's output)
                fixed4 c = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grabPos));
                
                // Calculate grayscale value using luminance formula
                float gray = dot(c.rgb, float3(0.299, 0.587, 0.114));
                
                // Interpolate between original color and grayscale based on intensity
                fixed3 finalColor = lerp(c.rgb, fixed3(gray, gray, gray), _GrayscaleIntensity);
                
                return fixed4(finalColor, c.a);
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
}
