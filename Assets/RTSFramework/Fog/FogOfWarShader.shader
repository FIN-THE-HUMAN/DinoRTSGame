Shader "RTSFramework/FogOfWarShader"
{
    Properties
    {
        _MainTex ("Fog Texture", 2D) = "white" {}
        _ShroudColor ("Shroud Color", Color) = (0.05, 0.05, 0.05, 0.55)
        _UnexploredColor ("Unexplored Color", Color) = (0.05, 0.05, 0.05, 1.0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _ShroudColor;
            float4 _UnexploredColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // R = current visibility, G = explored memory
                float4 fogVal = tex2D(_MainTex, i.uv);

                float visibility = fogVal.r;
                float explored = fogVal.g;

                if (visibility > 0.1f)
                {
                    // Visible
                    return float4(0, 0, 0, 0);
                }
                else if (explored > 0.1f)
                {
                    // Explored, but currently shrouded (no direct vision)
                    return _ShroudColor;
                }
                else
                {
                    // Completely unexplored (shroud of war)
                    return _UnexploredColor;
                }
            }
            ENDCG
        }
    }
}
