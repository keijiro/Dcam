using UnityEngine;
using Klak.Math;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] float3 _positionRange = 0.1f;
    [SerializeField] float3 _rotationRange = 30;
    [SerializeField] float _distanceRange = 0.4f;
    [SerializeField] float _speed = 8;

    const uint Seed = 123;

    Random _random;
    (float3 p, quaternion r, float d) _target;

    public void RenewTarget()
    {
        var p = _random.NextFloat3(-_positionRange, _positionRange);
        var r = _random.NextFloat3(-_rotationRange, _rotationRange);
        var d = _random.NextFloat(-_distanceRange, _distanceRange);

        _target.p = p;
        _target.r = quaternion.EulerXZY(math.radians(r));
        _target.d = d;
    }

    void Start()
    {
        _random = new Random(Seed);
        RenewTarget();
    }

    void Update()
    {
        var child = transform.GetChild(0);
        transform.localPosition = ExpTween.Step(transform.localPosition, _target.p, _speed);
        transform.localRotation = ExpTween.Step(transform.localRotation, _target.r, _speed);
        child.localPosition = ExpTween.Step(child.localPosition, Vector3.forward * _target.d, _speed);
    }
}
