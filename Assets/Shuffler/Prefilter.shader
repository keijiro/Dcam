Shader "Shuffler/Prefilter"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}
        _OverlayTexture("", 2D) = "black" {}
        _OverlayOpacity("", Float) = 1
    }

CGINCLUDE

#include "UnityCG.cginc"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

// -- Uniforms

sampler2D _MainTex;
sampler2D _OverlayTexture;
float _OverlayOpacity;
float4 _Random;

// -- Common functions

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

// Rec.709 luma
float Luma(float3 c)
{
    return dot(c, float3(0.212, 0.701, 0.087));
}

// Input: Source texture sampling
float3 SampleSource(float2 uv)
{
    return LinearToSRGB(tex2D(_MainTex, uv).rgb);
}

// Output: Final composition with overlays
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

// -- Prefilter fragment functions

// Pass 0: Bypass
float4 FragmentBypass(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    return ComposeFinal(SampleSource(uv), uv);
}

// Pass 1: Posterizing
float4 FragmentPosterize(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float levels = floor(lerp(2, 6, _Random.x));
    const float color = floor(Luma(SampleSource(uv)) * levels + 0.5) / levels;
    return ComposeFinal(color, uv);
}

// Pass 2: Contours (Sobel filter)
float4 FragmentContours(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float2 ddxy = float2(ddx(uv.x), ddy(uv.y));
    const float4 delta = ddxy.xyxy * float4(1, 1, -1, 0);
    const float l0 = Luma(SampleSource(uv - delta.xy));
    const float l1 = Luma(SampleSource(uv - delta.wy));
    const float l2 = Luma(SampleSource(uv - delta.zy));
    const float l3 = Luma(SampleSource(uv - delta.xw));
    const float l4 = Luma(SampleSource(uv           ));
    const float l5 = Luma(SampleSource(uv + delta.xw));
    const float l6 = Luma(SampleSource(uv + delta.zy));
    const float l7 = Luma(SampleSource(uv + delta.wy));
    const float l8 = Luma(SampleSource(uv + delta.xy));
    const float gx = l2 - l0 + (l5 - l3) * 2 + l8 - l6;
    const float gy = l6 - l0 + (l7 - l1) * 2 + l8 - l2;
    const float g = 1 - smoothstep(0, 0.2, sqrt(gx * gx + gy * gy));
    return ComposeFinal(g, uv);
}

// Pass 3: Vertical split and mirroring
float4 FragmentVerticalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float2 uv1 = float2(uv.x + 0.25, uv.y);
    const float2 uv2 = float2(uv.x - 0.25, 1 - uv.y);
    return ComposeFinal(SampleSource(uv.x < 0.5 ? uv1 : uv2), uv);
}

// Pass 4: Horizontal split and mirroring
float4 FragmentHorizontalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float2 uv1 = float2(    uv.x, uv.y + 0.25);
    const float2 uv2 = float2(1 - uv.x, uv.y - 0.25);
    return ComposeFinal(SampleSource(uv.y < 0.5 ? uv1 : uv2), uv);
}

// Pass 5: Random slicing
float4 FragmentSlice(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    // Random slice angle
    const float phi = _Random.x * UNITY_PI * 2;
    // Base axis (1) and displacement axis (2)
    const float2 axis1 = float2(cos(phi), sin(phi));
    const float2 axis2 = axis1.yx * float2(-1, 1);
    // Random slice width
    const float width = lerp(5, 20, _Random.y);
    // Direction (+/-)
    const float dir = frac(dot(uv, axis1) * width) < 0.5 ? -1 : 1;
    // Displacement vector
    const float2 disp = axis2 * dir * _Random.z * 0.05;
    // Composition
    return ComposeFinal(SampleSource(uv + disp), uv);
}

// Pass 6: Random flow
float4 FragmentFlow(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    // Constant (sample count)
    const uint sample_count = 8;
    // Noise frequency
    const float freq = lerp(2, 10, _Random.x);
    // Step length
    const float slen = lerp(0.0001, 0.003, _Random.y);
    // Sampling iteration
    float2 p = uv;
    float3 acc = SampleSource(p);
    for (uint i = 1; i < sample_count; i++)
    {
        // Noise field sampling point
        float2 np = p * freq + float2(0, _Time.y);
        // 2D divergence-free noise field
        float2 dfn = cross(SimplexNoiseGrad(np), float3(0, 0, 1)).xy;
        // Step
        p += dfn * slen;
        // Sampling and accumulation
        acc += SampleSource(p);
    }
    // Composition
    return ComposeFinal(acc / sample_count, uv);
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
