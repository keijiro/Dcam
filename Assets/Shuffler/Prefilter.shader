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

sampler2D _MainTex;
sampler2D _OverlayTexture;
float _OverlayOpacity;

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

float4 FragmentMonochrome(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return ComposeFinal(Luminance(SampleSource(uv)), uv);
}

float4 FragmentInvert(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return ComposeFinal(1 - SampleSource(uv), uv);
}

float4 FragmentContours(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 ddxy = float2(ddx(uv.x), ddy(uv.y));
    float4 delta = ddxy.xyxy * float4(1, 1, -1, 0);
    float l0 = Luminance(SampleSource(uv - delta.xy));
    float l1 = Luminance(SampleSource(uv - delta.wy));
    float l2 = Luminance(SampleSource(uv - delta.zy));
    float l3 = Luminance(SampleSource(uv - delta.xw));
    float l4 = Luminance(SampleSource(uv           ));
    float l5 = Luminance(SampleSource(uv + delta.xw));
    float l6 = Luminance(SampleSource(uv + delta.zy));
    float l7 = Luminance(SampleSource(uv + delta.wy));
    float l8 = Luminance(SampleSource(uv + delta.xy));
    float gx = l2 - l0 + (l5 - l3) * 2 + l8 - l6;
    float gy = l6 - l0 + (l7 - l1) * 2 + l8 - l2;
    float g = 1 - smoothstep(0, 0.1, sqrt(gx * gx + gy * gy));
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
            #pragma fragment FragmentMonochrome
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentInvert
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
    }
}
