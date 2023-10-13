using UnityEngine;

struct Triangle
{
    public Vector3 p1, p2, p3;
    public Vector3 n1, n2, n3;

    public static Triangle[] TrianglesFromMesh (Mesh mesh, Transform transform)
    {
        int[] triIndices = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        
        Triangle[] triangles = new Triangle[triIndices.Length / 3];

        for (int i = 3; i < triIndices.Length; i += 3)
        {
            int triIndexRemap = i / 3 - 1;

            Quaternion rotation = transform.rotation;
            Vector3 position = transform.position;
            Vector3 scale = transform.lossyScale;
            
            triangles[triIndexRemap].p1 = rotation * Vector3.Scale(vertices[triIndices[i - 3]], scale) + position;
            triangles[triIndexRemap].p2 = rotation * Vector3.Scale(vertices[triIndices[i - 2]], scale) + position;
            triangles[triIndexRemap].p3 = rotation * Vector3.Scale(vertices[triIndices[i - 1]], scale) + position;
            
            triangles[triIndexRemap].n1 = rotation * normals[triIndices[i - 3]];
            triangles[triIndexRemap].n2 = rotation * normals[triIndices[i - 2]];
            triangles[triIndexRemap].n3 = rotation * normals[triIndices[i - 1]];
        }

        return triangles;
    }

    public override string ToString()
    {
        string output = "Points: " +
                        p1.ToString() + "\r\n" +
                        p2.ToString() + "\r\n" +
                        p3.ToString() + "\r\n";
            
        return output;
    }
}
