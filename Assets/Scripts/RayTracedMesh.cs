using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracedMesh : MonoBehaviour
{
    public RTMaterial[] materials;
    
    [Header("Info")]
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    [SerializeField] private bool vertexData;

    private void OnDrawGizmos()
    {
        if (vertexData)
        {
            float length = 0.1f;
            Triangle[] triangles = Triangle.TrianglesFromMesh(meshFilter.sharedMesh, transform);

            foreach (Triangle triangle in triangles)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(triangle.p1, triangle.p2);
                Gizmos.DrawLine(triangle.p1, triangle.p3);
                Gizmos.DrawLine(triangle.p2, triangle.p3);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(triangle.p1, triangle.n1 * length);
                Gizmos.DrawRay(triangle.p2, triangle.n2 * length);
                Gizmos.DrawRay(triangle.p3, triangle.n3 * length);
            }
        }
    }
}
