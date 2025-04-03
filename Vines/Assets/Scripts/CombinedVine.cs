using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombinedVine : MonoBehaviour
{
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
    [Header("Leaf stuff")]
    public float leafProbability = 0.5f;
    public GameObject leafPrefab;
    public bool generateLeaves = true;

    [Header("Stuff")]
    public List<Vertex> vinePoints = new List<Vertex>();
    public GameObject environmentMesh;
    public MeshFilter environmentMeshFilter;

    [Header("Vine growth stuff")]
    public float searchRadius = 1.0f;
    public bool stopVine = false;

    [Header("Mesh tube stuff")]
    public int circleDivisions = 24;
    public float startRadius = 0.06f;
    public float endRadius = 0.005f;
    public float uvTileFactor = 10.0f;
    public Texture2D vineTexture;

    MeshFilter mf;
    MeshRenderer mr;

    [Header("Light stuff")]
    public Transform lightTransform;

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        environmentMeshFilter = environmentMesh.GetComponent<MeshFilter>();

        if (mr.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            if (vineTexture != null)
            {
                mat.mainTexture = vineTexture;
            }
            else
            {
                mat.color = new Color(0.3f, 0.15f, 0.05f);
            }
            mr.material = mat;
        }

        AddPoint(GetGround(transform.position));
        GenerateMesh();
    }

    void Update()
    {
        if (!stopVine && vinePoints.Count < 100)
        {
            Vertex currentPoint = vinePoints.Last();
            FindNextPoint(currentPoint);
            GenerateMesh();
        }
        else
        {
            if (generateLeaves)
            {
                GenerateLeaves();
                generateLeaves = false;
            }
        }
    }

    Vertex GetGround(Vector3 startPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(startPosition + Vector3.up, Vector3.down, out hit, 10.0f))
        {
            return new Vertex(hit.point, hit.normal);
        }
        return new Vertex(startPosition, Vector3.up);
    }

    void AddPoint(Vertex newPoint)
    {
        if (vinePoints.Any(vp => Vector3.Distance(vp.point, newPoint.point) < 0.001f))
        {
            return;
        }
        vinePoints.Add(newPoint);
    }

    List<Vector3> GetNearbyMeshPoints(Vector3 currentPoint, float radius)
    {
        List<Vector3> nearbyPoints = new List<Vector3>();

        Vector3[] vertices = environmentMeshFilter.mesh.vertices;

        if (environmentMesh != null && environmentMeshFilter.mesh != null)
        {
            vertices = environmentMeshFilter.mesh.vertices;
        }
        else
        {
            Debug.LogWarning("Mesh is not assigned or mesh is empty.");
        }

        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = environmentMesh.transform.TransformPoint(vertex);

            if (Vector3.Distance(worldVertex, currentPoint) <= radius)
            {
                nearbyPoints.Add(worldVertex);
            }
        }

        return nearbyPoints;
    }

    void FindNextPoint(Vertex currentPoint)
    {
        Vector3 prevPos = currentPoint.point;

        // Get the nearby mesh points around the current vine point
        List<Vector3> nearbyPoints = GetNearbyMeshPoints(prevPos, searchRadius);

        float bestScore = float.MinValue;
        Vector3 bestPoint = prevPos;

        // Light position to calculate sun alignment (using the direction to the light)
        Vector3 lightPosition = lightTransform.position;

        // Flag to determine if the vine should go down
        bool shouldMoveDown = true;

        // Check if there are any candidates that are equal or higher than the current position
        foreach (Vector3 candidate in nearbyPoints)
        {
            if (candidate == prevPos || Vector3.Distance(prevPos, candidate) < 0.001f)
                continue;  // Skip if the candidate is the same as the previous point

            // Compute vertical movement (Y difference)
            float verticalMovement = candidate.y - prevPos.y;

            // If any candidate point is higher or equal, set shouldMoveDown to false
            if (verticalMovement >= 0)
            {
                shouldMoveDown = false; // There's no need to go down if there are candidates higher or at the same height
            }
        }

        // Iterate over nearby points to choose the best one based on scoring
        foreach (Vector3 candidate in nearbyPoints)
        {
            if (candidate == prevPos || Vector3.Distance(prevPos, candidate) < 0.001f)
                continue;  // Skip if the candidate is the same as the previous point

            // Compute vertical movement (Y difference)
            float verticalMovement = candidate.y - prevPos.y;

            // Check if we should allow downward movement or only upwards
            if (shouldMoveDown && verticalMovement >= 0) // Allow downward movement only if no higher candidates
            {
                // If we're allowing downward movement, apply a smaller penalty
                verticalMovement = Mathf.Clamp(verticalMovement * 0.5f, -1f, 0f);
            }

            // Calculate the direction towards the light and candidate direction
            Vector3 toLight = (lightPosition - prevPos).normalized;
            Vector3 candidateDir = (candidate - prevPos).normalized;

            // Getting weight for candidate's orientation towards the sun
            float sunFactor = Mathf.Clamp01(Vector3.Dot(candidateDir, toLight));

            // Penalize downward movement, but allow it to some degree
            float heightFactor;
            if (verticalMovement < 0)  // If going downward
            {
                // Check if downward movement is necessary
                heightFactor = Mathf.Clamp01(verticalMovement * 0.1f); // Smaller penalty for going down
            }
            else  // If going upwards
            {
                // Prioritize upward movement more
                heightFactor = Mathf.Clamp01(verticalMovement * 2f);  // Larger reward for going up
            }

            // Compute a score based on sun alignment and vertical movement
            float score = (sunFactor * 0.5f) + (heightFactor * 2.0f);

            // Choose the best candidate with the highest score
            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidate;
            }
        }

        // If a valid candidate was found, add it to the vine
        if (bestScore != float.MinValue)
        {
            // Add a new point only if it's sufficiently different from the last one
            if (Vector3.Distance(bestPoint, vinePoints.Last().point) > 0.001f)
            {
                AddPoint(new Vertex(bestPoint, Vector3.up)); // Add the best point to the vine
            }
            else
            {
                stopVine = true; // Stop vine growth if no valid new points
            }
        }
        else
        {
            stopVine = true; // Stop vine growth if no valid point is found
        }
    }


    void GenerateMesh()
    {
        if (vinePoints.Count < 2)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // We'll store the previous ring rotation to interpolate smoothly
        Quaternion lastRingRotation = Quaternion.identity;

        for (int i = 0; i < vinePoints.Count; i++)
        {
            float t = (float)i / (vinePoints.Count - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            // Offset the center along the vine's normal to help prevent clipping.
            Vector3 centerWorld = vinePoints[i].point + vinePoints[i].normal * radius;
            Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);

            // Compute the tangent using adjacent vine points.
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

            // Choose an up reference that isn’t too aligned with the tangent.
            Vector3 upRef = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangentLocal, upRef)) > 0.99f)
                upRef = Vector3.right;

            // Compute the target rotation for the ring.
            Quaternion targetRotation = Quaternion.LookRotation(tangentLocal, upRef);

            // Smoothly interpolate with the previous ring's rotation.
            Quaternion ringRotation = (i == 0) ? targetRotation : Quaternion.Slerp(lastRingRotation, targetRotation, 0.5f);
            lastRingRotation = ringRotation;

            // Generate ring vertices using the interpolated rotation.
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2 * Mathf.PI * j / circleDivisions;
                // Create a 2D point on a circle (lying in the ring's plane)
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                // Rotate the offset into the correct orientation for this ring.
                Vector3 offset = ringRotation * localOffset;
                vertices.Add(centerLocal + offset);
                uvs.Add(new Vector2((float)j / circleDivisions, t * uvTileFactor));
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
        Debug.Log("Generating leaves...");
        for (int i = 0; i < vinePoints.Count; i++)
        {
            if (Random.value <= leafProbability)
            {
                Debug.Log("Leaf generated at: " + vinePoints[i].point);
                SpawnLeafAtPoint(i);
            }
        }
    }

    void SpawnLeafAtPoint(int i)
    {
        Vector3 centerWorld = vinePoints[i].point;
        Vector3 surfaceNormal = vinePoints[i].normal;

        // Use the vine's radius (calculated based on position in the vine) to offset leaf position
        float radius = Mathf.Lerp(startRadius, endRadius, (float)i / (vinePoints.Count - 1));
        Vector3 leafOffset = surfaceNormal * radius;  // Offset the leaf along the surface normal

        // Add random deviation to make leaf position more natural
        Vector3 randomDeviation = Random.insideUnitSphere * 0.02f;  // Slight random offset for variation
        Vector3 leafPosition = centerWorld + leafOffset + randomDeviation;

        // Generate leaf rotation based on surface normal
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

        // Convert to local space and instantiate the leaf
        Vector3 centerLocal = transform.InverseTransformPoint(leafPosition);
        GameObject leaf = Instantiate(leafPrefab, transform);
        leaf.transform.localPosition = centerLocal;
        leaf.transform.localRotation = leafRotation;

        // Apply random scale for variety
        float baseScale = 0.0025f;
        float randomScale = Random.Range(0.8f, 1.2f);
        leaf.transform.localScale = Vector3.one * (baseScale * randomScale);
    }

}
