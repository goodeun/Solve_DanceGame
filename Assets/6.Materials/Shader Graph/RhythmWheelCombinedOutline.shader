Shader "UI/RhythmWheelCombinedOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTexture ("Mask Texture", 2D) = "black" {}
        _OutlineWidthPixels ("Outline Width Pixels", Range(1, 12)) = 2
        _CenterRadius ("Center Radius", Range(0, 0.5)) = 0.3
        _Color1 ("Bottom Color", Color) = (0, 1, 1, 1)
        _Color2 ("Top Color", Color) = (1, 0, 1, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MaskTexture;
            float4 _MaskTexture_TexelSize;
            float _OutlineWidthPixels;
            float _CenterRadius;
            fixed4 _Color1;
            fixed4 _Color2;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            float MaskAlpha(float2 uv)
            {
                // UI를 별도 카메라로 RenderTexture에 그리면 플랫폼/포맷에 따라
                // alpha 채널이 0으로 정리될 수 있다. 흰 마스크의 RGB까지 포함해
                // 판정하면 마스크가 투명해지는 문제를 피한다.
                fixed4 mask = tex2D(_MaskTexture, saturate(uv));
                return max(mask.a, max(mask.r, max(mask.g, mask.b)));
            }

            float CircleMask(float2 uv)
            {
                return step(length(uv - 0.5), _CenterRadius);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = _MaskTexture_TexelSize.xy * _OutlineWidthPixels;
                float2 uv = i.texcoord;
                float2 x = float2(offset.x, 0);
                float2 y = float2(0, offset.y);

                float keyCenter = MaskAlpha(uv);
                float keyRight = MaskAlpha(uv + x);
                float keyLeft = MaskAlpha(uv - x);
                float keyUp = MaskAlpha(uv + y);
                float keyDown = MaskAlpha(uv - y);
                float keyRightUp = MaskAlpha(uv + offset);
                float keyLeftUp = MaskAlpha(uv - x + y);
                float keyRightDown = MaskAlpha(uv + x - y);
                float keyLeftDown = MaskAlpha(uv - offset);

                float circleCenter = CircleMask(uv);
                float circleRight = CircleMask(uv + x);
                float circleLeft = CircleMask(uv - x);
                float circleUp = CircleMask(uv + y);
                float circleDown = CircleMask(uv - y);
                float circleRightUp = CircleMask(uv + offset);
                float circleLeftUp = CircleMask(uv - x + y);
                float circleRightDown = CircleMask(uv + x - y);
                float circleLeftDown = CircleMask(uv - offset);

                float keyDilated = max(max(max(keyCenter, keyRight), max(keyLeft, keyUp)), max(max(keyDown, keyRightUp), max(keyLeftUp, max(keyRightDown, keyLeftDown))));
                float keyEroded = min(min(min(keyCenter, keyRight), min(keyLeft, keyUp)), min(min(keyDown, keyRightUp), min(keyLeftUp, min(keyRightDown, keyLeftDown))));
                float circleDilated = max(max(max(circleCenter, circleRight), max(circleLeft, circleUp)), max(max(circleDown, circleRightUp), max(circleLeftUp, max(circleRightDown, circleLeftDown))));
                float circleEroded = min(min(min(circleCenter, circleRight), min(circleLeft, circleUp)), min(min(circleDown, circleRightUp), min(circleLeftUp, min(circleRightDown, circleLeftDown))));
                float outline = max(saturate(keyDilated - keyEroded), saturate(circleDilated - circleEroded));

                fixed4 color = lerp(_Color1, _Color2, i.texcoord.y);
                color.a *= outline * i.color.a;
                return color;
            }
            ENDCG
        }
    }
}
