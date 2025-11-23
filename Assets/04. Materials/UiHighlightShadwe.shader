Shader "Custom/UiHighlightShadwe"
{
    Properties
    {
        _Color("Tint", Color) = (0,0,0,0.7)
        _HoleCenter("Hole Center", Vector) = (0.5,0.5,0,0)
        _HoleRadius("Hole Radius", Float) = 0.2
        _EdgeSoftness("Edge Softness", Float) = 0.05
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float2 _HoleCenter;
            float _HoleRadius;
            float _EdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 구멍까지의 거리 계산
                float dist = distance(i.uv, _HoleCenter);

            // 부드러운 경계 처리
            float alpha = smoothstep(_HoleRadius, _HoleRadius - _EdgeSoftness, dist);

            // 최종 색상
            fixed4 col = _Color;
            col.a *= alpha;
            return col;
        }
        ENDCG
    }
    }
}
