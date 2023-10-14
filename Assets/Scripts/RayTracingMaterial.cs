using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RayTracingMaterial
{
    public Color diffuseColor = Color.white;
    public Color emissionColor;
    public float emissionStrength;
}
