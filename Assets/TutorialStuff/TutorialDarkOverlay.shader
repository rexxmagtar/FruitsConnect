Shader "Custom/TutorialDarkOverlay"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,0.8)
        _RectX ("Rectangle X", Float) = 0
        _RectY ("Rectangle Y", Float) = 0
        _RectWidth ("Rectangle Width", Float) = 100
        _RectHeight ("Rectangle Height", Float) = 100
        _SmoothSize ("Smooth Border Size", Float) = 1
        [KeywordEnum(Ellipse, Rectangle)] _ShapeType ("Shape Type", Float) = 0
        _CornerRadius ("Corner Radius", Float) = 10
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 screenPos : TEXCOORD1;
            };

            float4 _Color;
            float _RectX;
            float _RectY;
            float _RectWidth;
            float _RectHeight;
            float _SmoothSize;
            float _ShapeType;
            float _CornerRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex).xy * _ScreenParams.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(_RectX + _RectWidth/2, _RectY + _RectHeight/2);
                float2 pos = i.screenPos - center;
                float2 radius = float2(_RectWidth/2, _RectHeight/2);
                
                float alpha = 1;
                
                if (_ShapeType < 0.5) // Ellipse
                {
                    float2 normalizedPos = pos / radius;
                    float ellipseDistance = dot(normalizedPos, normalizedPos) - 1.0;
                    
                    if (ellipseDistance < 0)
                    {
                        alpha = 0;
                    }
                    else
                    {
                        float distanceToEllipse = ellipseDistance * length(radius) / _SmoothSize;
                        alpha = saturate(distanceToEllipse);
                    }
                }
                else // Rectangle with rounded corners
                {
                    float2 absPos = abs(pos);
                    float2 rectDistance = absPos - radius + _CornerRadius;
                    float maxDistance = max(rectDistance.x, rectDistance.y);
                    
                    // Calculate distance to rounded corners
                    float2 cornerPos = max(rectDistance, 0.0);
                    float cornerDistance = length(cornerPos) - _CornerRadius;
                    
                    if (maxDistance < 0)
                    {
                        alpha = 0;
                    }
                    else
                    {
                        float distanceToRect = min(maxDistance, cornerDistance) / _SmoothSize;
                        alpha = saturate(distanceToRect);
                    }
                }
                
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
} 