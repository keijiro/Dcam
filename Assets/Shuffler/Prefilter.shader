Shader "Shuffler/Prefilter"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
        _OverlayTexture("", 2D) = "black" {}
        _OverlayOpacity("", Float) = 1
    }

CGINCLUDE

#include "UnityCG.cginc"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

sampler2D _MainTex;
sampler2D _OverlayTexture;
float _OverlayOpacity;
float4 _Random;

// Color space conversion between sRGB and linear space.
// http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html
float3 LinearToSRGB(float3 c)
{
    return max(1.055 * pow(saturate(c), 0.416666667) - 0.055, 0.0);
}

float3 SRGBToLinear(float3 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

float Luma(float3 c)
{
    return dot(c, float3(0.212, 0.701, 0.087));
}

// Common functions
float3 SampleSource(float2 uv)
{
    return LinearToSRGB(tex2D(_MainTex, uv).rgb);
}

float4 ComposeFinal(float3 color, float2 uv)
{
    float4 ovr = tex2D(_OverlayTexture, uv);
    ovr.rgb = LinearToSRGB(ovr);
    color = lerp(color, ovr.rgb, ovr.a * _OverlayOpacity);
    return float4(SRGBToLinear(color), 1);
}

// Vertex function
void Vertex(float4 inPos : POSITION, float2 inUV : TEXCOORD0,
            out float4 outPos : SV_Position, out float2 outUV : TEXCOORD0)
{
    outPos = UnityObjectToClipPos(inPos);
    outUV = inUV;
}

// Prefilter fragment functions

float4 FragmentBypass(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return ComposeFinal(SampleSource(uv), uv);
}

float4 FragmentPosterize(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float l = Luma(SampleSource(uv));
    float res = floor(lerp(2, 6, _Random.x));
    l = floor(l * res + 0.5) / res;
    return ComposeFinal(l, uv);
}

float4 FragmentContours(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 ddxy = float2(ddx(uv.x), ddy(uv.y));
    float4 delta = ddxy.xyxy * float4(1, 1, -1, 0);
    float l0 = Luma(SampleSource(uv - delta.xy));
    float l1 = Luma(SampleSource(uv - delta.wy));
    float l2 = Luma(SampleSource(uv - delta.zy));
    float l3 = Luma(SampleSource(uv - delta.xw));
    float l4 = Luma(SampleSource(uv           ));
    float l5 = Luma(SampleSource(uv + delta.xw));
    float l6 = Luma(SampleSource(uv + delta.zy));
    float l7 = Luma(SampleSource(uv + delta.wy));
    float l8 = Luma(SampleSource(uv + delta.xy));
    float gx = l2 - l0 + (l5 - l3) * 2 + l8 - l6;
    float gy = l6 - l0 + (l7 - l1) * 2 + l8 - l2;
    float g = 1 - smoothstep(0, 0.2, sqrt(gx * gx + gy * gy));
    return ComposeFinal(g, uv);
}

float4 FragmentVerticalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 uv1 = float2(uv.x + 0.25, uv.y);
    float2 uv2 = float2(uv.x - 0.25, 1 - uv.y);
    return ComposeFinal(SampleSource(uv.x < 0.5 ? uv1 : uv2), uv);
}

float4 FragmentHorizontalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 uv1 = float2(    uv.x, uv.y + 0.25);
    float2 uv2 = float2(1 - uv.x, uv.y - 0.25);
    return ComposeFinal(SampleSource(uv.y < 0.5 ? uv1 : uv2), uv);
}

float4 FragmentSlice(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float phi = _Random.x * UNITY_PI * 2;
    float width = lerp(5, 20, _Random.y);
    float disp = _Random.z * 0.1;
    float2 dir = float2(cos(phi), sin(phi));
    disp *= frac(dot(uv, dir) * width) < 0.5 ? -0.5 : 0.5;
    float2 uv_d = uv + dir.yx * float2(-1, 1) * disp;
    return ComposeFinal(SampleSource(uv_d), uv);
}

float4 FragmentFlow(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float freq = lerp(2, 10, _Random.x);
    float disp = lerp(0.0001, 0.003, _Random.y);
    float2 p = uv;
    float3 acc = 0;
    for (uint i = 0; i < 8; i++)
    {
        float2 np = p * freq + float2(0, _Time.y);
        float2 flow = cross(SimplexNoiseGrad(np), float3(0, 0, 1)).xy;
        p += flow * disp;
        acc += SampleSource(p);
    }
    return ComposeFinal(acc / 8, uv);
}

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentBypass
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentPosterize
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentContours
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentVerticalSplit
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentHorizontalSplit
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentSlice
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentFlow
            ENDCG
        }
    }
}
