using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingObject : MonoBehaviour
{
    public RayTracingMaterial material;
    
    private void OnEnable()
    {
        RayTracingManager.RegisterObject(this);
    }
    private void OnDisable()
    {
        RayTracingManager.UnregisterObject(this);
    }
}
