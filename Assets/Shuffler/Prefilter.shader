Shader "Shuffler/Prefilter"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
    }

CGINCLUDE

#include "UnityCG.cginc"

sampler2D _MainTex;

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

// Common vertex function
void Vertex(float4 inPos : POSITION, float2 inUV : TEXCOORD0,
            out float4 outPos : SV_Position, out float2 outUV : TEXCOORD0)
{
    outPos = UnityObjectToClipPos(inPos);
    outUV = inUV;
}

// Prefilter fragment functions

float4 FragmentBypass(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return tex2D(_MainTex, uv);
}

float4 FragmentMonochrome(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float3 col = tex2D(_MainTex, uv).rgb;
    return float4((float3)Luminance(col), 1);
}

float4 FragmentInvert(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float3 col = tex2D(_MainTex, uv).rgb;
    col = SRGBToLinear(1 - LinearToSRGB(col));
    return float4(col, 1);
}

float4 FragmentContours(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 ddxy = float2(ddx(uv.x), ddy(uv.y));
    float4 delta = ddxy.xyxy * float4(1, 1, -1, 0);
    float l0 = Luminance(LinearToSRGB(tex2D(_MainTex, uv - delta.xy).rgb));
    float l1 = Luminance(LinearToSRGB(tex2D(_MainTex, uv - delta.wy).rgb));
    float l2 = Luminance(LinearToSRGB(tex2D(_MainTex, uv - delta.zy).rgb));
    float l3 = Luminance(LinearToSRGB(tex2D(_MainTex, uv - delta.xw).rgb));
    float l4 = Luminance(LinearToSRGB(tex2D(_MainTex, uv           ).rgb));
    float l5 = Luminance(LinearToSRGB(tex2D(_MainTex, uv + delta.xw).rgb));
    float l6 = Luminance(LinearToSRGB(tex2D(_MainTex, uv + delta.zy).rgb));
    float l7 = Luminance(LinearToSRGB(tex2D(_MainTex, uv + delta.wy).rgb));
    float l8 = Luminance(LinearToSRGB(tex2D(_MainTex, uv + delta.xy).rgb));
    float gx = l2 - l0 + (l5 - l3) * 2 + l8 - l6;
    float gy = l6 - l0 + (l7 - l1) * 2 + l8 - l2;
    float g = 1 - smoothstep(0, 0.1, sqrt(gx * gx + gy * gy));
    return float4(g, g, g, 1);
}

float4 FragmentVerticalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 uv1 = float2(uv.x + 0.25, uv.y);
    float2 uv2 = float2(uv.x - 0.25, 1 - uv.y);
    return tex2D(_MainTex, uv.x < 0.5 ? uv1 : uv2);
}

float4 FragmentHorizontalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    float2 uv1 = float2(    uv.x, uv.y + 0.25);
    float2 uv2 = float2(1 - uv.x, uv.y - 0.25);
    return tex2D(_MainTex, uv.y < 0.5 ? uv1 : uv2);
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
