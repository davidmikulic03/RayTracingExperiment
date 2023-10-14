using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct RayTracingMaterial
{
    public Color diffuseColor;
    public Color emissiveColor;
    public float emission;
}
