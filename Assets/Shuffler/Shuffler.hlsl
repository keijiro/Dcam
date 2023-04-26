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

float PseudoOcclusion(float2 uv)
{
    return 4 * length(max(0, abs(uv - 0.5) - 1.0 / 3 / 2 ));
}

void ShufflerFragment_float
  (UnityTexture2D tex1, UnityTexture2D tex2,
   float2 uv, float prog, float occlusion,
   out float3 output)
{
    const uint SampleCount = 16;

    float blur = 0.01f / 4;// * (1 - prog);

    float3 acc = 0;
    for (uint i = 0; i < SampleCount; i++)
        acc += SampleShuffler(tex1, tex2, uv, prog + blur * i);

    float occ = lerp(1, PseudoOcclusion(uv), occlusion);

    output = occ * acc / SampleCount;
}
