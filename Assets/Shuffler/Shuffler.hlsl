// Pseudo ambient occlusion
float ShufflerOcclusion(float2 uv, float size, float ext)
{
    float dist = length(max(0, abs(uv - 0.5) - size / 2));
    return saturate(1 - dist / ext);
}

// Source color sampling function
float3 SampleShuffler
  (UnityTexture2D tex1, UnityTexture2D tex2, float2 uv, float t)
{
    float y1 = uv.y - pow(saturate(1 - t), 2.2);
    float y2 = uv.y;
    float3 c1 = tex2D(tex1, float2(uv.x, y1)).rgb;
    float3 c2 = tex2D(tex2, float2(uv.x, y2)).rgb;
    return y1 > 0 ? c1 : c2;
}

// Custom node function
void ShufflerFragment_float
  (UnityTexture2D tex1, UnityTexture2D tex2,
   float2 uv, float time, float blur,
   float occ_size, float occ_ext, float occ_str,
   out float3 output, out float alpha)
{
    const uint SampleCount = 16;

    // Pseudo ambient occlusion
    float occ = ShufflerOcclusion(uv, occ_size, occ_ext);

    // Source color sampling with motion blur
    float3 acc = 0;
    float t0 = time - blur / 2;
    float dt = blur / SampleCount;
    for (uint i = 0; i < SampleCount; i++)
        acc += SampleShuffler(tex1, tex2, uv, t0 + dt * i);

    // Composition
    output = (1 - occ * occ_str) * acc / SampleCount;
    alpha = 1 - length(max(abs(uv - 0.5) - 0.45, 0)) / 0.05;
}
