#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

float3 SampleShuffler
  (UnityTexture2D tex1, UnityTexture2D tex2, float2 uv, float prog)
{
    float2 uv1 = uv;
    float2 uv2 = uv;

    uv1.y = uv1.y - 1 + smoothstep(-1, 1, prog) * 2 - 1;

    float3 c1 = tex2D(tex1, uv1).rgb;
    float3 c2 = tex2D(tex2, uv2).rgb;

    return uv1.y > 0 ? c1 : c2;
}

void ShufflerFragment_float
  (UnityTexture2D tex1, UnityTexture2D tex2, float2 uv, float prog, out float3 output)
{
    const uint SampleCount = 16;

    float blur = 0.01f / 4;// * (1 - prog);

    float3 acc = 0;
    for (uint i = 0; i < SampleCount; i++)
        acc += SampleShuffler(tex1, tex2, uv, prog + blur * i);

    output = acc / SampleCount;
}

void ShufflerMajorPage_float
  (UnityTexture2D tex, float2 uv, float seed,
   out float3 outColor, out float outAlpha)
{
    float2 np = (uv + seed) * 10;
    float n = 0.5;
    n += SimplexNoise(np * 1) / 4;
    n += SimplexNoise(np * 2) / 8;
    n += SimplexNoise(np * 4) / 16;

    outColor = tex2D(tex, uv).rgb;
    outAlpha = n;
}
