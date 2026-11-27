// 댄스 플로어 빛기둥(FloorLightPillar). 가산 합성, 위로 갈수록 사라지는 빛 + 흐르는 세로 줄무늬.
// _Rise: 0 → 1로 올리면 바닥에서 위로 솟구친다(1보다 크면 더 높이).
Shader "Solve/LightPillar"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.2, 0.8, 1)
        _Intensity ("Intensity", Float) = 1.6
        _Rise ("Rise", Range(0, 1.6)) = 1
        _FadePower ("Fade Power", Range(0.2, 6)) = 1.6
        _BaseGlow ("Base Glow", Range(0, 6)) = 2
        _EdgeSoftness ("Top Edge Softness", Range(1, 60)) = 18
        _StreakCount ("Streak Count", Float) = 12
        _StreakSpeed ("Streak Speed", Float) = 0.6
        _StreakAmount ("Streak Amount", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Intensity;
            float _Rise;
            float _FadePower;
            float _BaseGlow;
            float _EdgeSoftness;
            float _StreakCount;
            float _StreakSpeed;
            float _StreakAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float h = i.uv.y;                       // 0 = 바닥, 1 = 기둥 꼭대기
                float rise = max(_Rise, 0.0001);
                float visible = saturate((rise - h) * _EdgeSoftness);
                float t = saturate(h / rise);
                float fade = pow(1.0 - t, _FadePower);
                float baseBand = exp(-h * 14.0) * _BaseGlow;   // 바닥 쪽 강한 띠
                float wave = 0.5 + 0.5 * sin((i.uv.x * _StreakCount + _Time.y * _StreakSpeed) * 6.2831853 + h * 3.0);
                float streak = 1.0 - _StreakAmount + _StreakAmount * wave;
                float a = (fade * streak + baseBand) * visible;
                return float4(_Color.rgb * _Intensity * a, 1.0);
            }
            ENDCG
        }
    }
}
