Shader "Shuffler/Prefilter"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}
        _LutTexture("", 3D) = "" {}
        _Layer1Texture("", 2D) = "black" {}
        _Layer2Texture("", 2D) = "black" {}
        _Layer1Color("", Color) = (1, 1, 1, 1)
        _Layer2Color("", Color) = (1, 1, 1, 1)
    }

CGINCLUDE

#include "UnityCG.cginc"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

// -- Uniforms

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler3D _LutTexture;
sampler2D _Layer1Texture;
sampler2D _Layer2Texture;
float4x4 _Layer1Matrix;
float4x4 _Layer2Matrix;
float4 _Layer1Color;
float4 _Layer2Color;
float4 _Random;

// -- Common functions

// Color space conversion between sRGB and linear space.
// http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html
float3 LinearToSRGB(float3 c)
{
    return max(1.055 * pow(saturate(c), 0.416666667) - 0.055, 0.0);
}

float4 LinearToSRGB(float4 c) { return float4(LinearToSRGB(c.rgb), c.a); }

float3 SRGBToLinear(float3 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

float4 SRGBToLinear(float4 c) { return float4(SRGBToLinear(c.rgb), c.a); }

// Rec.709 luma
float Luma(float3 c)
{
    return dot(c, float3(0.212, 0.701, 0.087));
}

// Input: Source texture sampling
float3 SampleSource(float2 uv)
{
    return tex3D(_LutTexture, LinearToSRGB(tex2D(_MainTex, uv).rgb));
}

// Output: Final composition with title/overlay
float4 ComposeFinal(float3 color, float2 uv)
{
    const float2 uv1 = mul(_Layer1Matrix, float4(uv, 0, 1)).xy;
    const float2 uv2 = mul(_Layer2Matrix, float4(uv, 0, 1)).xy;
    const float4 c1 = tex2D(_Layer1Texture, uv1) * _Layer1Color;
    const float4 c2 = tex2D(_Layer2Texture, uv2) * _Layer2Color;
    color = lerp(color, LinearToSRGB(c1.rgb), c1.a);
    color = lerp(color, LinearToSRGB(c2.rgb), c2.a);
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

// Pass 2: Vertical split and mirroring
float4 FragmentVerticalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float2 uv1 = float2(uv.x + 0.25, uv.y);
    const float2 uv2 = float2(uv.x - 0.25, 1 - uv.y);
    return ComposeFinal(SampleSource(uv.x < 0.5 ? uv1 : uv2), uv);
}

// Pass 3: Horizontal split and mirroring
float4 FragmentHorizontalSplit(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    const float2 uv1 = float2(    uv.x, uv.y + 0.25);
    const float2 uv2 = float2(1 - uv.x, uv.y - 0.25);
    return ComposeFinal(SampleSource(uv.y < 0.5 ? uv1 : uv2), uv);
}

// Pass 4: Random slicing
float4 FragmentSlice(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    // Aspect ratio compensation
    const float2 aspfix = float2(_MainTex_TexelSize.x * _MainTex_TexelSize.w, 1);
    // Random slice angle
    const float phi = _Random.x * UNITY_PI * 2;
    // Base axis (1) and displacement axis (2)
    const float2 axis1 = float2(cos(phi), sin(phi));
    const float2 axis2 = axis1.yx * float2(-1, 1);
    // Random slice width
    const float width = 10;//lerp(5, 20, _Random.y);
    // Direction (+/-)
    const float dir = frac(dot(uv * aspfix.yx, axis1) * width) < 0.5 ? -1 : 1;
    // Displacement vector
    const float2 disp = axis2 * dir * aspfix * _Random.z * 0.05;
    // Composition
    return ComposeFinal(SampleSource(uv + disp), uv);
}

// Pass 5: Random flow
float4 FragmentFlow(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    // Aspect ratio compensation
    const float2 aspfix = float2(_MainTex_TexelSize.x * _MainTex_TexelSize.w, 1);
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
        float2 np = p * aspfix.yx * freq + float2(0, _Time.y);
        // 2D divergence-free noise field
        float2 dfn = cross(SimplexNoiseGrad(np), float3(0, 0, 1)).xy;
        // Step
        p += dfn * aspfix * slen;
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
