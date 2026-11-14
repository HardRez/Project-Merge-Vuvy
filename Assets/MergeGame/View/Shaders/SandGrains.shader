// Draws the grains of a sand pour for uGUI, one quad per grain (see SandGrainGraphic). The vertex colour is the grain's
// colour; uv0 runs from -1 to 1 across the quad; uv1.x is the grain's roundness. A resting grain (roundness 0) fills its
// square, so the grains tile into a solid surface identical to the sand tile; as it falls it turns into a round, softly
// lit grain, and a quad stretched along the fall becomes a streak. Keeps the standard UI stencil and clip-rect support,
// so it works under Masks.
Shader "MergeGame/UI/Sand Grains"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Shading ("Highlight And Shadow", Range(0, 0.5)) = 0.18
        _Sparkle ("Sparkle", Range(0, 1)) = 0.45

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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 local : TEXCOORD0;
                float roundness : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            float _Shading;
            float _Sparkle;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.local = v.texcoord;
                OUT.roundness = v.texcoord1.x;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 d = IN.local;
                float r = length(d);
                float roundness = IN.roundness;

                // A square while resting, a soft-edged disc (an ellipse on a stretched quad) once it falls.
                float coverage = lerp(1, smoothstep(1.0, 0.72, r), roundness);

                // Lit from the top left, darker towards the bottom right and the rim; only round grains are shaded.
                float shading = _Shading * roundness;
                float light = 1 + shading * (d.y - d.x) * 0.5 - shading * r * r * 0.5;

                // A few grains catch the light; which ones depends on the grain's own colour, so it travels with it.
                float pick = frac(sin(dot(IN.color.rgb, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                float glint = step(0.9, pick) * _Sparkle * roundness * smoothstep(0.6, 0.0, length(d - float2(-0.35, 0.35)));

                fixed4 result = fixed4(IN.color.rgb * light + glint, IN.color.a * coverage);

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
