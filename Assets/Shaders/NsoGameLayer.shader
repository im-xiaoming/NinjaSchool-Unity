// Shows the game's own drawing (a RenderTexture) on a quad in the scene.
// Opaque keeps colours as drawn and only treats never-painted pixels (alpha 0) as see-through, because the game
// blends with straight alpha and would otherwise leave semi-transparent holes where overlays were drawn.
Shader "NSO/Game Layer"
{
    Properties
    {
        _MainTex ("Game texture", 2D) = "black" {}
        [Toggle] _Opaque ("Opaque", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Opaque;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (_Opaque > 0.5) c.a = step(0.001, c.a);
                return c;
            }
            ENDCG
        }
    }
}
