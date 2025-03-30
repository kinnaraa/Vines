using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombinedVine : MonoBehaviour
{
    // Struct to store a vine point (position + surface normal)
    public struct Vertex
    {
        public Vector3 point;
        public Vector3 normal;
        public Vertex(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal;
        }
    }

    // List of vine points (centerline of the vine)
    public List<Vertex> vinePoints = new List<Vertex>();

    [Header("Vine Growth Settings")]
    public float searchRadius = 0.5f;
    public int numRays = 100;
    public float highestSearchRadius = 4.0f;
    private Vector3 currentHighestPoint = Vector3.zero;
    private bool foundHighest = false;
    private bool stopVine = false;
    private bool needsToGoDown = false;

    [Header("Mesh Tube Settings")]
    public int circleDivisions = 12;  // Number of vertices in each ring
    public float startRadius = 0.075f;    // Radius at the beginning of the vine
    public float endRadius = 0.0075f;     // Radius at the end (tapering)

    MeshFilter mf;
    MeshRenderer mr;

    [Header("Leaf Settings")]
    public GameObject leafPrefab;
    public float leafProbability = 0.5f;
    // When growth is done, leaves are generated all at once.
    public bool generateLeaves = true;

    public Texture2D vineTexture;

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        // Set a default material if none is assigned.
        if (mr.sharedMaterial == null)
        {
            // Use a shader that supports textures, for example Unlit/Texture
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            if (vineTexture != null)
            {
                mat.mainTexture = vineTexture;
            }
            else
            {
                // Fallback to a darker brown if no texture is assigned.
                mat.color = new Color(0.3f, 0.15f, 0.05f);
            }
            mr.material = mat;
        }

        // Initialize the vine by getting a starting ground point.
        AddPoint(GetGround(transform.position));
        GenerateMesh();
    }

    void Update()
    {
        // Continue growing the vine if it hasn't stopped.
        if (!stopVine && vinePoints.Count < 100)
        {
            Vertex currentPoint = vinePoints.Last();
            FindNextPoint(currentPoint);
            GenerateMesh(); // update the mesh each time new points are added
        }
        else
        {
            // When vine growth is finished, generate leaves (once)
            if (generateLeaves)
            {
                GenerateLeaves();
                generateLeaves = false;
            }
        }
    }

    // Uses a raycast to get the ground position at a starting point.
    Vertex GetGround(Vector3 startPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(startPosition + Vector3.up, Vector3.down, out hit, 10.0f))
        {
            return new Vertex(hit.point, hit.normal);
        }
        return new Vertex(startPosition, Vector3.up);
    }

    // Adds a new vine point if it isn’t already in the list.
    void AddPoint(Vertex newPoint)
    {
        if (vinePoints.Any(vp => Vector3.Distance(vp.point, newPoint.point) < 0.001f))
        {
            return;
        }
        vinePoints.Add(newPoint);
    }

    // Searches for the next valid vine point using raycasts.
    void FindNextPoint(Vertex currentPoint)
    {
        if (foundHighest && (currentPoint.point.y - currentHighestPoint.y >= 0.0f ||
            currentPoint.point.y - currentHighestPoint.y >= -0.05f))
        {
            highestSearchRadius *= 1.5f;
            foundHighest = false;
        }

        if (!foundHighest)
        {
            float highestMaxY = currentHighestPoint.y;
            for (int i = 0; i < 500; i++)
            {
                Vector3 samplePoint = currentPoint.point + Random.insideUnitSphere * highestSearchRadius;
                samplePoint.y += 1.0f;
                RaycastHit hit;
                if (Physics.Raycast(samplePoint, Vector3.down, out hit, 20.0f))
                {
                    if (hit.point.y >= highestMaxY &&
                        !vinePoints.Any(vp => Vector3.Distance(vp.point, hit.point) < 0.001f))
                    {
                        highestMaxY = hit.point.y;
                        currentHighestPoint = hit.point;
                    }
                }
            }
            if (currentHighestPoint != Vector3.zero)
            {
                foundHighest = true;
            }
            else
            {
                stopVine = true;
            }
        }

        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.zero;
        float bestScore = float.MinValue;
        Vector3 growthDirection = (currentHighestPoint - currentPoint.point).normalized;
        Vector3 rayOrigin = currentPoint.point + currentPoint.normal * 0.05f;

        for (int i = 0; i < numRays; i++)
        {
            float azimuth = (360f / numRays) * i;
            float elevation = Random.Range(0f, 360f);
            if (needsToGoDown)
            {
                elevation = Random.Range(-135f, 10f);
                rayOrigin -= Vector3.up * 0.1f;
            }

            Quaternion rotationAzimuth = Quaternion.AngleAxis(azimuth, currentPoint.normal);
            Quaternion rotationElevation = Quaternion.AngleAxis(elevation, Vector3.Cross(currentPoint.normal, growthDirection));
            Vector3 direction = rotationAzimuth * rotationElevation * growthDirection;
            direction.Normalize();

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, direction, out hit, searchRadius))
            {
                float alignment = Vector3.Dot(direction, growthDirection);
                float score = alignment;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                }
            }
        }
        if (bestScore != float.MinValue)
        {
            AddPoint(new Vertex(bestPoint, bestNormal));
        }
        else
        {
            stopVine = true;
        }
    }

    // Generates a tube mesh along the vinePoints.
    void GenerateMesh()
    {
        if (vinePoints.Count < 2)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < vinePoints.Count; i++)
        {
            float t = (float)i / (vinePoints.Count - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            Vector3 centerWorld = vinePoints[i].point + vinePoints[i].normal * radius;
            Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);


            // Compute tangent in local space
            Vector3 tangentLocal;
            if (i < vinePoints.Count - 1)
            {
                Vector3 nextWorld = vinePoints[i + 1].point;
                Vector3 nextLocal = transform.InverseTransformPoint(nextWorld);
                tangentLocal = (nextLocal - centerLocal).normalized;
            }
            else
            {
                Vector3 prevWorld = vinePoints[i - 1].point;
                Vector3 prevLocal = transform.InverseTransformPoint(prevWorld);
                tangentLocal = (centerLocal - prevLocal).normalized;
            }

            // Pick an arbitrary vector to define normal/binormal
            Vector3 arbitraryLocal = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangentLocal, Vector3.up)) > 0.99f)
                arbitraryLocal = Vector3.right;

            Vector3 normalLocal = Vector3.Cross(tangentLocal, arbitraryLocal).normalized;
            Vector3 binormalLocal = Vector3.Cross(tangentLocal, normalLocal).normalized;

            // Create the ring vertices in local space
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2 * Mathf.PI * j / circleDivisions;
                Vector3 offset = (Mathf.Cos(angle) * normalLocal + Mathf.Sin(angle) * binormalLocal) * radius;
                vertices.Add(centerLocal + offset);
                uvs.Add(new Vector2((float)j / circleDivisions, t));
            }
        }

        // Build triangles by connecting consecutive rings.
        int ringCount = vinePoints.Count;
        for (int i = 0; i < ringCount - 1; i++)
        {
            int ringStart = i * circleDivisions;
            int nextRingStart = (i + 1) * circleDivisions;
            for (int j = 0; j < circleDivisions; j++)
            {
                int current = ringStart + j;
                int next = ringStart + ((j + 1) % circleDivisions);
                int currentNext = nextRingStart + j;
                int nextNext = nextRingStart + ((j + 1) % circleDivisions);

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(currentNext);

                triangles.Add(next);
                triangles.Add(nextNext);
                triangles.Add(currentNext);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "VineMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    void GenerateLeaves()
    {
        // Optional: Clear existing leaves.
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Leaf"))
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < vinePoints.Count; i++)
        {
            if (Random.value <= leafProbability)
            {
                SpawnLeafAtPoint(i);
            }
        }
    }

    void SpawnLeafAtPoint(int i)
    {
        Vector3 centerWorld = vinePoints[i].point;
        Vector3 surfaceNormal = vinePoints[i].normal;

        float deviationAngle = 5f;
        Vector3 randomAxis = Vector3.Cross(surfaceNormal, Random.onUnitSphere);
        if (randomAxis.sqrMagnitude < 0.001f)
            randomAxis = Vector3.right;
        randomAxis.Normalize();
        Quaternion deviationRotation = Quaternion.AngleAxis(Random.Range(-deviationAngle, deviationAngle), randomAxis);
        Vector3 adjustedUp = deviationRotation * surfaceNormal;

        Vector3 randomForwardCandidate = Random.onUnitSphere;
        Vector3 leafForward = Vector3.ProjectOnPlane(randomForwardCandidate, adjustedUp).normalized;
        if (leafForward == Vector3.zero)
            leafForward = Vector3.forward;

        Quaternion leafRotation = Quaternion.LookRotation(leafForward, adjustedUp);

        Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);
        GameObject leaf = Instantiate(leafPrefab, transform);
        leaf.transform.localPosition = centerLocal;
        leaf.transform.localRotation = leafRotation;

        float baseScale = 0.006f;
        float randomScale = Random.Range(0.8f, 1.2f);
        leaf.transform.localScale = Vector3.one * (baseScale * randomScale);
    }


}
