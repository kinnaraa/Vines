using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VineGenerator : MonoBehaviour
{
    [Header("Vine Path Settings")]
    public int segments = 300;            // Increased segments for a longer, smoother vine
    public float stepLength = 0.3f;         // Distance between center points (increases total length)
    public float curveAmplitude = 0.3f;     // Amplitude of the curve (try keeping it moderate)
    public float curveFrequency = 0.2f;     // Lower frequency for gentle curves
    public float noiseAmount = 0.05f;       // Amount of random noise added to each center point (x and y)

    [Header("Vine Tube Settings")]
    public int circleDivisions = 12;      // Number of vertices around each ring
    public float startRadius = 0.1f;      // Starting radius (thin vine)
    public float endRadius = 0.01f;       // Ending radius (tapers to almost a line)

    void Start()
    {
        GenerateVine();
    }

    void GenerateVine()
    {
        // Generate centerline points along the vine.
        Vector3[] centerPoints = new Vector3[segments + 1];
        centerPoints[0] = Vector3.zero;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float z = i * stepLength;
            // Base curve using sine and cosine.
            float x = Mathf.Sin(i * curveFrequency) * curveAmplitude;
            float y = Mathf.Cos(i * curveFrequency) * curveAmplitude;
            // Add random variation in x and y.
            Vector2 randomOffset = Random.insideUnitCircle * noiseAmount;
            centerPoints[i] = new Vector3(x + randomOffset.x, y + randomOffset.y, z);
        }

        // Prepare lists for mesh data.
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Generate a circular ring of vertices at each center point.
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            Vector3 center = centerPoints[i];

            // Determine tangent direction for the current segment.
            Vector3 tangent;
            if (i < segments)
                tangent = (centerPoints[i + 1] - center).normalized;
            else
                tangent = (center - centerPoints[i - 1]).normalized;

            // Choose an arbitrary vector to build a coordinate frame.
            Vector3 arbitrary = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.99f)
                arbitrary = Vector3.right;

            Vector3 normal = Vector3.Cross(tangent, arbitrary).normalized;
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            // Create vertices in a circle around the center.
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2 * Mathf.PI * j / circleDivisions;
                Vector3 offset = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal) * radius;
                vertices.Add(center + offset);
                uvs.Add(new Vector2((float)j / circleDivisions, t));
            }
        }

        // Create triangles connecting each ring.
        for (int i = 0; i < segments; i++)
        {
            int ringStart = i * circleDivisions;
            int nextRingStart = (i + 1) * circleDivisions;

            for (int j = 0; j < circleDivisions; j++)
            {
                int current = ringStart + j;
                int next = ringStart + ((j + 1) % circleDivisions);
                int currentNext = nextRingStart + j;
                int nextNext = nextRingStart + ((j + 1) % circleDivisions);

                // First triangle of quad.
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(currentNext);

                // Second triangle of quad.
                triangles.Add(next);
                triangles.Add(nextNext);
                triangles.Add(currentNext);
            }
        }

        // Build and assign the mesh.
        Mesh mesh = new Mesh();
        mesh.name = "LongThinVineMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        // Create a brown material if none is assigned.
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Unlit/Color"));
            // Set a basic brown color.
            mat.color = new Color(0.55f, 0.27f, 0.07f);
            mr.material = mat;
        }
    }
}
