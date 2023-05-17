using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public sealed class Prefilter
{
    #region Public modifiable properties

    public Texture2D Layer1Texture { set => SetTexture1(value); }
    public Texture2D Layer2Texture { set => SetTexture2(value); }
    public Color Layer1Color { set => SetColor1(value); }
    public Color Layer2Color { set => SetColor2(value); }

    #endregion

    #region Public read only properties

    public RenderTexture Output { get; private set; }

    #endregion

    #region Public methods

    public Prefilter(int width, int height, Shader shader, Texture3D lut)
    {
        _material = new Material(shader);
        _material.SetTexture("_LutTexture", lut);
        Output = new RenderTexture(width, height, 0);
    }

    public void Destroy()
    {
        Object.Destroy(_material);
        Object.Destroy(Output);
        (_material, Output) = (null, null);
    }

    public void Apply(Texture source, int pass)
    {
        var mtx = CalculatePrefilterMatrices();
        _material.SetMatrix("_Layer1Matrix", mtx.Item1);
        _material.SetMatrix("_Layer2Matrix", mtx.Item2);
        _material.SetVector("_Random", _random.NextFloat4());
        Graphics.Blit(source, Output, _material, pass);
    }

    #endregion

    #region Private members

    Material _material;
    float _aspectLayer1, _aspectLayer2;
    Random _random = new Random(123);

    void SetTexture1(Texture2D texture)
    {
        _material.SetTexture("_Layer1Texture", texture);
        _aspectLayer1 = (float)texture.width / texture.height;
    }

    void SetTexture2(Texture2D texture)
    {
        _material.SetTexture("_Layer2Texture", texture);
        _aspectLayer2 = (float)texture.width / texture.height;
    }

    void SetColor1(Color color)
      => _material.SetColor("_Layer1Color", color);

    void SetColor2(Color color)
      => _material.SetColor("_Layer2Color", color);

    (float4x4, float4x4) CalculatePrefilterMatrices()
    {
        // Random TRS for the overlay layer
        var rndT = _random.NextFloat3(-1, 1) * math.float3(0.5f, 0.2f, 0);
        var rndR = _random.NextFloat(-0.3f, 0.3f) * math.PI;
        var rndS = _random.NextFloat(0.5f, 2);
        var mtxRnd = float4x4.TRS(rndT, quaternion.RotateZ(rndR), rndS);

        // UV (0..1) -> (-0.5..+0.5)
        var mtxCenter = float4x4.Translate(math.float3(-0.5f, -0.5f, 0));

        // UV aspect ratio fix
        var baseRatio = (float)Output.height / Output.width;
        var mtxUVSS = float4x4.Scale(1, baseRatio, 1);

        // Inverse aspect ratio fix for the each layer
        var mtxUV1 = float4x4.Scale(1, _aspectLayer1, 1);
        var mtxUV2 = float4x4.Scale(1, _aspectLayer2, 1);

        // (-0.5..+0.5) -> (0..1)
        var mtxOffs = float4x4.Translate(math.float3(0.5f, 0.5f, 0));

        // UV transform matrices for the each layer
        var mtxBase = math.mul(mtxUVSS, mtxCenter);
        return (math.mul(math.mul(mtxOffs, mtxUV1), mtxBase),
                math.mul(math.mul(mtxOffs, mtxUV2), math.mul(mtxRnd, mtxBase)));
    }

    #endregion
}
