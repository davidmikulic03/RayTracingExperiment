using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteAlways]
public class RayTracingObject : MonoBehaviour
{
    [HideInInspector] public int index; 
    public RayTracingMaterial material;
    
    private void OnEnable()
    {
        RayTracingManager.RegisterObject(this);
    }
    private void OnDisable()
    {
        RayTracingManager.UnregisterObject(this);
    }

    public void GetBounds(ref Vector3 min, ref Vector3 max)
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        
        if (transform.rotation == Quaternion.identity)
        {
            min = Vector3.Scale(mesh.bounds.min, transform.lossyScale)  + transform.position;
            max = Vector3.Scale(mesh.bounds.max, transform.lossyScale) + transform.position;
        }
        else
        {
            
        }
    }

    private void OnValidate()
    {
        RayTracingManager.UpdateObject(this);
    }
}
