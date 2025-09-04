Shader "Fog/MaskAndComposite"
{
    Properties
    {
        _FogTex ("Fog Mask (R)", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _Intensity ("Intensity (0..1)", Range(0,1)) = 0.9
    }

    SubShader
    {
        // ---------- PASS 0: Mask Writer ----------
        Tags { "RenderType"="Opaque" } 
        Cull Off ZWrite Off ZTest Always
        ColorMask R
        BlendOp Min
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex   vertMask
            #pragma fragment fragMask
            #include "UnityCG.cginc"

            float4 _WorldMin, _WorldMax;

            struct appdata { float3 vertex : POSITION; };  // world-space mesh
            struct v2f      { float4 pos    : SV_POSITION; };

            v2f vertMask (appdata v)
            {
                v2f o;
                float2 uv = (v.vertex.xy - _WorldMin.xy) / (_WorldMax.xy - _WorldMin.xy);
                float2 clip = uv * 2.0 - 1.0;
                o.pos = float4(clip, 0, 1);
                return o;
            }

            fixed4 fragMask (v2f i) : SV_Target
            {
                return fixed4(0,0,0,0);
            }
            ENDCG
        }

        // ---------- PASS 1: Composite Overlay ----------
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vertComp
            #pragma fragment fragComp
            #include "UnityCG.cginc"

            sampler2D _FogTex;
            float4 _FogColor;
            float  _Intensity;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f      { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vertComp (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 fragComp (v2f i) : SV_Target
            {
                // 1 = fog; 0 = clear
                float m = tex2D(_FogTex, i.uv).r;
                float a = saturate(m * _Intensity);
                return fixed4(_FogColor.rgb, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
