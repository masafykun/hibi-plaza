Shader "HibiPlaza/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.10, 0.62, 0.78, 0.72)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.92
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness;
        struct Input { float3 worldPos; };
        void vert(inout appdata_full v)
        {
            v.vertex.y += sin(v.vertex.x * 3.2 + _Time.y * 1.8) * 0.018
                + cos(v.vertex.z * 2.7 - _Time.y * 1.3) * 0.014;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float shimmer = sin((IN.worldPos.x + IN.worldPos.z) * 5.0 + _Time.y * 2.0) * 0.04;
            o.Albedo = _Color.rgb + shimmer;
            o.Metallic = 0.08;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
}
