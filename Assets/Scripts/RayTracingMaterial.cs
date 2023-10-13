using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaterial : MonoBehaviour
{
    public Mesh mesh;
    public Color diffuseColor;
    public Color emissionColor;
    public float emissionStrength;

    private void OnDrawGizmos()
    {
        float length = 0.1f;
        Triangle[] triangles = Triangle.TrianglesFromMesh(mesh, transform);

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
