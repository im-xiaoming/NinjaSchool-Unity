// Anti-aliasing (FXAA) for the whole game picture. NsoGameLayer runs it once per frame over the game's render
// texture, before showing it: it finds hard luminance edges (jagged steps on sprites, rotated art, scaled images, text)
// and blends across them. Alpha is kept as drawn, so the Game Layer's opaque / see-through handling is unchanged.
// Based on the public FXAA 3.11 "quality" algorithm (edge direction + end-of-edge search + sub-pixel blend).
Shader "NSO/FXAA"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _ContrastThreshold ("Contrast Threshold", Range(0.0312, 0.0833)) = 0.0312
        _RelativeThreshold ("Relative Threshold", Range(0.063, 0.333)) = 0.063
        _SubpixelBlending ("Subpixel Blending", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // x = 1/width, y = 1/height
            float _ContrastThreshold;       // edges darker than this (absolute) are skipped
            float _RelativeThreshold;       // ... or than this part of the brightest neighbour
            float _SubpixelBlending;        // strength of the blur on single-pixel details

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Luma(float2 uv)
            {
                return dot(tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb, float3(0.299, 0.587, 0.114));
            }

            float LumaAt(float2 uv, float dx, float dy)
            {
                return Luma(uv + _MainTex_TexelSize.xy * float2(dx, dy));
            }

            // step sizes (in pixels) used while walking along an edge to find its two ends
            #define EDGE_STEP_COUNT 10
            static const float edgeSteps[EDGE_STEP_COUNT] = { 1, 1.5, 2, 2, 2, 2, 2, 2, 2, 4 };
            #define EDGE_GUESS 8

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texel = _MainTex_TexelSize.xy;
                float4 centre = tex2Dlod(_MainTex, float4(uv, 0, 0));

                // 3x3 neighbourhood luminance (n = up, s = down in texture space)
                float m = dot(centre.rgb, float3(0.299, 0.587, 0.114));
                float n = LumaAt(uv, 0, 1), s = LumaAt(uv, 0, -1), e = LumaAt(uv, 1, 0), w = LumaAt(uv, -1, 0);
                float ne = LumaAt(uv, 1, 1), nw = LumaAt(uv, -1, 1), se = LumaAt(uv, 1, -1), sw = LumaAt(uv, -1, -1);

                float highest = max(max(max(max(n, e), max(s, w)), m), 0);
                float lowest = min(min(min(n, e), min(s, w)), m);
                float contrast = highest - lowest;
                // flat area: nothing to smooth
                if (contrast < max(_ContrastThreshold, _RelativeThreshold * highest)) return centre;

                // sub-pixel blend: how much this pixel differs from its neighbourhood average
                float filter = 2 * (n + e + s + w) + ne + nw + se + sw;
                filter = saturate(abs(filter / 12 - m) / contrast);
                float pixelBlend = smoothstep(0, 1, filter);
                pixelBlend = pixelBlend * pixelBlend * _SubpixelBlending;

                // edge direction
                float horizontal = 2 * abs(n + s - 2 * m) + abs(ne + se - 2 * e) + abs(nw + sw - 2 * w);
                float vertical = 2 * abs(e + w - 2 * m) + abs(ne + nw - 2 * n) + abs(se + sw - 2 * s);
                bool isHorizontal = horizontal >= vertical;

                float pixelStep = isHorizontal ? texel.y : texel.x;
                float pLuma = isHorizontal ? n : e;
                float nLuma = isHorizontal ? s : w;
                float pGradient = abs(pLuma - m);
                float nGradient = abs(nLuma - m);
                float oppositeLuma, gradient;
                if (pGradient < nGradient) { pixelStep = -pixelStep; oppositeLuma = nLuma; gradient = nGradient; }
                else { oppositeLuma = pLuma; gradient = pGradient; }

                // walk both ways along the edge to find where it ends
                float2 uvEdge = uv;
                float2 edgeStep;
                if (isHorizontal) { uvEdge.y += pixelStep * 0.5; edgeStep = float2(texel.x, 0); }
                else { uvEdge.x += pixelStep * 0.5; edgeStep = float2(0, texel.y); }
                float edgeLuma = (m + oppositeLuma) * 0.5;
                float gradientThreshold = gradient * 0.25;

                float2 puv = uvEdge + edgeStep * edgeSteps[0];
                float pDelta = Luma(puv) - edgeLuma;
                bool pAtEnd = abs(pDelta) >= gradientThreshold;
                [unroll]
                for (int k = 1; k < EDGE_STEP_COUNT && !pAtEnd; k++)
                {
                    puv += edgeStep * edgeSteps[k];
                    pDelta = Luma(puv) - edgeLuma;
                    pAtEnd = abs(pDelta) >= gradientThreshold;
                }
                if (!pAtEnd) puv += edgeStep * EDGE_GUESS;

                float2 nuv = uvEdge - edgeStep * edgeSteps[0];
                float nDelta = Luma(nuv) - edgeLuma;
                bool nAtEnd = abs(nDelta) >= gradientThreshold;
                [unroll]
                for (int j = 1; j < EDGE_STEP_COUNT && !nAtEnd; j++)
                {
                    nuv -= edgeStep * edgeSteps[j];
                    nDelta = Luma(nuv) - edgeLuma;
                    nAtEnd = abs(nDelta) >= gradientThreshold;
                }
                if (!nAtEnd) nuv -= edgeStep * EDGE_GUESS;

                float pDistance = isHorizontal ? puv.x - uv.x : puv.y - uv.y;
                float nDistance = isHorizontal ? uv.x - nuv.x : uv.y - nuv.y;
                float shortest;
                bool deltaSign;
                if (pDistance <= nDistance) { shortest = pDistance; deltaSign = pDelta >= 0; }
                else { shortest = nDistance; deltaSign = nDelta >= 0; }
                float edgeBlend = deltaSign == (m - edgeLuma >= 0) ? 0 : 0.5 - shortest / (pDistance + nDistance);

                float finalBlend = max(pixelBlend, edgeBlend);
                if (isHorizontal) uv.y += pixelStep * finalBlend;
                else uv.x += pixelStep * finalBlend;
                // colour from the blended position (needs bilinear sampling), alpha as drawn
                return float4(tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb, centre.a);
            }
            ENDCG
        }
    }
}
