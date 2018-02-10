// Code originally posted on unity forums by Bunny83
// https://answers.unity.com/questions/143025/more-questions-on-generating-camera-frustrum.html

using UnityEngine;

public static class CameraExtension
{
    private static int[] m_VertOrder = new int[24]
    {
         0,1,2,3, // near
         6,5,4,7, // far
         0,4,5,1, // left
         3,2,6,7, // right
         1,5,6,2, // top
         0,3,7,4  // bottom
    };
    private static int[] m_Indices = new int[36]
    {
          0,  1,  2,  3,  0,  2, // near
          4,  5,  6,  7,  4,  6, // far
          8,  9, 10, 11,  8, 10, // left
         12, 13, 14, 15, 12, 14, // right
         16, 17, 18, 19, 16, 18, // top
         20, 21, 22, 23, 20, 22, // bottom
    };  //              |______|---> shared vertices

    public static Mesh GenerateFrustumMesh(this Camera cam)
    {
        Mesh mesh = new Mesh();
        Vector3[] v = new Vector3[8];
        v[0] = v[4] = new Vector3(0, 0, 0);
        v[1] = v[5] = new Vector3(0, 1, 0);
        v[2] = v[6] = new Vector3(1, 1, 0);
        v[3] = v[7] = new Vector3(1, 0, 0);
        v[0].z = v[1].z = v[2].z = v[3].z = cam.nearClipPlane;
        v[4].z = v[5].z = v[6].z = v[7].z = cam.farClipPlane;
        // Transform viewport --> world --> local
        for (int i = 0; i < 8; i++)
            v[i] = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(v[i]));

        Vector3[] vertices = new Vector3[24];
        Vector3[] normals = new Vector3[24];
        // Split vertices for each face (8 vertices --> 24 vertices)
        for (int i = 0; i < 24; i++)
            vertices[i] = v[m_VertOrder[i]];
        // Calculate facenormal
        for (int i = 0; i < 6; i++)
        {
            Vector3 faceNormal = Vector3.Cross(vertices[i * 4 + 2] - vertices[i * 4 + 1], vertices[i * 4 + 0] - vertices[i * 4 + 1]);
            normals[i * 4 + 0] = normals[i * 4 + 1] = normals[i * 4 + 2] = normals[i * 4 + 3] = faceNormal;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = m_Indices;
        return mesh;
    }
}
