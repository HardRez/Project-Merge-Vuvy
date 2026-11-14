// Draws a sand simulation for uGUI: each texel of the state texture is one grain (rgb = its colour, a = 0 when empty,
// otherwise 0.5 + 0.5 * its free-fall speed). Every texel becomes a small round grain with a soft highlight and shadow;
// grains overlap their neighbours a little so packed sand reads as a solid surface, and fast grains stretch upward
// along their fall into silky streams. Keeps the standard UI stencil and clip-rect support, so it works under Masks.
Shader "MergeGame/UI/Sand Grains"
{
    Properties
    {
        [PerRendererData] _MainTex ("Grain State", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GrainRadius ("Grain Radius (in grains)", Range(0.4, 1.0)) = 0.85
        _Shading ("Highlight And Shadow", Range(0, 0.5)) = 0.1
        _Sparkle ("Sparkle", Range(0, 1)) = 0.35
        _Stretch ("Stretch Of Fast Grains (in grains)", Range(0, 1.5)) = 1.2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float4 _ClipRect;
            float _GrainRadius;
            float _Shading;
            float _Sparkle;
            float _Stretch;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // The grain stored in texel `cell`, seen from `p` (both in grain units): colour and coverage.
            float4 Grain(float2 cell, float2 p)
            {
                // Outside the grid there is no sand (the clamped sampler would otherwise repeat the edge).
                if (any(cell < 0) || any(cell >= _MainTex_TexelSize.zw))
                    return 0;

                float4 state = tex2D(_MainTex, (cell + 0.5) * _MainTex_TexelSize.xy);
                if (state.a < 0.25)
                    return 0;

                // A fast grain becomes an ellipse trailing upward, the way a falling grain smears in the eye.
                float stretch = saturate(state.a * 2 - 1) * _Stretch;
                float2 centre = cell + float2(0.5, 0.5 + stretch * 0.5);
                float2 d = (p - centre) / float2(_GrainRadius, _GrainRadius + stretch * 0.5);
                float r = length(d);
                float coverage = smoothstep(1.0, 0.7, r);

                // Lit from the top left: brighter there, darker towards the bottom right and the rim.
                float light = 1 + _Shading * (d.y - d.x) * 0.7 - _Shading * r * r * 0.6;

                // A few grains catch the light; which ones depends on the grain's own colour, so it travels with it.
                float pick = frac(sin(dot(state.rgb, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                float glint = step(0.92, pick) * _Sparkle * smoothstep(0.55, 0.0, length(d - float2(-0.35, 0.35)));

                return float4(state.rgb * light + glint, coverage);
            }

            // Paints `grain` over `under`.
            float4 Over(float4 under, float4 grain)
            {
                return float4(lerp(under.rgb, grain.rgb, grain.a), 1 - (1 - under.a) * (1 - grain.a));
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.texcoord * _MainTex_TexelSize.zw;
                float2 cell = floor(p);

                // Neighbouring grains overlap into this texel; the one below may also trail up into it.
                float4 colour = 0;
                colour = Over(colour, Grain(cell + float2(0, 1), p));
                colour = Over(colour, Grain(cell + float2(-1, 0), p));
                colour = Over(colour, Grain(cell + float2(1, 0), p));
                colour = Over(colour, Grain(cell + float2(0, -1), p));
                colour = Over(colour, Grain(cell, p));

                fixed4 result = fixed4(colour.rgb, colour.a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
